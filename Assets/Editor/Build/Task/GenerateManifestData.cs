using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using XGAsset.Runtime.Misc;
using XGAsset.Editor.Settings;
using XGFramework.LitJson;

namespace XGAsset.Editor.Build.Task
{
    public class GenerateManifestData : IBuildTask
    {
        public int Version => 1;

#pragma warning disable 649
        [InjectContext(ContextUsage.In)] IBundleBuildParameters m_Parameters;

        [InjectContext] IBundleBuildResults m_Results;

        [InjectContext(ContextUsage.In)] IBundleBuildContent m_BuildContent;

        [InjectContext] IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)] IBuildSpriteData m_SpriteData;

        [InjectContext(ContextUsage.In)] IAssetBuildContext m_xBuildContext;

        [InjectContext(ContextUsage.In, true)] IBuildCache m_Cache;

        [InjectContext(ContextUsage.In)] IBundleWriteData m_WriteData;
#pragma warning restore 649

        public ReturnCode Run()
        {
            var assetContext = m_xBuildContext as AssetBuildContext ?? new AssetBuildContext();

            var manifest = new ManifestData
            {
                PackageName = assetContext.CurrPackage.PackageName,
                Version = assetContext.CurrPackage.Version,
                AddressInfos = new List<AddressInfo>(),
                BundleInfos = new List<BundleInfo>(),
                LoadPath = StringPlaceholderUtil.GetString(assetContext.CurrPackage.LoadPath),
            };

            var md5Map = new Dictionary<string, string>();
            var bundleNameMap = new Dictionary<string, string>();
            foreach (var item in m_Results.BundleInfos)
            {
                var bundleInfo = item.Value;
                var md5 = AssetUtility.GetFileMD5(bundleInfo.FileName);
                var newFileName = GetNewFileName(bundleInfo.FileName, md5);
                File.Move(bundleInfo.FileName, newFileName);

                md5Map[newFileName] = md5;
                md5Map[Path.GetFileName(newFileName)] = md5;
                md5Map[Path.GetFileName(bundleInfo.FileName)] = md5;

                bundleNameMap[bundleInfo.FileName] = newFileName;
                bundleNameMap[Path.GetFileName(bundleInfo.FileName)] = Path.GetFileName(newFileName);
            }

            assetContext.BundleNamesMap = bundleNameMap;

            foreach (var item in m_BuildContent.BundleLayout)
            {
                var assetList = item.Value.Select(AssetDatabase.GUIDToAssetPath).ToList();
                foreach (var assetPath in assetList)
                {
                    var entry = GetEntry(assetContext.CurrPackage, assetPath);

                    if (entry != null)
                    {
                        var addressInfo = new AddressInfo
                        {
                            Address = entry.Address,
                            AssetPath = entry.AssetPath,
                            Label = entry.Labels.ToArray(),
                            BundleName = bundleNameMap[item.Key],
                        };
                        manifest.AddressInfos.Add(addressInfo);
                    }
                }
            }

            var bundleDependencies = CalculateBundleDependencies(m_WriteData.AssetToFiles.Values.ToList(), m_WriteData.FileToBundle, false);
            var bundleDependencies2 = CalculateBundleDependencies(m_WriteData.AssetToFiles.Values.ToList(), m_WriteData.FileToBundle, true);
            foreach (var dependency in bundleDependencies)
            {
                foreach (var bundleName in dependency.Value)
                {
                    bundleDependencies2[dependency.Key].Remove(bundleName);
                }
            }

            foreach (var item in m_Results.BundleInfos)
            {
                var bundleInfo = item.Value;
                var detail = new BundleInfo();
                var newFileName = bundleNameMap[bundleInfo.FileName];
                detail.Name = Path.GetFileName(newFileName);
                detail.MD5 = md5Map[detail.Name];
                detail.Size = AssetUtility.GetFileSize(newFileName);
                detail.Crc = AssetUtility.GetFileCRC32(newFileName);
                detail.SizeStr = AssetUtility.GetSizeUnit((long)detail.Size);
                detail.BundleCrc = bundleInfo.Crc;
                detail.Dependencies = bundleDependencies.GetValueOrDefault(item.Key)?.Select(v => bundleNameMap[v]).Sort2List().ToArray();
                detail.IndirectDependencies = bundleDependencies2.GetValueOrDefault(item.Key)?.Select(v => bundleNameMap[v]).Sort2List().ToArray();

                var b = m_WriteData.FileToBundle.FirstOrDefault(v => v.Value.Equals(item.Key));
                detail.IncludeAssets = m_WriteData.FileToObjects[b.Key].Select(v => AssetDatabase.GUIDToAssetPath(v.guid)).Distinct().Where(v => !v.EndsWith(".cs")).ToArray();
                var temp = detail.IncludeAssets.ToList();
                temp.Sort();
                detail.IncludeAssets = temp.ToArray();

                var list = new List<string>();

                var builtInGuid = new GUID("0000000000000000f000000000000000");
                foreach (var asset in detail.IncludeAssets)
                {
                    var assetGuid = new GUID(AssetDatabase.AssetPathToGUID(asset));
                    var assetIncludes = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(assetGuid, m_Parameters.Target);
                    var assetReferences = ContentBuildInterface.GetPlayerDependenciesForObjects(assetIncludes, m_Parameters.Target, m_Parameters.ScriptInfo).Where(v => !v.guid.Equals(builtInGuid))
                        .ToArray();
                    list.AddRange(assetReferences.Select(v => AssetDatabase.GUIDToAssetPath(v.guid)).Where(v => !v.EndsWith(".cs") && !string.IsNullOrEmpty(v)).ToList());
                }

                detail.ReferenceAssets = list.Distinct().Except(detail.IncludeAssets).Sort2List().ToArray();

                manifest.BundleInfos.Add(detail);
            }

            foreach (var bundleInfo in manifest.BundleInfos)
            {
                var list = new List<string>();
                foreach (var referenceAsset in bundleInfo.ReferenceAssets)
                {
                    list.Add(referenceAsset + "   ---   " + manifest.BundleInfos.FirstOrDefault(v => v.IncludeAssets.Contains(referenceAsset))?.Name);
                }

                bundleInfo.ReferenceAssets = list.Sort2List().ToArray();
            }

            if (m_Parameters is BundleBuildParameters parameters)
            {
                var manifestPath = $"{parameters.OutputFolder}/Manifest_{manifest.PackageName}_{manifest.Version}";
                var jsonFile = $"{manifestPath}.json";
                var hashFile = $"{manifestPath}.hash";
                var zipFile = $"{manifestPath}.zip";
                File.WriteAllText(jsonFile, JsonMapper.ToJson(manifest));
                AssetUtility.CreateZipFile(zipFile, new[] { jsonFile });
                File.WriteAllText(hashFile, AssetUtility.GetFileMD5(zipFile));
            }

            assetContext.ManifestData = manifest;

            return ReturnCode.Success;
        }

        private string GetNewFileName(string fileName, string md5)
        {
            var ext = Path.GetExtension(fileName);
            return $"{fileName.Replace(ext, "")}_{md5}{ext}".Replace($"{(m_xBuildContext as AssetBuildContext).CurrPackage.PackageName.ToLower()}_", "");
        }

        private Dictionary<string, List<string>> CalculateBundleDependencies(List<List<string>> assetFileList, Dictionary<string, string> filenameToBundleName, bool recursively)
        {
            var bundleDependencies = new Dictionary<string, List<string>>();
            Dictionary<string, HashSet<string>> bundleDependenciesHash = new Dictionary<string, HashSet<string>>();
            foreach (var files in assetFileList)
            {
                if (files.Count == 0)
                    continue;

                string bundle = filenameToBundleName[files.First()];
                HashSet<string> dependencies;
                if (!bundleDependenciesHash.TryGetValue(bundle, out dependencies))
                {
                    dependencies = new HashSet<string>();
                    bundleDependenciesHash.Add(bundle, dependencies);
                }

                dependencies.UnionWith(files.Select(x => filenameToBundleName[x]));
                dependencies.Remove(bundle);

                // Ensure we create mappings for all encountered files
                foreach (var file in files)
                {
                    if (!bundleDependenciesHash.TryGetValue(filenameToBundleName[file], out dependencies))
                    {
                        dependencies = new HashSet<string>();
                        bundleDependenciesHash.Add(filenameToBundleName[file], dependencies);
                    }
                }
            }

            if (recursively)
            {
                // Recursively combine dependencies
                foreach (var dependencyPair in bundleDependenciesHash)
                {
                    List<string> dependencies = dependencyPair.Value.ToList();
                    for (int i = 0; i < dependencies.Count; i++)
                    {
                        if (!bundleDependenciesHash.TryGetValue(dependencies[i], out var recursiveDependencies))
                            continue;
                        foreach (var recursiveDependency in recursiveDependencies)
                        {
                            if (dependencyPair.Value.Add(recursiveDependency))
                                dependencies.Add(recursiveDependency);
                        }
                    }
                }
            }

            foreach (var dep in bundleDependenciesHash)
            {
                var ret = dep.Value.ToList();
                ret.Sort();
                bundleDependencies.Add(dep.Key, ret);
            }

            return bundleDependencies;
        }

        private AssetAddressEntry GetEntry(AssetAddressPackage package, string assetPath)
        {
            foreach (var group in package.Groups)
            {
                var entry = group.Entries.FirstOrDefault(v => v.AssetPath.Equals(assetPath));
                if (entry != null)
                    return entry;
            }

            return default;
        }
    }
}