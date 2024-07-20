using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using XGAsset.Editor.Build;
using XGAsset.Runtime;
using XGAsset.Runtime.Misc;

namespace XGAsset.Editor.Build.Task
{
    public class CopyToStreamingAssets : IBuildTask
    {
        public int Version => 1;

#pragma warning disable 649
        [InjectContext(ContextUsage.In)] IBundleBuildParameters m_Parameters;

        [InjectContext] IBundleBuildResults m_Results;

        [InjectContext(ContextUsage.In)] IBundleBuildContent m_BuildContent;

        [InjectContext] IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)] IBuildSpriteData m_SpriteData;

        [InjectContext(ContextUsage.In)] IAssetBuildContext m_assetBuildContext;

        [InjectContext(ContextUsage.In, true)] IBuildCache m_Cache;
#pragma warning restore 649


        private ManifestData m_manifestData;
        private AssetBuildContext m_assetContent;

        public ReturnCode Run()
        {
            var parameters = m_Parameters as BundleBuildParameters;

            m_assetContent = m_assetBuildContext as AssetBuildContext;

            if (parameters is null || m_assetContent is null)
            {
                return ReturnCode.Error;
            }

            m_manifestData = m_assetContent.ManifestData;

            var copyToFolder = string.Join("/", CommonString.StreamingAssets, m_assetContent.CurrPackage.PackageName);
            if (Directory.Exists(copyToFolder))
            {
                Directory.Delete(copyToFolder, true);
            }

            var files = GenerateCopyFiles();

            if (files.Count > 0)
                Directory.CreateDirectory(copyToFolder);

            foreach (var fileName in files)
            {
                var toFileName = string.Join("/", copyToFolder, fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(toFileName) ?? string.Empty);
                File.Copy($"{parameters.OutputFolder}{fileName}", toFileName, true);
            }

            AssetDatabase.Refresh();
            return ReturnCode.Success;
        }

        private List<string> GenerateCopyFiles()
        {
            var files = new List<string>();

            // foreach (var item in m_manifestData.BundleInfos)
            // {
            //     var fileName = item.Name;
            //     if (m_assetContent.BundleNamesMap.TryGetValue(fileName, out var orgFileName))
            //     {
            //         var bundleLayout = m_assetContent.CustomBundleLayouts.FirstOrDefault(v => v.BundleName.Equals(orgFileName, StringComparison.OrdinalIgnoreCase));
            //         if (bundleLayout is { CopyToStreamingAssets: true })
            //         {
            //             GenerateDependCopyFiles(files, fileName);
            //         }
            //     }
            // }

            foreach (var customBundleLayout in m_assetContent.CustomBundleLayouts)
            {
                if (customBundleLayout is { CopyToStreamingAssets: true })
                {
                    if (m_assetContent.BundleNamesMap.TryGetValue(customBundleLayout.BundleName.ToLower(), out var newFileName))
                    {
                        GenerateDependCopyFiles(files, newFileName);
                    }
                }
            }

            var assetContext = m_assetBuildContext as AssetBuildContext ?? new AssetBuildContext();

            if (m_Parameters is BundleBuildParameters parameters)
            {
                var manifestPath = $"Manifest_{assetContext.CurrPackage.PackageName}_{assetContext.CurrPackage.Version}";
                var jsonFile = $"{manifestPath}.json";
                var hashFile = $"{manifestPath}.hash";
                var zipFile = $"{manifestPath}.zip";

                var copyToFolder = string.Join("/", CommonString.StreamingAssets, assetContext.CurrPackage.PackageName);
                files.Add(jsonFile);
                files.Add(hashFile);
                files.Add(zipFile);
            }


            files.Sort();

            return files;
        }

        private void GenerateDependCopyFiles(List<string> files, string fileName)
        {
            if (!files.Contains(fileName))
            {
                files.Add(fileName);
            }

            var bundleInfo = m_manifestData.BundleInfos.FirstOrDefault(v => v.Name.Equals(fileName));

            foreach (var dependency in bundleInfo.Dependencies)
            {
                if (!files.Contains(dependency))
                {
                    GenerateDependCopyFiles(files, dependency);
                }
            }
        }
    }
}