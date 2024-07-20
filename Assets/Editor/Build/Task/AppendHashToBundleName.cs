using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using XGAsset.Editor.Build;

namespace XGAsset.Editor.Build.Task
{
    public class AppendHashToBundleName : IBuildTask
    {
        public int Version => 1;

#pragma warning disable 649
        [InjectContext(ContextUsage.In)] IBundleBuildParameters m_Parameters;

        [InjectContext] IBundleBuildResults m_Results;

        [InjectContext(ContextUsage.In)] IBundleBuildContent m_BuildContent;

        [InjectContext] IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)]
        IBuildSpriteData m_SpriteData;

        [InjectContext(ContextUsage.In)] IAssetBuildContext m_xBuildContext;

        [InjectContext(ContextUsage.In, true)] IBuildCache m_Cache;
#pragma warning restore 649

        public ReturnCode Run()
        {
            var xBuildContent = (m_xBuildContext as AssetBuildContext) ?? new AssetBuildContext();

            xBuildContent.BundleNamesMap = new Dictionary<string, string>();
            
            return ReturnCode.Success;

            // var newBundleLayout = new Dictionary<string, List<GUID>>();
            // foreach (var item in m_BuildContent.BundleLayout)
            // {
            //     var bundleName = item.Key;
            //     var hash = CalcHash(item.Value);
            //     var newBundleName = bundleName; // $"{Path.GetFileNameWithoutExtension(item.Key)}_{hash}{Path.GetExtension(bundleName)}";
            //     newBundleLayout.Add(newBundleName, item.Value);
            //
            //     xBuildContent.BundleNamesMap[item.Key] = newBundleName;
            //     xBuildContent.BundleNamesMap[newBundleName] = item.Key;
            // }
            //
            // m_BuildContent.BundleLayout.Clear();
            //
            // foreach (var item in newBundleLayout)
            //     m_BuildContent.BundleLayout.Add(item.Key, item.Value);
            // return ReturnCode.Success;
        }

        private RawHash CalcHash(List<GUID> guids)
        {
            var xBuildContent = (m_xBuildContext as AssetBuildContext) ?? new AssetBuildContext();
            var list = guids.Select(AssetDatabase.GetAssetDependencyHash).ToList();
            list.Add(HashingMethods.Calculate(xBuildContent.CurrPackage.PackageName).ToHash128());
            return HashingMethods.Calculate(list);
        }
    }
}