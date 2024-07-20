using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using XGAsset.Editor.Settings;
using XGAsset.Utility;
using XGAsset.Editor.Other;

namespace XGAsset.Editor.Build
{
    public interface IBundlePackRule
    {
        public List<BundleBuildLayout> CreateAssetBundleBuildLayouts(AssetAddressGroupInfo group);
    }
}