using System;

namespace Neo.SmartContract.Framework
{
    // MONOREPO PATCH: ManifestNameAttribute
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ManifestNameAttribute : Attribute
    {
        public string Value { get; set; }

        public ManifestNameAttribute(string value)
        {
            Value = value;
        }
    }
}