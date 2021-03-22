using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;

namespace Neo.Compiler.MSIL.TestClasses
{
    public class Contract_CreateAndUpdate : SmartContract.Framework.SmartContract
    {
        public static int OldContract(byte[] nefFile, string manifest)
        {
            ContractManagement.Update((ByteString)nefFile, manifest, null);
            return 123;
        }
    }
}
