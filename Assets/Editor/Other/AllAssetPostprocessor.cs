using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using XGAsset.Editor.Settings;

namespace XGAsset.Editor.Other
{
    public class AllAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var savePackages = new List<string>();

            // 处理移动目录
            foreach (var path in movedFromAssetPaths.Where(v => !Path.HasExtension(v)))
            {
                var group = AssetAddressDefaultSettings.GetGroup(path);
                if (group != null)
                {
                    var newPath = AssetDatabase.GUIDToAssetPath(group.Guid);
                    group.FolderPath = newPath;
                    savePackages.Add(group.PackageName);
                }
            }

            // 处理移动的或者导入的资源
            var moveOrImports = importedAssets.Union(movedAssets).Distinct().ToList();
            foreach (var assetPath in moveOrImports)
            {
                var entry = AssetAddressDefaultSettings.GetEntry(AssetDatabase.AssetPathToGUID(assetPath));
                if (entry != null)
                {
                    entry.AssetPath = assetPath;
                    var group = AssetAddressDefaultSettings.GetGroup(entry);
                    savePackages.Add(group.PackageName);
                    group.GenerateCache();
                }
            }

            // 处理删除的资源
            foreach (var assetPath in deletedAssets)
            {
                var groupName = AssetAddressDefaultSettings.GetEntry(assetPath)?.GroupName;
                if (!string.IsNullOrEmpty(groupName))
                {
                    savePackages.Add(AssetAddressDefaultSettings.GetGroup(groupName).PackageName);
                    AssetAddressDefaultSettings.RemoveEntry(assetPath);
                }
                else
                {
                    var packageName = AssetAddressDefaultSettings.GetGroup(assetPath)?.PackageName;
                    if (!string.IsNullOrEmpty(packageName))
                    {
                        savePackages.Add(packageName);
                        AssetAddressDefaultSettings.RemoveGroup(assetPath);
                    }
                }
            }

            savePackages = savePackages.Distinct().ToList();
            foreach (var packageName in savePackages)
            {
                AssetAddressDefaultSettings.SavePackage(packageName);
            }
        }
    }
}