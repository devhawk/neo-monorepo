using System;

namespace Neo.SmartContract.Framework.Services
{
    [Flags]
    public enum FindOptions : byte
    {
        None = 0,

        KeysOnly = 1 << 0,
        RemovePrefix = 1 << 1,
        ValuesOnly = 1 << 2,
        DeserializeValues = 1 << 3,
        PickField0 = 1 << 4,
        PickField1 = 1 << 5
    }
}
