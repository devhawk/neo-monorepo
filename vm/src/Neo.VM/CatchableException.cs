// Copyright (C) 2016-2022 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.VM
{
    public class CatchableException : Exception
    {
        public CatchableException(string message) : base(message)
        {
        }
    }
}
