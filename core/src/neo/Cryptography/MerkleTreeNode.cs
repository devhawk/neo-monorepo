// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography
{
    internal class MerkleTreeNode
    {
        public UInt256 Hash;
        public MerkleTreeNode Parent;
        public MerkleTreeNode LeftChild;
        public MerkleTreeNode RightChild;

        public bool IsLeaf => LeftChild == null && RightChild == null;

        public bool IsRoot => Parent == null;
    }
}
