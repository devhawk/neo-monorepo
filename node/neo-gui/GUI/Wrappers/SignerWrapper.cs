using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using System.Collections.Generic;
using System.ComponentModel;

namespace Neo.GUI.Wrappers
{
    internal class SignerWrapper
    {
        [TypeConverter(typeof(UIntBaseConverter))]
        public UInt160 Account { get; set; }
        public WitnessScope Scopes { get; set; }
        public List<UInt160> AllowedContracts { get; set; } = new List<UInt160>();
        public List<ECPoint> AllowedGroups { get; set; } = new List<ECPoint>();

        public Signer Unwrap()
        {
            return new Signer
            {
                Account = Account,
                Scopes = Scopes,
                AllowedContracts = AllowedContracts.ToArray(),
                AllowedGroups = AllowedGroups.ToArray()
            };
        }
    }
}
