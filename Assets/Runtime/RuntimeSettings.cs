using System;
using System.Collections.Generic;
using UnityEngine;

namespace XGAsset.Runtime
{
    [Serializable]
    public class PackageSetting
    {
        public string PackageName = string.Empty;
        public string RemoteLoadPath = string.Empty;
    }

    public class RuntimeSettings : ScriptableObject
    {
        [SerializeField]
        public List<PackageSetting> PackageSettings = new List<PackageSetting>();
    }
}