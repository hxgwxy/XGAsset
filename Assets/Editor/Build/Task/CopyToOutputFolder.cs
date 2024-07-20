using System.IO;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using XGAsset.Editor.Build;
using XGAsset.Runtime.Misc;

namespace XGFramework.XGAsset.Editor.Build.Task
{
    public class CopyToOutputFolder : IBuildTask
    {
        public int Version => 1;

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBundleBuildParameters m_Parameters;

        [InjectContext]
        IBundleBuildResults m_Results;

        [InjectContext(ContextUsage.In)]
        IBundleBuildContent m_BuildContent;

        [InjectContext]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)]
        IBuildSpriteData m_SpriteData;

        [InjectContext(ContextUsage.In)]
        IAssetBuildContext m_xBuildContext;

        [InjectContext(ContextUsage.In, true)]
        IBuildCache m_Cache;
#pragma warning restore 649


        public ReturnCode Run()
        {
            var xBuildContent = m_xBuildContext as AssetBuildContext;
            var parameters = m_Parameters as BundleBuildParameters;

            if (parameters is null || xBuildContent is null)
            {
                return ReturnCode.Error;
            }

            var packageOutputFolder = StringPlaceholderUtil.GetString($"{xBuildContent.CurrPackage.BuildPath}");
            if (Directory.Exists(packageOutputFolder))
                Directory.Delete(packageOutputFolder, true);

            if (!string.IsNullOrEmpty(packageOutputFolder))
            {
                var outputFolder = $"{parameters.OutputFolder}";
                var files = Directory.GetFiles(outputFolder);
                foreach (var file in files)
                {
                    var fromFile = file.Replace(parameters.OutputFolder, "");
                    var toFileName2 = $"{packageOutputFolder}/{fromFile}";
                    Directory.CreateDirectory(Path.GetDirectoryName(toFileName2) ?? string.Empty);
                    File.Copy(file, toFileName2, true);
                }

                AssetDatabase.Refresh();
            }

            return ReturnCode.Success;
        }
    }
}