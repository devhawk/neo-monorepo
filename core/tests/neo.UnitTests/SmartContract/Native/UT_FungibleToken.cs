using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_FungibleToken : TestKit
    {
        protected const byte Prefix_TotalSupply = 11;
        private static readonly TestNep17Token test = new TestNep17Token();

        [TestMethod]
        public void TestName()
        {
            Assert.AreEqual(test.Name, test.Manifest.Name);
        }

        [TestMethod]
        public void TestTotalSupply()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            StorageItem item = new StorageItem
            {
                Value = new byte[] { 0x01 }
            };
            var key = CreateStorageKey(Prefix_TotalSupply);

            key.Id = test.Id;

            snapshot.Add(key, item);
            test.TotalSupply(snapshot).Should().Be(1);
        }

        [TestMethod]
        public void TestTotalSupplyDecimal()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            BigInteger totalSupply = 100_000_000;
            totalSupply *= test.Factor;

            StorageItem item = new StorageItem
            {
                Value = totalSupply.ToByteArrayStandard()
            };
            var key = CreateStorageKey(Prefix_TotalSupply);

            key.Id = test.Id;

            snapshot.Add(key, item);

            test.TotalSupply(snapshot).Should().Be(10_000_000_000_000_000);
        }

        public StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                Id = 0,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            key?.CopyTo(storageKey.Key.AsSpan(1));
            return storageKey;
        }
    }

    public class TestNep17Token : FungibleToken<NeoToken.NeoAccountState>
    {
        public override string Symbol => throw new NotImplementedException();
        public override byte Decimals => 8;
    }
}
