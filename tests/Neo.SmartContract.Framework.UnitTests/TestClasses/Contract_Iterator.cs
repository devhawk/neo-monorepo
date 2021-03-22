using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;

namespace Neo.Compiler.MSIL.TestClasses
{
    public class Contract_Iterator : SmartContract.Framework.SmartContract
    {
        public static int TestNextByteArray(byte[] a)
        {
            int sum = 0;
            var iterator = Iterator.Create<byte>(a);

            while (iterator.Next())
            {
                sum += iterator.Value;
            }

            return sum;
        }

        public static int TestNextIntArray(int[] a)
        {
            int sum = 0;
            var iterator = Iterator.Create<int>(a);

            while (iterator.Next())
            {
                sum += iterator.Value;
            }

            return sum;
        }

        public static int TestNextIntArrayBase(int[] a)
        {
            int sum = 0;
            var iterator = (Iterator)Iterator.Create<int>(a);

            while (iterator.Next())
            {
                sum += (int)iterator.Value;
            }

            return sum;
        }
    }
}
