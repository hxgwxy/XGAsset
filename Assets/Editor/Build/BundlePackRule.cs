using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using XGAsset.Editor.Settings;
using XGAsset.Utility;
using XGFramework.XGAsset.Editor.Other;

namespace XGAsset.Editor.Build
{
    /// <summary>
    /// 一起打包
    /// </summary>
    public class PackTogether : IBundlePackRule
    {
        public List<BundleBuildLayout> CreateAssetBundleBuildLayouts(AssetAddressGroupInfo group)
        {
            var layouts = new List<BundleBuildLayout>();
            var assetLayout = new BundleBuildLayout();
            assetLayout.GroupGuid = group.Guid;
            assetLayout.BundleName = group.GroupName + "_asset" + AssetAddressDefaultSettings.BundleSuffix;
            assetLayout.CopyToStreamingAssets = group.CopyToStreamingAssets;
            assetLayout.AllRefAssets.AddRange(AssetUtilityEditor.GetDependencies(group.Entries.Where(entry => entry.Active).Select(entry => entry.AssetPath).ToList()));

            if (assetLayout.AllRefAssets.Count > 0)
            {
                var sceneEntries = assetLayout.AllRefAssets.Where(v => AssetDatabase.GetMainAssetTypeAtPath(v) == typeof(SceneAsset)).ToList();

                if (sceneEntries.Count > 0)
                {
                    var sceneLayout = new BundleBuildLayout();
                    sceneLayout.BundleName = group.GroupName + AssetAddressDefaultSettings.BundleSuffix;
                    sceneLayout.CopyToStreamingAssets = group.CopyToStreamingAssets;
                    sceneLayout.AllRefAssets.AddRange(assetLayout.AllRefAssets.Where(v => AssetDatabase.GetMainAssetTypeAtPath(v) == typeof(SceneAsset)));
                    sceneLayout.GroupGuid = group.Guid;
                    sceneLayout.SceneBundle = true;

                    assetLayout.AllRefAssets = assetLayout.AllRefAssets.Except(sceneLayout.AllRefAssets).ToList();

                    if (sceneLayout.AllRefAssets.Count > 0)
                        layouts.Add(sceneLayout);
                }

                if (assetLayout.AllRefAssets.Count > 0)
                    layouts.Add(assetLayout);
            }

            return layouts;
        }
    }

    /// <summary>
    /// 分别打包
    /// </summary>
    public class PackSeparately : IBundlePackRule
    {
        public List<BundleBuildLayout> CreateAssetBundleBuildLayouts(AssetAddressGroupInfo group)
        {
            var layouts = new List<BundleBuildLayout>();
            var newGroups = new List<AssetAddressGroupInfo>();
            foreach (var entry in group.Entries)
            {
                if (!entry.Active)
                    continue;
                var newGroup = new AssetAddressGroupInfo();
                var name = Path.GetFileNameWithoutExtension(entry.Address) ?? string.Empty;
                newGroup.GroupName = $"{group.GroupName}_{name}";
                newGroup.AddAssetEntry(entry.AssetPath);
                newGroup.Guid = group.Guid;
                newGroup.CopyToStreamingAssets = group.CopyToStreamingAssets;
                newGroups.Add(newGroup);
            }

            var rule = new PackTogether();
            foreach (var group1 in newGroups)
            {
                layouts.AddRange(rule.CreateAssetBundleBuildLayouts(group1));
            }

            return layouts;
        }
    }

    /// <summary>
    /// 按照资源类型打包
    /// </summary>
    public class PackTogetherByType : IBundlePackRule
    {
        public List<BundleBuildLayout> CreateAssetBundleBuildLayouts(AssetAddressGroupInfo group)
        {
            var groups = group.Entries.GroupBy(v => AssetDatabase.GetMainAssetTypeAtPath(v.AssetPath).Name);
            var newGroups = new List<AssetAddressGroupInfo>();
            foreach (var group1 in groups)
            {
                var newGroup = new AssetAddressGroupInfo();
                newGroup.GroupName = $"{group.GroupName}_{group1.Key}";
                newGroup.CopyToStreamingAssets = group.CopyToStreamingAssets;
                newGroup.Guid = group.Guid;
                foreach (var entry in group1)
                {
                    if (entry.Active) newGroup.AddAssetEntry(entry.AssetPath);
                }

                if (newGroup.Entries.Count > 0)
                    newGroups.Add(newGroup);
            }

            var layouts = new List<BundleBuildLayout>();
            var rule = new PackTogether();
            foreach (var group1 in newGroups)
            {
                layouts.AddRange(rule.CreateAssetBundleBuildLayouts(group1));
            }

            return layouts;
        }
    }
}