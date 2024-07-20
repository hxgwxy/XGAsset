using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XGAsset.Editor.Settings;

/// 分组信息，按照文件夹分组
namespace XGAsset.Utility
{
    [Serializable]
    public class AssetAddressGroupInfo
    {
        [SerializeField]
        private string packageName = string.Empty;

        [SerializeField]
        private string groupName = string.Empty;

        [SerializeField]
        private string folderPath = string.Empty;

        [SerializeField]
        private string guid = string.Empty;

        [SerializeField]
        private List<AssetAddressEntry> entries = new List<AssetAddressEntry>();

        [SerializeField]
        private string packRule = string.Empty;

        [SerializeField]
        private bool copyToStreamingAssets = true;

        [SerializeField]
        private bool active = true;

        public string PackageName
        {
            get => packageName;
            internal set => packageName = value;
        }

        public string GroupName
        {
            get => groupName;
            internal set => groupName = value;
        }

        public string FolderPath
        {
            get => folderPath;
            internal set => folderPath = value;
        }

        public string Guid
        {
            get => guid;
            internal set => guid = value;
        }

        public string PackRule
        {
            get => packRule;
            set => packRule = value;
        }

        public bool CopyToStreamingAssets
        {
            get => copyToStreamingAssets;
            set => copyToStreamingAssets = value;
        }

        public bool Active
        {
            get => active;
            set => active = value;
        }

        public List<AssetAddressEntry> Entries => entries;

        private SortedDictionary<string, List<AssetAddressEntry>> entriesCache = new SortedDictionary<string, List<AssetAddressEntry>>();

        private void AddCache(AssetAddressEntry entry)
        {
            if (!entriesCache.ContainsKey(entry.Address)) entriesCache.Add(entry.Address, new List<AssetAddressEntry>());
            entriesCache[entry.Address].Add(entry);

            if (!entriesCache.ContainsKey(entry.AssetPath)) entriesCache.Add(entry.AssetPath, new List<AssetAddressEntry>());
            entriesCache[entry.AssetPath].Add(entry);

            if (!entriesCache.ContainsKey(entry.Guid)) entriesCache.Add(entry.Guid, new List<AssetAddressEntry>());
            entriesCache[entry.Guid].Add(entry);

            foreach (var label in entry.Labels)
            {
                if (!entriesCache.ContainsKey(label)) entriesCache.Add(label, new List<AssetAddressEntry>());
                entriesCache[label].Add(entry);
            }
        }

        private void RemoveCache(AssetAddressEntry entry)
        {
            if (entriesCache.ContainsKey(entry.Address)) entriesCache.Remove(entry.Address);

            if (entriesCache.ContainsKey(entry.AssetPath)) entriesCache.Remove(entry.AssetPath);

            if (entriesCache.ContainsKey(entry.Guid)) entriesCache.Remove(entry.Guid);

            foreach (var label in entry.Labels)
            {
                if (entriesCache.ContainsKey(label)) entriesCache[label].Remove(entry);
            }
        }

        public AssetAddressEntry GetAssetInfo(string assetPath)
        {
            if (entriesCache.Count == 0)
            {
                foreach (var entry in entries)
                {
                    AddCache(entry);
                }
            }

            if (entriesCache.ContainsKey(assetPath))
                return entriesCache[assetPath].Count > 0 ? entriesCache[assetPath][0] : null;
            return null;
        }

        public List<AssetAddressEntry> GetAssetInfos(string assetPath)
        {
            if (entriesCache.Count == 0)
            {
                foreach (var entry in entries)
                {
                    AddCache(entry);
                }
            }

            if (entriesCache.ContainsKey(assetPath))
                return entriesCache[assetPath];
            return null;
        }

        public AssetAddressEntry AddAssetEntry(string assetPath, string address = null)
        {
            if (!IsAssetMatch(assetPath))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                if (string.IsNullOrEmpty(address))
                {
                    address = assetPath;
                }

                var entry = GetAssetInfo(assetPath);

                if (entry == null)
                {
                    entry = new AssetAddressEntry()
                    {
                        AssetPath = assetPath,
                        Guid = AssetDatabase.AssetPathToGUID(assetPath),
                        Address = address,
                        MainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath)?.FullName,
                        GroupName = groupName,
                    };
                    entries.Add(entry);
                    AddCache(entry);
                    entries.Sort((a, b) => string.CompareOrdinal(a.AssetPath, b.AssetPath));
                }

                return entry;
            }

            return null;
        }

        public bool RemoveEntry(string assetPath)
        {
            var entry = entries.Find(v => v.AssetPath.Equals(assetPath) || v.Address.Equals(assetPath));

            if (entry != null)
            {
                entries.Remove(entry);
                RemoveCache(entry);
                return true;
            }

            return false;
        }

        public bool HasAssetInfo(string assetPath)
        {
            return GetAssetInfo(assetPath) != null;
        }

        /// <summary>
        /// 是否属于这个分组
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public bool IsAssetMatch(string assetPath)
        {
            return assetPath.StartsWith(folderPath);
        }

        public void GenerateCache()
        {
            entriesCache.Clear();
            foreach (var entry in entries)
            {
                AddCache(entry);
            }
        }
    }
}