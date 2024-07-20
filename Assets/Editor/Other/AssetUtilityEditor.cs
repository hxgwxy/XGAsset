using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using XGAsset.Runtime.Misc;
using Object = UnityEngine.Object;

namespace XGAsset.Editor.Other
{
    public class AssetUtilityEditor
    {
        static HashSet<string> excludedExtensions = new HashSet<string>(new string[] { ".cs", ".js", ".boo", ".exe", ".dll", ".meta", ".preset", ".asmdef" });

        public static List<string> GetDependencies(string assetPath)
        {
            return GetDependencies(assetPath, true);
        }

        private static List<string> GetDependencies(List<string> assetPaths, bool recursive)
        {
            return AssetDatabase.GetDependencies(assetPaths.ToArray(), recursive).Where(IsValidAssetPath).Sort2List();
        }

        public static List<string> GetDependencies(List<string> assetPaths)
        {
            return GetDependencies(assetPaths, true);
        }

        public static List<string> GetDependencies(string assetPath, bool recursive)
        {
            return GetDependencies(new List<string>() { assetPath }, true);
        }

        public static List<string> GetDependencies2(string assetPath, bool recursive)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var guid = new GUID(AssetDatabase.AssetPathToGUID(assetPath));
            var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(guid, target);
            var mode = recursive ? DependencyType.RecursiveOperation | DependencyType.ValidReferences : DependencyType.ValidReferences;
            var dependenciesObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, target, null, mode);
            return dependenciesObjects.Select(v => AssetDatabase.GUIDToAssetPath(v.guid)).Where(IsValidAssetPath).Sort2List();
        }


        public static bool TryGetPathAndGUIDFromTarget(Object target, out string path, out string guid)
        {
            guid = string.Empty;
            path = string.Empty;
            if (target == null)
                return false;
            path = AssetDatabase.GetAssetOrScenePath(target);
            if (!IsValidAssetPath(path))
                return false;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out guid, out long id))
                return false;
            return true;
        }

        internal static bool IsValidAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            path = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            if (!path.StartsWith("assets", StringComparison.OrdinalIgnoreCase) && !IsPathValidPackageAsset(path))
                return false;
            if (path == UnityEditor.Build.Utilities.CommonStrings.UnityEditorResourcePath ||
                path == UnityEditor.Build.Utilities.CommonStrings.UnityDefaultResourcePath ||
                path == UnityEditor.Build.Utilities.CommonStrings.UnityBuiltInExtraPath)
                return false;
            if (path.EndsWith($"{Path.DirectorySeparatorChar}Editor") || path.Contains($"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}")
                                                                      || path.EndsWith("/Editor") || path.Contains("/Editor/"))
                return false;
            if (path == "Assets")
                return false;
            return !excludedExtensions.Contains(Path.GetExtension(path));
        }

        internal static bool IsPathValidPackageAsset(string path)
        {
            string[] splitPath = path.ToLower().Split(Path.DirectorySeparatorChar);

            if (splitPath.Length < 3)
                return false;
            if (splitPath[0] != "packages")
                return false;
            if (splitPath[2] == "package.json")
                return false;
            return true;
        }
        
        internal static void ClearDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                return;

            var direction = new DirectoryInfo(dirPath);
            var files = direction.GetFiles("*", SearchOption.AllDirectories);

            foreach (var f in files)
            {
                File.Delete(f.FullName);
            }

            var dirs = direction.GetDirectories("*", SearchOption.AllDirectories);

            for (var i = dirs.Length - 1; i >= 0; i--)
            {
                Directory.Delete(dirs[i].FullName);
            }
        }

    }
}