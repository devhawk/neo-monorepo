#pragma warning disable IDE0051

using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    public sealed class ContractManagement : NativeContract
    {
        private const byte Prefix_MinimumDeploymentFee = 20;
        private const byte Prefix_NextAvailableId = 15;
        private const byte Prefix_Contract = 8;

        internal ContractManagement()
        {
            var events = new List<ContractEventDescriptor>(Manifest.Abi.Events)
            {
                new ContractEventDescriptor
                {
                    Name = "Deploy",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "Hash",
                            Type = ContractParameterType.Hash160
                        }
                    }
                },
                new ContractEventDescriptor
                {
                    Name = "Update",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "Hash",
                            Type = ContractParameterType.Hash160
                        }
                    }
                },
                new ContractEventDescriptor
                {
                    Name = "Destroy",
                    Parameters = new ContractParameterDefinition[]
                    {
                        new ContractParameterDefinition()
                        {
                            Name = "Hash",
                            Type = ContractParameterType.Hash160
                        }
                    }
                }
            };

            Manifest.Abi.Events = events.ToArray();
        }

        private static void Check(byte[] script, ContractAbi abi)
        {
            Script s = new Script(script, true);
            foreach (ContractMethodDescriptor method in abi.Methods)
                s.GetInstruction(method.Offset);
            abi.GetMethod(string.Empty, 0); // Trigger the construction of ContractAbi.methodDictionary to check the uniqueness of the method names.
            _ = abi.Events.ToDictionary(p => p.Name); // Check the uniqueness of the event names.
        }

        private int GetNextAvailableId(DataCache snapshot)
        {
            StorageItem item = snapshot.GetAndChange(CreateStorageKey(Prefix_NextAvailableId));
            int value = (int)(BigInteger)item;
            item.Add(1);
            return value;
        }

        internal override void Initialize(ApplicationEngine engine)
        {
            engine.Snapshot.Add(CreateStorageKey(Prefix_MinimumDeploymentFee), new StorageItem(10_00000000));
            engine.Snapshot.Add(CreateStorageKey(Prefix_NextAvailableId), new StorageItem(1));
        }

        internal override void OnPersist(ApplicationEngine engine)
        {
            foreach (NativeContract contract in Contracts)
            {
                if (engine.ProtocolSettings.GetNativeActivation(contract.Name) != engine.PersistingBlock.Index)
                    continue;
                engine.Snapshot.Add(CreateStorageKey(Prefix_Contract).Add(contract.Hash), new StorageItem(new ContractState
                {
                    Id = contract.Id,
                    Nef = contract.Nef,
                    Hash = contract.Hash,
                    Manifest = contract.Manifest
                }));
                contract.Initialize(engine);
            }
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        private long GetMinimumDeploymentFee(DataCache snapshot)
        {
            return (long)(BigInteger)snapshot[CreateStorageKey(Prefix_MinimumDeploymentFee)];
        }

        [ContractMethod(0_03000000, CallFlags.WriteStates)]
        private void SetMinimumDeploymentFee(ApplicationEngine engine, BigInteger value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (!CheckCommittee(engine)) throw new InvalidOperationException();
            engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_MinimumDeploymentFee)).Set(value);
        }

        [ContractMethod(0_01000000, CallFlags.ReadStates)]
        public ContractState GetContract(DataCache snapshot, UInt160 hash)
        {
            return snapshot.TryGet(CreateStorageKey(Prefix_Contract).Add(hash))?.GetInteroperable<ContractState>();
        }

        public IEnumerable<ContractState> ListContracts(DataCache snapshot)
        {
            byte[] listContractsPrefix = CreateStorageKey(Prefix_Contract).ToArray();
            return snapshot.Find(listContractsPrefix).Select(kvp => kvp.Value.GetInteroperable<ContractState>());
        }

        [ContractMethod(0, CallFlags.WriteStates | CallFlags.AllowNotify)]
        private ContractState Deploy(ApplicationEngine engine, byte[] nefFile, byte[] manifest)
        {
            return Deploy(engine, nefFile, manifest, StackItem.Null);
        }

        [ContractMethod(0, CallFlags.WriteStates | CallFlags.AllowNotify)]
        private ContractState Deploy(ApplicationEngine engine, byte[] nefFile, byte[] manifest, StackItem data)
        {
            if (engine.ScriptContainer is not Transaction tx)
                throw new InvalidOperationException();
            if (nefFile.Length == 0)
                throw new ArgumentException($"Invalid NefFile Length: {nefFile.Length}");
            if (manifest.Length == 0)
                throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");

            engine.AddGas(Math.Max(
                engine.StoragePrice * (nefFile.Length + manifest.Length),
                GetMinimumDeploymentFee(engine.Snapshot)
                ));

            NefFile nef = nefFile.AsSerializable<NefFile>();
            ContractManifest parsedManifest = ContractManifest.Parse(manifest);
            Check(nef.Script, parsedManifest.Abi);
            UInt160 hash = Helper.GetContractHash(tx.Sender, nef.CheckSum, parsedManifest.Name);
            StorageKey key = CreateStorageKey(Prefix_Contract).Add(hash);
            if (engine.Snapshot.Contains(key))
                throw new InvalidOperationException($"Contract Already Exists: {hash}");
            ContractState contract = new ContractState
            {
                Id = GetNextAvailableId(engine.Snapshot),
                UpdateCounter = 0,
                Nef = nef,
                Hash = hash,
                Manifest = parsedManifest
            };

            if (!contract.Manifest.IsValid(hash)) throw new InvalidOperationException($"Invalid Manifest Hash: {hash}");

            engine.Snapshot.Add(key, new StorageItem(contract));

            // Execute _deploy

            ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod("_deploy", 2);
            if (md != null)
                engine.CallFromNativeContract(Hash, hash, md.Name, data, false);

            engine.SendNotification(Hash, "Deploy", new VM.Types.Array { contract.Hash.ToArray() });

            return contract;
        }

        [ContractMethod(0, CallFlags.WriteStates | CallFlags.AllowNotify)]
        private void Update(ApplicationEngine engine, byte[] nefFile, byte[] manifest)
        {
            Update(engine, nefFile, manifest, StackItem.Null);
        }

        [ContractMethod(0, CallFlags.WriteStates | CallFlags.AllowNotify)]
        private void Update(ApplicationEngine engine, byte[] nefFile, byte[] manifest, StackItem data)
        {
            if (nefFile is null && manifest is null) throw new ArgumentException();

            engine.AddGas(engine.StoragePrice * ((nefFile?.Length ?? 0) + (manifest?.Length ?? 0)));

            var contract = engine.Snapshot.GetAndChange(CreateStorageKey(Prefix_Contract).Add(engine.CallingScriptHash))?.GetInteroperable<ContractState>();
            if (contract is null) throw new InvalidOperationException($"Updating Contract Does Not Exist: {engine.CallingScriptHash}");

            if (nefFile != null)
            {
                if (nefFile.Length == 0)
                    throw new ArgumentException($"Invalid NefFile Length: {nefFile.Length}");

                // Update nef
                contract.Nef = nefFile.AsSerializable<NefFile>();
            }
            if (manifest != null)
            {
                if (manifest.Length == 0)
                    throw new ArgumentException($"Invalid Manifest Length: {manifest.Length}");
                ContractManifest manifest_new = ContractManifest.Parse(manifest);
                if (manifest_new.Name != contract.Manifest.Name)
                    throw new InvalidOperationException("The name of the contract can't be changed.");
                if (!manifest_new.IsValid(contract.Hash))
                    throw new InvalidOperationException($"Invalid Manifest Hash: {contract.Hash}");
                contract.Manifest = manifest_new;
            }
            Check(contract.Nef.Script, contract.Manifest.Abi);
            contract.UpdateCounter++; // Increase update counter
            if (nefFile != null)
            {
                ContractMethodDescriptor md = contract.Manifest.Abi.GetMethod("_deploy", 2);
                if (md != null)
                    engine.CallFromNativeContract(Hash, contract.Hash, md.Name, data, true);
            }
            engine.SendNotification(Hash, "Update", new VM.Types.Array { contract.Hash.ToArray() });
        }

        [ContractMethod(0_01000000, CallFlags.WriteStates | CallFlags.AllowNotify)]
        private void Destroy(ApplicationEngine engine)
        {
            UInt160 hash = engine.CallingScriptHash;
            StorageKey ckey = CreateStorageKey(Prefix_Contract).Add(hash);
            ContractState contract = engine.Snapshot.TryGet(ckey)?.GetInteroperable<ContractState>();
            if (contract is null) return;
            engine.Snapshot.Delete(ckey);
            foreach (var (key, _) in engine.Snapshot.Find(BitConverter.GetBytes(contract.Id)))
                engine.Snapshot.Delete(key);
            engine.SendNotification(Hash, "Destroy", new VM.Types.Array { hash.ToArray() });
        }
    }
}
