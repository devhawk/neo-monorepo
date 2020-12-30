using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using static System.Threading.Interlocked;
using Array = System.Array;
using VMArray = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    public partial class ApplicationEngine : ExecutionEngine
    {
        /// <summary>
        /// This constant can be used for testing scripts.
        /// </summary>
        public const long TestModeGas = 20_00000000;

        public static event EventHandler<NotifyEventArgs> Notify;
        public static event EventHandler<LogEventArgs> Log;

        private static IApplicationEngineProvider applicationEngineProvider;
        private static Dictionary<uint, InteropDescriptor> services;
        private readonly long gas_amount;
        private List<NotifyEventArgs> notifications;
        private List<IDisposable> disposables;
        private readonly Dictionary<UInt160, int> invocationCounter = new Dictionary<UInt160, int>();
        private readonly uint exec_fee_factor;
        internal readonly uint StoragePrice;

        public static IReadOnlyDictionary<uint, InteropDescriptor> Services => services;
        private List<IDisposable> Disposables => disposables ??= new List<IDisposable>();
        public TriggerType Trigger { get; }
        public IVerifiable ScriptContainer { get; }
        public StoreView Snapshot { get; }
        public long GasConsumed { get; private set; } = 0;
        public long GasLeft => gas_amount - GasConsumed;
        public Exception FaultException { get; private set; }
        public UInt160 CurrentScriptHash => CurrentContext?.GetScriptHash();
        public UInt160 CallingScriptHash => CurrentContext?.GetState<ExecutionContextState>().CallingScriptHash;
        public UInt160 EntryScriptHash => EntryContext?.GetScriptHash();
        public IReadOnlyList<NotifyEventArgs> Notifications => notifications ?? (IReadOnlyList<NotifyEventArgs>)Array.Empty<NotifyEventArgs>();

        protected ApplicationEngine(TriggerType trigger, IVerifiable container, StoreView snapshot, long gas)
        {
            this.Trigger = trigger;
            this.ScriptContainer = container;
            this.Snapshot = snapshot;
            this.gas_amount = gas;
            this.exec_fee_factor = snapshot is null ? PolicyContract.DefaultExecFeeFactor : NativeContract.Policy.GetExecFeeFactor(Snapshot);
            this.StoragePrice = snapshot is null ? PolicyContract.DefaultStoragePrice : NativeContract.Policy.GetStoragePrice(Snapshot);
        }

        protected internal void AddGas(long gas)
        {
            GasConsumed = checked(GasConsumed + gas);
            if (GasConsumed > gas_amount)
                throw new InvalidOperationException("Insufficient GAS.");
        }

        protected override void OnFault(Exception e)
        {
            FaultException = e;
            base.OnFault(e);
        }

        internal void CallFromNativeContract(UInt160 callingScriptHash, UInt160 hash, string method, params StackItem[] args)
        {
            CallContractInternal(hash, method, CallFlags.All, false, args);
            ExecutionContextState state = CurrentContext.GetState<ExecutionContextState>();
            state.CallingScriptHash = callingScriptHash;
            StepOut();
        }

        internal T CallFromNativeContract<T>(UInt160 callingScriptHash, UInt160 hash, string method, params StackItem[] args)
        {
            CallContractInternal(hash, method, CallFlags.All, true, args);
            ExecutionContextState state = CurrentContext.GetState<ExecutionContextState>();
            state.CallingScriptHash = callingScriptHash;
            StepOut();
            return (T)Convert(Pop(), new InteropParameterDescriptor(typeof(T)));
        }

        public static ApplicationEngine Create(TriggerType trigger, IVerifiable container, StoreView snapshot, long gas = TestModeGas)
        {
            return applicationEngineProvider?.Create(trigger, container, snapshot, gas)
                  ?? new ApplicationEngine(trigger, container, snapshot, gas);
        }

        protected override void LoadContext(ExecutionContext context)
        {
            // Set default execution context state

            var state = context.GetState<ExecutionContextState>();
            state.ScriptHash ??= ((byte[])context.Script).ToScriptHash();
            invocationCounter.TryAdd(state.ScriptHash, 1);

            base.LoadContext(context);
        }

        public ExecutionContext LoadContract(ContractState contract, string method, CallFlags callFlags, bool hasReturnValue, ushort pcount)
        {
            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod(method);
            if (md is null) return null;

            ExecutionContext context = LoadScript(contract.Script,
                pcount: pcount,
                rvcount: hasReturnValue ? 1 : 0,
                initialPosition: md.Offset,
                configureState: p =>
                {
                    p.CallFlags = callFlags;
                    p.ScriptHash = contract.Hash;
                });

            // Call initialization
            var init = contract.Manifest.Abi.GetMethod("_initialize");
            if (init != null)
            {
                LoadContext(context.Clone(init.Offset));
            }

            return context;
        }

        public ExecutionContext LoadScript(Script script, ushort pcount = 0, int rvcount = -1, int initialPosition = 0, Action<ExecutionContextState> configureState = null)
        {
            // Create and configure context
            ExecutionContext context = CreateContext(script, pcount, rvcount, initialPosition);
            configureState?.Invoke(context.GetState<ExecutionContextState>());
            // Load context
            LoadContext(context);
            return context;
        }

        protected internal StackItem Convert(object value)
        {
            if (value is IDisposable disposable) Disposables.Add(disposable);
            return value switch
            {
                null => StackItem.Null,
                bool b => b,
                sbyte i => i,
                byte i => (BigInteger)i,
                short i => i,
                ushort i => (BigInteger)i,
                int i => i,
                uint i => i,
                long i => i,
                ulong i => i,
                Enum e => Convert(System.Convert.ChangeType(e, e.GetTypeCode())),
                byte[] data => data,
                string s => s,
                BigInteger i => i,
                JObject o => o.ToByteArray(false),
                IInteroperable interoperable => interoperable.ToStackItem(ReferenceCounter),
                ISerializable i => i.ToArray(),
                StackItem item => item,
                (object a, object b) => new Struct(ReferenceCounter) { Convert(a), Convert(b) },
                Array array => new VMArray(ReferenceCounter, array.OfType<object>().Select(p => Convert(p))),
                _ => StackItem.FromInterface(value)
            };
        }

        protected internal object Convert(StackItem item, InteropParameterDescriptor descriptor)
        {
            if (descriptor.IsArray)
            {
                Array av;
                if (item is VMArray array)
                {
                    av = Array.CreateInstance(descriptor.Type.GetElementType(), array.Count);
                    for (int i = 0; i < av.Length; i++)
                        av.SetValue(descriptor.Converter(array[i]), i);
                }
                else
                {
                    int count = (int)item.GetInteger();
                    if (count > Limits.MaxStackSize) throw new InvalidOperationException();
                    av = Array.CreateInstance(descriptor.Type.GetElementType(), count);
                    for (int i = 0; i < av.Length; i++)
                        av.SetValue(descriptor.Converter(Pop()), i);
                }
                return av;
            }
            else
            {
                object value = descriptor.Converter(item);
                if (descriptor.IsEnum)
                    value = Enum.ToObject(descriptor.Type, value);
                else if (descriptor.IsInterface)
                    value = ((InteropInterface)value).GetInterface<object>();
                return value;
            }
        }

        public override void Dispose()
        {
            if (disposables != null)
            {
                foreach (IDisposable disposable in disposables)
                    disposable.Dispose();
                disposables = null;
            }
            base.Dispose();
        }

        protected void ValidateCallFlags(InteropDescriptor descriptor)
        {
            ExecutionContextState state = CurrentContext.GetState<ExecutionContextState>();
            if (!state.CallFlags.HasFlag(descriptor.RequiredCallFlags))
                throw new InvalidOperationException($"Cannot call this SYSCALL with the flag {state.CallFlags}.");
        }

        protected override void OnSysCall(uint method)
        {
            InteropDescriptor descriptor = services[method];
            ValidateCallFlags(descriptor);
            AddGas(descriptor.FixedPrice * exec_fee_factor);
            List<object> parameters = descriptor.Parameters.Count > 0
                ? new List<object>()
                : null;
            foreach (var pd in descriptor.Parameters)
                parameters.Add(Convert(Pop(), pd));
            object returnValue = descriptor.Handler.Invoke(this, parameters?.ToArray());
            if (descriptor.Handler.ReturnType != typeof(void))
                Push(Convert(returnValue));
        }

        protected override void PreExecuteInstruction()
        {
            if (CurrentContext.InstructionPointer < CurrentContext.Script.Length)
                AddGas(exec_fee_factor * OpCodePrices[CurrentContext.CurrentInstruction.OpCode]);
        }

        internal void StepOut()
        {
            int c = InvocationStack.Count;
            while (State != VMState.HALT && State != VMState.FAULT && InvocationStack.Count >= c)
                ExecuteNext();
            if (State == VMState.FAULT)
                throw new InvalidOperationException("StepOut failed.", FaultException);
        }

        private static Block CreateDummyBlock(StoreView snapshot)
        {
            var currentBlock = snapshot.Blocks[snapshot.CurrentBlockHash];
            return new Block
            {
                Version = 0,
                PrevHash = snapshot.CurrentBlockHash,
                MerkleRoot = new UInt256(),
                Timestamp = currentBlock.Timestamp + Blockchain.MillisecondsPerBlock,
                Index = snapshot.Height + 1,
                NextConsensus = currentBlock.NextConsensus,
                Witness = new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                },
                ConsensusData = new ConsensusData(),
                Transactions = new Transaction[0]
            };
        }

        private static InteropDescriptor Register(string name, string handler, long fixedPrice, CallFlags requiredCallFlags)
        {
            MethodInfo method = typeof(ApplicationEngine).GetMethod(handler, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? typeof(ApplicationEngine).GetProperty(handler, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetMethod;
            InteropDescriptor descriptor = new InteropDescriptor(name, method, fixedPrice, requiredCallFlags);
            services ??= new Dictionary<uint, InteropDescriptor>();
            services.Add(descriptor.Hash, descriptor);
            return descriptor;
        }

        internal static void ResetApplicationEngineProvider()
        {
            Exchange(ref applicationEngineProvider, null);
        }

        public static ApplicationEngine Run(byte[] script, StoreView snapshot = null, IVerifiable container = null, Block persistingBlock = null, int offset = 0, long gas = TestModeGas)
        {
            SnapshotView disposable = null;
            if (snapshot is null)
            {
                disposable = Blockchain.Singleton.GetSnapshot();
                snapshot = disposable;
            }
            snapshot.PersistingBlock = persistingBlock ?? snapshot.PersistingBlock ?? CreateDummyBlock(snapshot);
            ApplicationEngine engine = Create(TriggerType.Application, container, snapshot, gas);
            if (disposable != null) engine.Disposables.Add(disposable);
            engine.LoadScript(script, initialPosition: offset);
            engine.Execute();
            return engine;
        }

        internal static bool SetApplicationEngineProvider(IApplicationEngineProvider provider)
        {
            return CompareExchange(ref applicationEngineProvider, provider, null) is null;
        }
    }
}
