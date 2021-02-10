using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public sealed class Header : IEquatable<Header>, IVerifiable
    {
        public uint Version;
        public UInt256 PrevHash;
        public UInt256 MerkleRoot;
        public ulong Timestamp;
        public uint Index;
        public byte PrimaryIndex;
        public UInt160 NextConsensus;
        public Witness Witness;

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

        public int Size =>
            sizeof(uint) +      // Version
            UInt256.Length +    // PrevHash
            UInt256.Length +    // MerkleRoot
            sizeof(ulong) +     // Timestamp
            sizeof(uint) +      // Index
            sizeof(byte) +      // PrimaryIndex
            UInt160.Length +    // NextConsensus
            1 + Witness.Size;   // Witness   

        Witness[] IVerifiable.Witnesses
        {
            get
            {
                return new[] { Witness };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException();
                Witness = value[0];
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Witness[] witnesses = reader.ReadSerializableArray<Witness>(1);
            if (witnesses.Length != 1) throw new FormatException();
            Witness = witnesses[0];
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            if (Version > 0) throw new FormatException();
            PrevHash = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt64();
            Index = reader.ReadUInt32();
            PrimaryIndex = reader.ReadByte();
            NextConsensus = reader.ReadSerializable<UInt160>();
        }

        public bool Equals(Header other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Header);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(DataCache snapshot)
        {
            if (PrevHash == UInt256.Zero) return new[] { Witness.ScriptHash };
            TrimmedBlock prev = NativeContract.Ledger.GetTrimmedBlock(snapshot, PrevHash);
            if (prev is null) throw new InvalidOperationException();
            return new[] { prev.Header.NextConsensus };
        }

        public void Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(new Witness[] { Witness });
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Index);
            writer.Write(PrimaryIndex);
            writer.Write(NextConsensus);
        }

        public JObject ToJson(ProtocolSettings settings)
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["previousblockhash"] = PrevHash.ToString();
            json["merkleroot"] = MerkleRoot.ToString();
            json["time"] = Timestamp;
            json["index"] = Index;
            json["primary"] = PrimaryIndex;
            json["nextconsensus"] = NextConsensus.ToAddress(settings.AddressVersion);
            json["witnesses"] = new JArray(Witness.ToJson());
            return json;
        }

        internal bool Verify(ProtocolSettings settings, DataCache snapshot)
        {
            if (PrimaryIndex >= settings.ValidatorsCount)
                return false;
            TrimmedBlock prev = NativeContract.Ledger.GetTrimmedBlock(snapshot, PrevHash);
            if (prev is null) return false;
            if (prev.Index + 1 != Index) return false;
            if (prev.Header.Timestamp >= Timestamp) return false;
            if (!this.VerifyWitnesses(settings, snapshot, 1_00000000)) return false;
            return true;
        }

        internal bool Verify(ProtocolSettings settings, DataCache snapshot, HeaderCache headerCache)
        {
            Header prev = headerCache.Last;
            if (prev is null) return Verify(settings, snapshot);
            if (PrimaryIndex >= settings.ValidatorsCount)
                return false;
            if (prev.Hash != PrevHash) return false;
            if (prev.Index + 1 != Index) return false;
            if (prev.Timestamp >= Timestamp) return false;
            return this.VerifyWitness(settings, snapshot, prev.NextConsensus, Witness, 1_00000000, out _);
        }
    }
}
