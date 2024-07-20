using System.Collections.Generic;
using UnityEditor.Build.Pipeline.Interfaces;
using XGAsset.Runtime.Misc;
using XGAsset.Editor.Settings;

namespace XGAsset.Editor.Build
{
    public class IAssetBuildContext : IContextObject
    {
    }

    public class AssetBuildContext : IAssetBuildContext
    {
        public Dictionary<string, string> BundleNamesMap;
        public AssetAddressPackage CurrPackage;
        public List<BundleBuildLayout> CustomBundleLayouts;
        public ManifestData ManifestData;
    }
}