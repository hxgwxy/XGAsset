using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace XGAsset.Editor.Build
{
    public class AssetStruct
    {
        public string AssetPath;
        public string Address;
    }

    public class BundleBuildLayout
    {
        public string BundleName;
        public string MainType;
        public string GroupGuid;
        public bool CopyToStreamingAssets;
        public bool SceneBundle;
        public List<string> AllRefAssets = new List<string>();
    }
}