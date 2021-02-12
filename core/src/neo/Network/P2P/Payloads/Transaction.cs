using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Neo.SmartContract.Helper;
using Array = Neo.VM.Types.Array;

namespace Neo.Network.P2P.Payloads
{
    public class Transaction : IEquatable<Transaction>, IInventory, IInteroperable
    {
        public const int MaxTransactionSize = 102400;
        public const uint MaxValidUntilBlockIncrement = 5760; // 24 hour
        /// <summary>
        /// Maximum number of attributes that can be contained within a transaction
        /// </summary>
        public const int MaxTransactionAttributes = 16;

        private byte version;
        private uint nonce;
        private long sysfee;
        private long netfee;
        private uint validUntilBlock;
        private Signer[] _signers;
        private TransactionAttribute[] attributes;
        private byte[] script;
        private Witness[] witnesses;

        public const int HeaderSize =
            sizeof(byte) +  //Version
            sizeof(uint) +  //Nonce
            sizeof(long) +  //SystemFee
            sizeof(long) +  //NetworkFee
            sizeof(uint);   //ValidUntilBlock

        private Dictionary<Type, TransactionAttribute[]> _attributesCache;
        public TransactionAttribute[] Attributes
        {
            get => attributes;
            set { attributes = value; _attributesCache = null; _hash = null; _size = 0; }
        }

        /// <summary>
        /// The <c>NetworkFee</c> for the transaction divided by its <c>Size</c>.
        /// <para>Note that this property must be used with care. Getting the value of this property multiple times will return the same result. The value of this property can only be obtained after the transaction has been completely built (no longer modified).</para>
        /// </summary>
        public long FeePerByte => NetworkFee / Size;

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = this.CalculateHash();
                }
                return _hash;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.TX;

        /// <summary>
        /// Distributed to consensus nodes.
        /// </summary>
        public long NetworkFee
        {
            get => netfee;
            set { netfee = value; _hash = null; }
        }

        public uint Nonce
        {
            get => nonce;
            set { nonce = value; _hash = null; }
        }

        public byte[] Script
        {
            get => script;
            set { script = value; _hash = null; _size = 0; }
        }

        /// <summary>
        /// The first signer is the sender of the transaction, regardless of its WitnessScope.
        /// The sender will pay the fees of the transaction.
        /// </summary>
        public UInt160 Sender => _signers[0].Account;

        public Signer[] Signers
        {
            get => _signers;
            set { _signers = value; _hash = null; _size = 0; }
        }

        private int _size;
        public int Size
        {
            get
            {
                if (_size == 0)
                {
                    _size = HeaderSize +
                        Signers.GetVarSize() +      // Signers
                        Attributes.GetVarSize() +   // Attributes
                        Script.GetVarSize() +       // Script
                        Witnesses.GetVarSize();     // Witnesses
                }
                return _size;
            }
        }

        /// <summary>
        /// Fee to be burned.
        /// </summary>
        public long SystemFee
        {
            get => sysfee;
            set { sysfee = value; _hash = null; }
        }

        public uint ValidUntilBlock
        {
            get => validUntilBlock;
            set { validUntilBlock = value; _hash = null; }
        }

        public byte Version
        {
            get => version;
            set { version = value; _hash = null; }
        }

        public Witness[] Witnesses
        {
            get => witnesses;
            set { witnesses = value; _size = 0; }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            int startPosition = -1;
            if (reader.BaseStream.CanSeek)
                startPosition = (int)reader.BaseStream.Position;
            DeserializeUnsigned(reader);
            Witnesses = reader.ReadSerializableArray<Witness>(Signers.Length);
            if (Witnesses.Length != Signers.Length) throw new FormatException();
            if (startPosition >= 0)
                _size = (int)reader.BaseStream.Position - startPosition;
        }

        private static IEnumerable<TransactionAttribute> DeserializeAttributes(BinaryReader reader, int maxCount)
        {
            int count = (int)reader.ReadVarInt((ulong)maxCount);
            HashSet<TransactionAttributeType> hashset = new HashSet<TransactionAttributeType>();
            while (count-- > 0)
            {
                TransactionAttribute attribute = TransactionAttribute.DeserializeFrom(reader);
                if (!attribute.AllowMultiple && !hashset.Add(attribute.Type))
                    throw new FormatException();
                yield return attribute;
            }
        }

        private static IEnumerable<Signer> DeserializeSigners(BinaryReader reader, int maxCount)
        {
            int count = (int)reader.ReadVarInt((ulong)maxCount);
            if (count == 0) throw new FormatException();
            HashSet<UInt160> hashset = new HashSet<UInt160>();
            for (int i = 0; i < count; i++)
            {
                Signer signer = reader.ReadSerializable<Signer>();
                if (!hashset.Add(signer.Account)) throw new FormatException();
                yield return signer;
            }
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadByte();
            if (Version > 0) throw new FormatException();
            Nonce = reader.ReadUInt32();
            SystemFee = reader.ReadInt64();
            if (SystemFee < 0) throw new FormatException();
            NetworkFee = reader.ReadInt64();
            if (NetworkFee < 0) throw new FormatException();
            if (SystemFee + NetworkFee < SystemFee) throw new FormatException();
            ValidUntilBlock = reader.ReadUInt32();
            Signers = DeserializeSigners(reader, MaxTransactionAttributes).ToArray();
            Attributes = DeserializeAttributes(reader, MaxTransactionAttributes - Signers.Length).ToArray();
            Script = reader.ReadVarBytes(ushort.MaxValue);
            if (Script.Length == 0) throw new FormatException();
        }

        public bool Equals(Transaction other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Transaction);
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        public T GetAttribute<T>() where T : TransactionAttribute
        {
            return GetAttributes<T>().FirstOrDefault();
        }

        public IEnumerable<T> GetAttributes<T>() where T : TransactionAttribute
        {
            _attributesCache ??= attributes.GroupBy(p => p.GetType()).ToDictionary(p => p.Key, p => p.ToArray());
            if (_attributesCache.TryGetValue(typeof(T), out var result))
                return result.OfType<T>();
            return Enumerable.Empty<T>();
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public UInt160[] GetScriptHashesForVerifying(DataCache snapshot)
        {
            return Signers.Select(p => p.Account).ToArray();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(Witnesses);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Nonce);
            writer.Write(SystemFee);
            writer.Write(NetworkFee);
            writer.Write(ValidUntilBlock);
            writer.Write(Signers);
            writer.Write(Attributes);
            writer.WriteVarBytes(Script);
        }

        public JObject ToJson(ProtocolSettings settings)
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["nonce"] = Nonce;
            json["sender"] = Sender.ToAddress(settings.AddressVersion);
            json["sysfee"] = SystemFee.ToString();
            json["netfee"] = NetworkFee.ToString();
            json["validuntilblock"] = ValidUntilBlock;
            json["signers"] = Signers.Select(p => p.ToJson()).ToArray();
            json["attributes"] = Attributes.Select(p => p.ToJson()).ToArray();
            json["script"] = Convert.ToBase64String(Script);
            json["witnesses"] = Witnesses.Select(p => p.ToJson()).ToArray();
            return json;
        }

        public virtual VerifyResult VerifyStateDependent(ProtocolSettings settings, DataCache snapshot, TransactionVerificationContext context)
        {
            uint height = NativeContract.Ledger.CurrentIndex(snapshot);
            if (ValidUntilBlock <= height || ValidUntilBlock > height + MaxValidUntilBlockIncrement)
                return VerifyResult.Expired;
            UInt160[] hashes = GetScriptHashesForVerifying(snapshot);
            foreach (UInt160 hash in hashes)
                if (NativeContract.Policy.IsBlocked(snapshot, hash))
                    return VerifyResult.PolicyFail;
            if (!(context?.CheckTransaction(this, snapshot) ?? true)) return VerifyResult.InsufficientFunds;
            foreach (TransactionAttribute attribute in Attributes)
                if (!attribute.Verify(snapshot, this))
                    return VerifyResult.Invalid;
            long net_fee = NetworkFee - Size * NativeContract.Policy.GetFeePerByte(snapshot);
            if (net_fee < 0) return VerifyResult.InsufficientFunds;

            if (net_fee > MaxVerificationGas) net_fee = MaxVerificationGas;
            uint execFeeFactor = NativeContract.Policy.GetExecFeeFactor(snapshot);
            for (int i = 0; i < hashes.Length; i++)
            {
                if (witnesses[i].VerificationScript.IsSignatureContract())
                    net_fee -= execFeeFactor * SignatureContractCost();
                else if (witnesses[i].VerificationScript.IsMultiSigContract(out int m, out int n))
                    net_fee -= execFeeFactor * MultiSignatureContractCost(m, n);
                else
                {
                    if (!this.VerifyWitness(settings, snapshot, hashes[i], witnesses[i], net_fee, out long fee))
                        return VerifyResult.Invalid;
                    net_fee -= fee;
                }
                if (net_fee < 0) return VerifyResult.InsufficientFunds;
            }
            return VerifyResult.Succeed;
        }

        public virtual VerifyResult VerifyStateIndependent(ProtocolSettings settings)
        {
            if (Size > MaxTransactionSize) return VerifyResult.Invalid;
            try
            {
                _ = new Script(Script, true);
            }
            catch (BadScriptException)
            {
                return VerifyResult.Invalid;
            }
            long net_fee = Math.Min(NetworkFee, MaxVerificationGas);
            UInt160[] hashes = GetScriptHashesForVerifying(null);
            for (int i = 0; i < hashes.Length; i++)
                if (witnesses[i].VerificationScript.IsStandardContract())
                    if (!this.VerifyWitness(settings, null, hashes[i], witnesses[i], net_fee, out long fee))
                        return VerifyResult.Invalid;
                    else
                    {
                        net_fee -= fee;
                        if (net_fee < 0) return VerifyResult.InsufficientFunds;
                    }
            return VerifyResult.Succeed;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[]
            {
                // Computed properties
                Hash.ToArray(),

                // Transaction properties
                (int)Version,
                Nonce,
                Sender.ToArray(),
                SystemFee,
                NetworkFee,
                ValidUntilBlock,
                Script,
            });
        }
    }
}
