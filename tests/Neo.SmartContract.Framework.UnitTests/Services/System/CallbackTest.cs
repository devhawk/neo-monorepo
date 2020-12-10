extern alias scfx;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Compiler.MSIL.UnitTests.Utils;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Framework.UnitTests;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;
using SyscallCallback = scfx.Neo.SmartContract.Framework.Services.System.SyscallCallback;

namespace Neo.Compiler.MSIL.SmartContractFramework.Services.System
{
    [TestClass]
    public class CallbackTest
    {
        private TestEngine _engine;
        private UInt160 scriptHash;

        [TestInitialize]
        public void Init()
        {
            var _ = TestBlockchain.TheNeoSystem;
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();
            snapshot.BlockHashIndex.Get().Index = 1234;

            _engine = new TestEngine(TriggerType.Application, snapshot: snapshot);
            _engine.AddEntryScript("./TestClasses/Contract_Callback.cs");
            scriptHash = _engine.ScriptEntry.finalNEFScript.ToScriptHash();

            snapshot.Contracts.Add(scriptHash, new ContractState()
            {
                Hash = scriptHash,
                Script = _engine.ScriptEntry.finalNEFScript,
                Manifest = ContractManifest.Parse(_engine.ScriptEntry.finalManifest)
            });
        }

        [TestMethod]
        public void createFromMethodTest()
        {
            _engine.Reset();
            var result = _engine.ExecuteTestCaseStandard("createFromMethod", scriptHash.ToArray(), "test");
            Assert.AreEqual(VMState.HALT, _engine.State);
            Assert.AreEqual(1, result.Count);

            var item = result.Pop();
            Assert.IsInstanceOfType(item, typeof(InteropInterface));
        }

        [TestMethod]
        public void createAndCallFromMethodTest()
        {
            _engine.Reset();
            var result = _engine.ExecuteTestCaseStandard("createAndCallFromMethod", scriptHash.ToArray(), "test");
            Assert.AreEqual(VMState.HALT, _engine.State);
            Assert.AreEqual(1, result.Count);

            var item = result.Pop<Integer>();
            Assert.AreEqual(123, item.GetInteger());
        }

        [TestMethod]
        public void createFromSyscallTest()
        {
            _engine.Reset();
            var result = _engine.ExecuteTestCaseStandard("createFromSyscall", (uint)SyscallCallback.System_Blockchain_GetHeight);
            Assert.AreEqual(VMState.HALT, _engine.State);
            Assert.AreEqual(1, result.Count);

            var item = result.Pop();
            Assert.IsInstanceOfType(item, typeof(InteropInterface));
        }

        [TestMethod]
        public void createAndCallFromSyscallTest()
        {
            _engine.Reset();
            var result = _engine.ExecuteTestCaseStandard("createAndCallFromSyscall", (uint)SyscallCallback.System_Blockchain_GetHeight);
            Assert.AreEqual(VMState.HALT, _engine.State);
            Assert.AreEqual(1, result.Count);

            var item = result.Pop<Integer>();
            Assert.AreEqual(1234, item.GetInteger());
        }

        [TestMethod]
        public void createTest()
        {
            _engine.Reset();
            var result = _engine.ExecuteTestCaseStandard("create");
            Assert.AreEqual(VMState.HALT, _engine.State);
            Assert.AreEqual(1, result.Count);

            var item = result.Pop();
            Assert.IsInstanceOfType(item, typeof(InteropInterface));
        }

        [TestMethod]
        public void createAndCallTest()
        {
            _engine.Reset();
            var result = _engine.ExecuteTestCaseStandard("createAndCall");
            Assert.AreEqual(VMState.HALT, _engine.State);
            Assert.AreEqual(1, result.Count);

            var item = result.Pop<Integer>();
            Assert.AreEqual(123, item.GetInteger());
        }
    }
}
