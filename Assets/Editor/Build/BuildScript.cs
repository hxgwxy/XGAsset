using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XGAsset.Editor.Build.Task;
using XGAsset.Editor.Settings;
using XGAsset.Runtime;
using XGAsset.Runtime.Misc;
using XGAsset.Utility;
using XGAsset.Editor.Build.Task;
using XGAsset.Editor.Other;
using BuildCompression = UnityEngine.BuildCompression;
using Debug = UnityEngine.Debug;

namespace XGAsset.Editor.Build
{
    public static class BuildScript
    {
        internal static void StartBuild()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            var bundleLayouts = GenerateBundleLayoutsByGroup();
            var bundleBuilds = CreateAssetBundleBuilds(bundleLayouts);
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

            AssetUtilityEditor.ClearDirectory(CommonString.BuildOutput);

            var buildParams = new AssetBundleBuildParameters(buildTarget, buildTargetGroup, CommonString.BuildOutput)
            {
                BundleCompression = BuildCompression.LZ4,
                UseCache = false,
            };
            var xBuildContent = new AssetBuildContext()
            {
                CurrPackage = AssetAddressDefaultSettings.CurrPackage,
                CustomBundleLayouts = bundleLayouts,
            };
            var buildTasks = GetBuildTask(AssetAddressDefaultSettings.CurrPackage.PackageName);
            var bundleBuildContent = new BundleBuildContent(bundleBuilds);
            var exitCode = ContentPipeline.BuildAssetBundles(buildParams, bundleBuildContent, out var results, buildTasks, xBuildContent);

            var settings = Resources.Load<RuntimeSettings>(CommonString.RuntimeSettingsLoadPath);
            if (!settings)
            {
                settings = ScriptableObject.CreateInstance<RuntimeSettings>();
                Directory.CreateDirectory(Path.GetDirectoryName(CommonString.RuntimeSettingsPath));
                AssetDatabase.CreateAsset(settings, CommonString.RuntimeSettingsPath);
            }

            var packageSetting = settings.PackageSettings.FirstOrDefault(v => v.PackageName.Equals(AssetAddressDefaultSettings.CurrPackage.PackageName));
            if (packageSetting == null)
            {
                packageSetting = new PackageSetting();
                packageSetting.PackageName = AssetAddressDefaultSettings.CurrPackage.PackageName;
                settings.PackageSettings.Add(packageSetting);
            }

            packageSetting.RemoteLoadPath = AssetAddressDefaultSettings.CurrPackage.LoadPath;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);

            Debug.Log($"构建结果：{AssetAddressDefaultSettings.CurrPackage.PackageName} {exitCode}");
        }

        private static List<AssetBundleBuild> CreateAssetBundleBuilds(List<BundleBuildLayout> bundleLayouts)
        {
            return bundleLayouts.Select(v => new AssetBundleBuild()
            {
                assetBundleName = v.BundleName.ToLower(),
                assetNames = v.AllRefAssets.ToArray()
            }).ToList();
        }

        private static List<BundleBuildLayout> GenerateCurrentPackageBundleLayouts()
        {
            var groups = AssetAddressDefaultSettings.CurrPackage.Groups;
            var bundleLayouts = new List<BundleBuildLayout>();
            foreach (var group in groups)
            {
                if (group.Active)
                {
                    var layouts = GenerateBundleLayout(group);
                    if (layouts != null)
                    {
                        foreach (var bundleLayout in layouts)
                        {
                            bundleLayout.CopyToStreamingAssets = group.CopyToStreamingAssets;
                        }

                        bundleLayouts.AddRange(layouts);
                    }
                }
            }

            return bundleLayouts;
        }

        private static List<BundleBuildLayout> GenerateBundleLayout(AssetAddressGroupInfo group)
        {
            var packRuleTypes = TypeCache.GetTypesDerivedFrom(typeof(IBundlePackRule)).ToList();

            var rule = Activator.CreateInstance(packRuleTypes.FirstOrDefault(v => v.Name.Equals(group.PackRule)) ?? typeof(PackTogether)) as IBundlePackRule;

            return rule?.CreateAssetBundleBuildLayouts(group);
        }

        private static List<BundleBuildLayout> GenerateBundleLayoutsByGroup()
        {
            var bundleLayouts = GenerateCurrentPackageBundleLayouts();

            var refCount = new SortedDictionary<string, int>(); // 记录每个资源的引用次数

            foreach (var layout in bundleLayouts)
            {
                foreach (var asset in layout.AllRefAssets)
                {
                    if (!refCount.ContainsKey(asset))
                    {
                        refCount.Add(asset, 0);
                    }

                    refCount[asset] += 1;
                }
            }

            var duplicationAssets = refCount.Where(v => v.Value >= 2).Select(v => v.Key).ToList();

            foreach (var layout in bundleLayouts) // 剔除重复的
            {
                layout.AllRefAssets = layout.AllRefAssets.Except(duplicationAssets).ToList();
            }

            var paths = duplicationAssets;
            while (paths.Count > 0)
            {
                var keyword = "";
                var index = 0;
                while (true)
                {
                    index = paths[0].IndexOf("/", index, StringComparison.Ordinal);
                    if (index < 0)
                        break;
                    keyword = paths[0].Substring(0, index);
                    index += 1;

                    if (paths.Count(v => v.StartsWith(keyword)) < 2)
                    {
                        keyword = keyword.Substring(0, keyword.LastIndexOf("/", StringComparison.Ordinal));
                        break;
                    }
                }

                var shareLayout = new BundleBuildLayout();
                shareLayout.BundleName = $"share_{keyword.Replace("/", "_")}" + AssetAddressDefaultSettings.BundleSuffix;
                shareLayout.CopyToStreamingAssets = true;
                shareLayout.AllRefAssets.AddRange(paths.Where(v => v.StartsWith(keyword)));
                bundleLayouts.Add(shareLayout);
                paths = paths.Except(shareLayout.AllRefAssets).ToList();
            }

            return bundleLayouts;
        }

        private static IList<IBuildTask> GetBuildTask(string builtInShaderName)
        {
            var buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new SwitchToBuildPlatform());
            buildTasks.Add(new RebuildSpriteAtlasCache());

            // Player Scripts
            buildTasks.Add(new BuildPlayerScripts());
            buildTasks.Add(new PostScriptsCallback());

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
#if UNITY_2019_3_OR_NEWER
            buildTasks.Add(new CalculateCustomDependencyData());
#endif
            buildTasks.Add(new CalculateAssetDependencyData());
            buildTasks.Add(new StripUnusedSpriteSources());
            buildTasks.Add(new CreateUnityBuiltInShader($"{builtInShaderName}_UnityBuiltInShaders.bundle".ToLower()));
            // buildTasks.Add(new CreateMonoScriptBundle("UnityMonoScripts.bundle"));
            buildTasks.Add(new PostDependencyCallback());

            // Packing
            buildTasks.Add(new GenerateBundlePacking());
            buildTasks.Add(new UpdateBundleObjectLayout());
            buildTasks.Add(new GenerateBundleCommands());
            buildTasks.Add(new GenerateSubAssetPathMaps());
            buildTasks.Add(new GenerateBundleMaps());
            buildTasks.Add(new PostPackingCallback());

            // Writing
            buildTasks.Add(new WriteSerializedFiles());
            buildTasks.Add(new ArchiveAndCompressBundles());
            buildTasks.Add(new PostWritingCallback());

            // Custom
            // buildTasks.Add(new AppendHashToBundleName());
            buildTasks.Add(new GenerateManifestData());
            buildTasks.Add(new CopyToStreamingAssets());
            buildTasks.Add(new CopyToOutputFolder());

            return buildTasks;
        }
    }
}