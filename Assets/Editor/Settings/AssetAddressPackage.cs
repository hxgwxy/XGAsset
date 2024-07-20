using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XGAsset.Utility;

namespace XGFramework.XGAsset.Editor.Settings
{
    [CreateAssetMenu(fileName = "AssetAddressResPackage", menuName = "XGAsset/Create AssetBundle Package Settings")]
    public class AssetAddressPackage : ScriptableObject
    {
        [SerializeField]
        private string packageName = string.Empty;

        [SerializeField]
        private string version = string.Empty;

        [SerializeField]
        private string buildPath = string.Empty;

        [SerializeField]
        private string loadPath = string.Empty;

        public string PackageName
        {
            get => packageName;
            set => packageName = value;
        }

        public string Version
        {
            get => version;
            set => version = value;
        }

        public string BuildPath
        {
            get => buildPath;
            set => buildPath = value;
        }

        public string LoadPath
        {
            get => loadPath;
            set => loadPath = value;
        }

        [SerializeField]
        private List<AssetAddressGroupInfo> groups = new List<AssetAddressGroupInfo>();

        private Dictionary<string, AssetAddressGroupInfo> groupsCache = new Dictionary<string, AssetAddressGroupInfo>();

        private AssetAddressGroupInfo GetGroupFromCache(string folderPath)
        {
            if (!groupsCache.TryGetValue(folderPath, out var group1))
            {
                foreach (var group in groups)
                {
                    groupsCache[group.FolderPath] = group;
                    groupsCache[group.GroupName] = group;
                }
            }

            groupsCache.TryGetValue(folderPath, out var group2);
            return group2;
        }

        public List<AssetAddressGroupInfo> Groups => groups;

        private string ReplaceOnce(string str, string oldStr, string newStr)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            var index = str.IndexOf(oldStr, StringComparison.Ordinal);
            return index >= 0 ? str.Substring(index + oldStr.Length, str.Length - oldStr.Length - index) : str;
        }

        public AssetAddressGroupInfo AddGroup(string folderPath, string groupName = "")
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"{folderPath}:不是有效的目录");
                return null;
            }

            if (string.IsNullOrEmpty(groupName))
            {
                groupName = ReplaceOnce(folderPath.Replace("\\", "/").Replace("/", "_"), "Assets_", "");
                groupName = ReplaceOnce(groupName, "GameAssets_", "");
            }

            var group = GetGroup(folderPath);
            if (group == null)
            {
                group = new AssetAddressGroupInfo
                {
                    PackageName = PackageName,
                    FolderPath = folderPath,
                    GroupName = groupName,
                    Guid = AssetDatabase.AssetPathToGUID(folderPath),
                };
                groups.Add(group);
                groups.Sort((a, b) => string.CompareOrdinal(a.FolderPath, b.FolderPath));
            }

            return group;
        }

        public AssetAddressGroupInfo GetGroup(string folderPath)
        {
            return GetGroupFromCache(folderPath);
        }

        public AssetAddressGroupInfo GetMatchGroup(string folderPath)
        {
            return groups.FindLast(v => folderPath.StartsWith(v.FolderPath));
        }

        public void RemoveGroup(string folderPath)
        {
            var group = groups.Find(v => v.FolderPath.Equals(folderPath));
            groups.Remove(group);
            groupsCache.Remove(group.FolderPath);
        }

        public AssetAddressEntry GetAssetInfo(string assetPath)
        {
            foreach (var group in Groups)
            {
                var asset = group.GetAssetInfo(assetPath);
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }

        public List<AssetAddressEntry> GetAssetInfos(string assetPath)
        {
            List<AssetAddressEntry> list = null;
            foreach (var group in Groups)
            {
                var assets = group.GetAssetInfos(assetPath);
                if (assets != null && assets.Count > 0)
                {
                    list ??= new List<AssetAddressEntry>();
                    list.AddRange(assets);
                }
            }

            return list;
        }

        public AssetAddressEntry AddAssetInfo(string assetPath, string address = null)
        {
            assetPath = assetPath.Replace("\\", "/");
            for (var i = Groups.Count - 1; i >= 0; i--)
            {
                var info = Groups[i].AddAssetEntry(assetPath, address);
                if (info != null)
                {
                    return info;
                }
            }

            Debug.LogError($"{assetPath}不存在分组");
            return null;
        }

        public bool RemoveAssetInfo(string assetPath)
        {
            var remove = false;
            foreach (var group in Groups)
            {
                remove = remove || group.RemoveEntry(assetPath);
            }

            return remove;
        }

        public bool HasAssetInfo(string assetPath)
        {
            return GetAssetInfo(assetPath) != null;
        }

        public void GenerateCache()
        {
            foreach (var group in groupsCache.Values)
            {
                group.GenerateCache();
            }

            groupsCache.Clear();
        }
    }
}