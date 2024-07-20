using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XGAsset.Editor.Build;
using XGAsset.Utility;
using XGAsset.Editor.Settings;
using Object = UnityEngine.Object;

namespace XGAsset.Editor.Settings
{
    public class AssetAddressDefaultSettings
    {
        private static string DefaultDataFolder = "Assets/XGAssetData/";
        private static string DefaultPackageFolder = "Assets/XGAssetData/Packages/";

        public static string BundleSuffix = ".bundle";

        private static AssetAddressSettings _setting;

        private static List<AssetAddressPackage> _packages;

        public static List<AssetAddressPackage> AllPackages
        {
            get
            {
                if (_packages == null || _packages.Count == 0)
                {
                    _packages = new List<AssetAddressPackage>();
                    Directory.CreateDirectory(DefaultPackageFolder);
                    var list = AssetDatabase.FindAssets($"t:{nameof(AssetAddressPackage)}");
                    if (list.Length == 0)
                    {
                        var package = CreatePackage("DefaultPackage");
                        Setting.DefaultPackage = package;
                        EditorUtility.SetDirty(Setting);
                        AssetDatabase.SaveAssetIfDirty(Setting);
                    }

                    foreach (var s in list)
                    {
                        _packages.Add(AssetDatabase.LoadAssetAtPath<AssetAddressPackage>(AssetDatabase.GUIDToAssetPath(s)));
                    }
                }

                return _packages;
            }
        }

        public static AssetAddressPackage GetPackage(string packageName)
        {
            return AllPackages.Find(v => v.PackageName.Equals(packageName));
        }

        public static AssetAddressPackage CurrPackage
        {
            get { return AllPackages.Find(v => v.Equals(Setting.DefaultPackage)); }
        }

        public static AssetAddressSettings Setting
        {
            get
            {
                if (_setting == null)
                {
                    _setting = ScriptableObject.CreateInstance<AssetAddressSettings>();
                    var list = AssetDatabase.FindAssets($"t:{nameof(AssetAddressSettings)}");
                    if (list.Length == 0)
                    {
                        _setting = ScriptableObject.CreateInstance<AssetAddressSettings>();
                        AssetDatabase.CreateAsset(_setting, $"{DefaultDataFolder}/AssetAddressSettings.asset");
                        EditorUtility.SetDirty(_setting);
                        AssetDatabase.SaveAssetIfDirty(_setting);
                    }
                    else
                    {
                        _setting = AssetDatabase.LoadAssetAtPath<AssetAddressSettings>(AssetDatabase.GUIDToAssetPath(list[0]));
                    }
                }

                return _setting;
            }
        }

        public static string CalcAddress(string assetPath)
        {
            return Path.GetFileNameWithoutExtension(assetPath);
        }

        public static void Save(bool force = false)
        {
            foreach (var package in AllPackages)
            {
                EditorUtility.SetDirty(package);

                if (EditorUtility.IsDirty(package))
                {
                    AssetDatabase.SaveAssetIfDirty(package);
                    DispatchModifyMsg();
                }
            }

            AssetDatabase.SaveAssets();
        }

        public static void SavePackage(string packageName = "")
        {
            var package = GetPackage(packageName) ?? CurrPackage;
            package.GenerateCache();
            EditorUtility.SetDirty(package);
            AssetDatabase.SaveAssetIfDirty(package);
            DispatchModifyMsg();
        }

        private static AssetAddressPackage CreatePackage(string name)
        {
            var package = ScriptableObject.CreateInstance<AssetAddressPackage>();
            _packages.Add(package);
            AssetDatabase.CreateAsset(package, $"{DefaultPackageFolder}/AssetAddress{name}.asset");
            package.PackageName = name;
            EditorUtility.SetDirty(package);
            AssetDatabase.SaveAssetIfDirty(package);
            DispatchModifyMsg();
            return package;
        }

        public static void SetDefaultPackage(string name)
        {
            var package = AllPackages.Find(v => v.PackageName.Equals(name));
            if (package)
            {
                Setting.DefaultPackage = package;
            }
            else
            {
                Debug.LogError($"Package {name} 不存在!");
            }
        }

        public static event Action OnDataModify;

        public static event Action OnBuildBefore;

        public static event Action OnBuildAfter;

        public static void DispatchModifyMsg()
        {
            OnDataModify?.Invoke();
        }

        /// <summary>
        /// 修复因移动位置丢失信息的entry
        /// </summary>
        public static void Fix()
        {
            var deleteGroup = new List<AssetAddressGroupInfo>();
            var deleteEntries = new List<AssetAddressEntry>();
            foreach (var package in AllPackages)
            {
                // 修复移动资源位置后
                foreach (var group in package.Groups)
                {
                    var folder = AssetDatabase.GUIDToAssetPath(group.Guid);
                    if (!string.IsNullOrEmpty(folder))
                    {
                        group.FolderPath = folder;
                    }
                    else
                    {
                        var guid = AssetDatabase.AssetPathToGUID(group.FolderPath);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            group.Guid = guid;
                        }
                        else
                        {
                            deleteGroup.Add(group);
                            Debug.LogError($"无效的分组 {group.GroupName}");
                        }
                    }

                    foreach (var entry in group.Entries)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(entry.Guid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            entry.AssetPath = path;
                        }
                        else
                        {
                            deleteEntries.Add(entry);
                            Debug.LogError($"无效的资产 {entry.Address}");
                        }
                    }
                }

                // 删除folder entry
                foreach (var group in package.Groups)
                {
                    for (var i = group.Entries.Count - 1; i >= 0; i--)
                    {
                        var entry = group.Entries[i];
                        if (AssetDatabase.IsValidFolder(entry.AssetPath))
                        {
                            group.RemoveEntry(entry.AssetPath);
                        }
                    }
                }

                // 检查重复的group
                var groupList = new List<string>();
                foreach (var group in package.Groups)
                {
                    if (groupList.Contains(group.GroupName))
                    {
                        Debug.LogError($"重复的组 {group.GroupName} {group.GroupName}");
                    }
                    else
                    {
                        groupList.Add(group.GroupName);
                    }
                }

                // 检查重复的entry
                var assetList = new List<string>();
                foreach (var group in package.Groups)
                {
                    for (var i = group.Entries.Count - 1; i >= 0; i--)
                    {
                        var entry = group.Entries[i];
                        if (!assetList.Contains(entry.AssetPath))
                        {
                            assetList.Add(entry.AssetPath);
                        }
                        else
                        {
                            Debug.LogError($"重复的Entry {package.PackageName} - {group.GroupName} - {entry.AssetPath}");
                        }
                    }
                }

                foreach (var group in package.Groups)
                {
                    group.PackageName = package.PackageName;

                    foreach (var entry in group.Entries)
                    {
                        entry.GroupName = group.GroupName;
                    }
                }
            }

            foreach (var groupInfo in deleteGroup)
            {
                RemoveGroup(groupInfo.FolderPath);
            }

            foreach (var entry in deleteEntries)
            {
                RemoveEntry(entry.Address);
            }

            Save();
        }

        public static void Build()
        {
            OnBuildBefore?.Invoke();
            BuildScript.StartBuild();
            OnBuildAfter?.Invoke();
        }

        public static AssetAddressEntry GetEntry(string address)
        {
            AssetAddressEntry info = null;
            foreach (var package in AllPackages)
            {
                info = package.GetAssetInfo(address);
                if (info != null)
                    break;
            }

            return info;
        }

        public static List<AssetAddressEntry> GetEntries(string address)
        {
            return AllPackages.Select(package => package.GetAssetInfos(address)).Where(v => v != null).SelectMany(v => v).ToList();
        }

        public static AssetAddressEntry GetEntryByLabel(string label)
        {
            return AllPackages.Select(package => package.GetAssetInfo(label)).Where(entry => entry != null).Cast<AssetAddressEntry>().FirstOrDefault();
        }

        public static bool RemoveEntry(string address)
        {
            var remove = false;
            foreach (var package in AllPackages)
            {
                remove = remove || package.RemoveAssetInfo(address);
            }

            return remove;
        }

        public static AssetAddressGroupInfo GetGroup(string groupName)
        {
            return AllPackages.Select(package => package.GetGroup(groupName)).FirstOrDefault(group => group != null);
        }

        public static AssetAddressGroupInfo GetGroup(AssetAddressEntry targetEntry)
        {
            foreach (var package in AllPackages)
            {
                foreach (var group in package.Groups)
                {
                    foreach (var entry in group.Entries)
                    {
                        if (entry.Equals(targetEntry))
                        {
                            return group;
                        }
                    }
                }
            }

            return default;
        }

        public static void RemoveGroup(string value)
        {
            AssetAddressGroupInfo deleteGroup = null;
            foreach (var package in AllPackages)
            {
                foreach (var group in package.Groups)
                {
                    if (group.FolderPath.Equals(value) || group.GroupName.Equals(value))
                    {
                        deleteGroup = group;
                    }
                }

                if (deleteGroup != null) package.RemoveGroup(deleteGroup.FolderPath);
                deleteGroup = null;
            }
        }

        public static void AddLabel(string label)
        {
            if (!string.IsNullOrEmpty(label) && !Setting.Labels.Contains(label))
            {
                Setting.Labels.Add(label);
                Setting.Labels.Sort();
            }
        }

        public static void RemoveLabel(string label)
        {
            Setting.Labels.Remove(label);
            foreach (var package in AllPackages)
            {
                foreach (var group in package.Groups)
                {
                    foreach (var entry in group.Entries)
                    {
                        entry.RemoveLabel(label);
                    }
                }
            }
        }

        public static bool HasLabel(string label)
        {
            return Setting.Labels.Contains(label);
        }

        public static AssetAddressEntry GetAssetInfo(string assetPath)
        {
            foreach (var package in AllPackages)
            {
                foreach (var group in package.Groups)
                {
                    var asset = group.GetAssetInfo(assetPath);
                    if (asset != null)
                    {
                        return asset;
                    }
                }
            }

            return null;
        }

        public static void RemoveAssetInfo(string assetPath)
        {
            foreach (var package in AllPackages)
            {
                foreach (var group in package.Groups)
                {
                    group.RemoveEntry(assetPath);
                }
            }
        }

        public static AssetAddressEntry AddAssetInfo(string assetPath, string address = null)
        {
            return CurrPackage.AddAssetInfo(assetPath, address);
        }

        public static AssetAddressGroupInfo AddGroup(string folderPath, string groupName = null)
        {
            return CurrPackage.AddGroup(folderPath, groupName);
        }

        public static bool HasAssetInfo(string assetPath)
        {
            foreach (var package in AllPackages)
            {
                foreach (var group in package.Groups)
                {
                    if (group.HasAssetInfo(assetPath))
                        return true;
                }
            }

            return false;
        }

        public static void ChangeAssetAddress(string oldAddress, string newAddress)
        {
            if (oldAddress.Equals(newAddress))
                return;
            if (GetEntry(newAddress) == null)
            {
                GetEntry(oldAddress).Address = newAddress;
            }
            else
            {
                Debug.LogError($"Address重复:{oldAddress}->{newAddress}");
            }

            SavePackage();
        }

        public static void SetAssetAddress(Object target)
        {
            var assetPath = AssetDatabase.GetAssetPath(target);
            var entry = GetAssetInfo(assetPath);
            if (entry != null)
            {
                ChangeAssetAddress(entry.Address, Path.GetFileNameWithoutExtension(assetPath));
            }
        }
    }
}