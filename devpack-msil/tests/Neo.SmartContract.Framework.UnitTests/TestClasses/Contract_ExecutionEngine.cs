using Neo.SmartContract.Framework.Services;

namespace Neo.Compiler.MSIL.TestClasses
{
    public class Contract_ExecutionEngine : SmartContract.Framework.SmartContract
    {
        public static byte[] CallingScriptHash()
        {
            return (byte[])Runtime.CallingScriptHash;
        }

        public static byte[] EntryScriptHash()
        {
            return (byte[])Runtime.EntryScriptHash;
        }

        public static byte[] ExecutingScriptHash()
        {
            return (byte[])Runtime.ExecutingScriptHash;
        }

        public static object ScriptContainer()
        {
            return Runtime.ScriptContainer;
        }
    }
}
