using Neo.SmartContract.Framework.Services.Neo;

namespace Neo.Compiler.MSIL.TestClasses
{
    public class Contract_CreateAndUpdate : SmartContract.Framework.SmartContract
    {
        public static int OldContract(byte[] nefFile, string manifest)
        {
            ContractManagement.Update(nefFile, manifest);
            return 123;
        }
    }
}
