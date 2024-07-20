using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XGAsset.Runtime
{
    public struct ProgressStatus
    {
        public int id;
        public float Percent => TotalBytes == 0 ? 0 : (float)CompletedBytes / (float)TotalBytes;
        public ulong CompletedBytes;
        public ulong TotalBytes;
        public bool IsValid;
    }
}