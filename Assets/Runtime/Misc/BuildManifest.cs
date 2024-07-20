using System;
using System.Collections.Generic;

namespace XGAsset.Runtime.Misc
{
    [Serializable]
    public class BundleInfo
    {
        public string Name;
        public string MD5;
        public ulong Size;
        public string SizeStr;
        public string Crc;
        public uint BundleCrc;
        public string[] Dependencies; // 直接依赖的bundle
        public string[] IndirectDependencies; // 间接依赖的bundle
        public string[] IncludeAssets; // bundle包含的资源
        public string[] ReferenceAssets; // 引用其他bundle资源列表
    }

    [Serializable]
    public class AddressInfo
    {
        public string Address;
        public string[] Label;
        public string AssetPath;
        public string BundleName;
        public string PackageName;
    }

    [Serializable]
    public class ManifestData
    {
        public string PackageName;
        public string Version;
        public List<AddressInfo> AddressInfos;
        public List<BundleInfo> BundleInfos;
        public string LoadPath = string.Empty;
    }
}