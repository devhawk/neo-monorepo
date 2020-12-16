using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Compiler.MSIL.Extensions;
using Neo.Compiler.MSIL.UnitTests.Utils;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.VM.Types;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Template.NEP17.UnitTests
{
    [TestClass]
    public class UnitTest_NEP5
    {
        private TestEngine _engine;
        private static readonly byte[] _prefixAsset = Encoding.UTF8.GetBytes("asset");
        private static readonly byte[] _prefixContract = Encoding.UTF8.GetBytes("contract");

        [TestInitialize]
        public void Init()
        {
            _engine = CreateEngine();

        }

        TestEngine CreateEngine()
        {
            var engine = new TestEngine();
            engine.AddEntryScript(new string[]
            {
                "../../../../../templates/Template.NEP17.CSharp/NEP17.cs",
                "../../../../../templates/Template.NEP17.CSharp/Storage/TotalSupplyStorage.cs",
                "../../../../../templates/Template.NEP17.CSharp/Storage/AssetStorage.cs",
                "../../../../../templates/Template.NEP17.CSharp/NEP17.Owner.cs",
                "../../../../../templates/Template.NEP17.CSharp/NEP17.Crowdsale.cs",
                "../../../../../templates/Template.NEP17.CSharp/NEP17.Helpers.cs",
                "../../../../../templates/Template.NEP17.CSharp/NEP17.Methods.cs"
            });

            return engine;
        }

        [TestMethod]
        public void Test_name()
        {
            Assert.AreEqual("Token Name", JObject.Parse(_engine.ScriptEntry.finalManifest)["name"].AsString());
        }

        [TestMethod]
        public void Test_symbol()
        {
            var result = _engine.ExecuteTestCaseStandard("symbol");
            Assert.AreEqual(1, result.Count);

            var item = result.Pop();
            Assert.IsInstanceOfType(item, typeof(ByteString));
            Assert.AreEqual("TokenSymbol", item.GetString());
        }

        [TestMethod]
        public void Test_decimals()
        {
            var result = _engine.ExecuteTestCaseStandard("decimals");
            Assert.AreEqual(1, result.Count);

            var item = result.Pop();
            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(8, item.GetInteger());
        }

        [TestMethod]
        public void Test_totalSupply()
        {
            var engine = CreateEngine();
            var hash = engine.CurrentScriptHash;
            var snapshot = engine.Snapshot;

            snapshot.ContractAdd(new ContractState()
            {
                Hash = hash,
                Manifest = new Neo.SmartContract.Manifest.ContractManifest()
            });

            snapshot.Storages.Add(new Neo.Ledger.StorageKey()
            {
                Id = 0,
                Key = _prefixContract.Concat(Encoding.ASCII.GetBytes("totalSupply")).ToArray()
            },
            new Neo.Ledger.StorageItem()
            {
                IsConstant = false,
                Value = new BigInteger(123).ToByteArray()
            });

            var result = engine.ExecuteTestCaseStandard("totalSupply");
            Assert.AreEqual(1, result.Count);

            var item = result.Pop();
            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(123, item.GetInteger());
        }

        [TestMethod]
        public void Test_totalSupply_empty()
        {
            var engine = CreateEngine();
            var hash = engine.CurrentScriptHash;
            var snapshot = engine.Snapshot;

            snapshot.ContractAdd(new ContractState()
            {
                Hash = hash,
                Manifest = new Neo.SmartContract.Manifest.ContractManifest()
            });

            var result = engine.ExecuteTestCaseStandard("totalSupply");
            Assert.AreEqual(1, result.Count);

            var item = result.Pop();
            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(0, item.GetInteger());
        }

        [TestMethod]
        public void Test_balanceOf()
        {
            var engine = CreateEngine();
            var hash = engine.CurrentScriptHash;
            var snapshot = engine.Snapshot;
            var address = new byte[] { 0xf6, 0x64, 0x43, 0x49, 0x8d, 0x38, 0x78, 0xd3, 0x2b, 0x99, 0x4e, 0x4e, 0x12, 0x83, 0xc6, 0x93, 0x44, 0x21, 0xda, 0xfe };

            snapshot.ContractAdd(new ContractState()
            {
                Hash = hash,
                Manifest = new Neo.SmartContract.Manifest.ContractManifest()
            });

            snapshot.Storages.Add(new Neo.Ledger.StorageKey()
            {
                Id = 0,
                Key = _prefixAsset.Concat(address).ToArray()
            },
            new Neo.Ledger.StorageItem()
            {
                IsConstant = false,
                Value = new BigInteger(321).ToByteArray()
            });

            var result = engine.ExecuteTestCaseStandard("balanceOf", address);
            Assert.AreEqual(1, result.Count);

            var item = result.Pop();
            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(321, item.GetInteger());
        }

        [TestMethod]
        public void Test_balanceOf_empty()
        {
            var engine = CreateEngine();
            var hash = engine.CurrentScriptHash;
            var snapshot = engine.Snapshot;
            var address = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13 };

            snapshot.ContractAdd(new ContractState()
            {
                Hash = hash,
                Manifest = new Neo.SmartContract.Manifest.ContractManifest()
            });

            var result = engine.ExecuteTestCaseStandard("balanceOf", address);
            Assert.AreEqual(1, result.Count);

            var item = result.Pop();
            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(0, item.GetInteger());
        }
    }
}
