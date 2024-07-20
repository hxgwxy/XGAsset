using System;
using XGAsset.Utility;

namespace XGAsset.Editor.Other
{
    public interface ICollectRule
    {
        public bool IsCollect(AssetAddressGroupInfo groupInfo, string assetPath);
    }

    public class CollectRule : ICollectRule
    {
        public bool IsCollect(AssetAddressGroupInfo groupInfo, string assetPath)
        {
            throw new System.NotImplementedException();
        }
    }

    public class MapCollectRule : ICollectRule
    {
        public bool IsCollect(AssetAddressGroupInfo groupInfo, string assetPath)
        {
            return assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }
    }
}