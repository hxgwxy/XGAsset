using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XGAsset.Runtime
{
    public struct ProgressStatus
    {
        public int Id;
        public float Percent;
        public ulong CompletedBytes;
        public ulong TotalBytes;
        public bool IsValid;
    }
}