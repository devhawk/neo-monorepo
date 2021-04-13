using System;

namespace Neo.SmartContract.Framework
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ContractPermissionAttribute : Attribute
    {
        public string Contract { get; set; }
        public string[] Methods { get; set; }

        public ContractPermissionAttribute(string contract, params string[] methods)
        {
            Contract = contract;
            Methods = methods;
        }
    }
}
