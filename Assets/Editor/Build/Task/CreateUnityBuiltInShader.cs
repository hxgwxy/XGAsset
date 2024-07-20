using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace XGFramework.XGAsset.Editor.Build.Task
{
    public class CreateUnityBuiltInShader : IBuildTask
    {
        static readonly GUID k_BuiltInGuid = new GUID("0000000000000000f000000000000000");

        /// <inheritdoc />
        public int Version
        {
            get { return 1; }
        }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)] IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.InOut, true)]
        IBundleExplictObjectLayout m_Layout;
#pragma warning restore 649

        /// <summary>
        /// Stores the name for the built-in shaders bundle.
        /// </summary>
        public string ShaderBundleName { get; set; }

        /// <summary>
        /// Create the built-in shaders bundle.
        /// </summary>
        /// <param name="bundleName">The name of the bundle.</param>
        public CreateUnityBuiltInShader(string bundleName)
        {
            ShaderBundleName = bundleName;
        }

        /// <inheritdoc />
        public ReturnCode Run()
        {
            HashSet<ObjectIdentifier> buildInObjects = new HashSet<ObjectIdentifier>();
            foreach (AssetLoadInfo dependencyInfo in m_DependencyData.AssetInfo.Values)
                buildInObjects.UnionWith(dependencyInfo.referencedObjects.Where(x => x.guid == k_BuiltInGuid));

            foreach (SceneDependencyInfo dependencyInfo in m_DependencyData.SceneInfo.Values)
                buildInObjects.UnionWith(dependencyInfo.referencedObjects.Where(x => x.guid == k_BuiltInGuid));

            ObjectIdentifier[] usedSet = buildInObjects.ToArray();
            Type[] usedTypes = usedSet.Select(objectId => ContentBuildInterface.GetTypesForObject(objectId)[0]).ToArray();

            if (m_Layout == null)
                m_Layout = new BundleExplictObjectLayout();

            Type shader = typeof(Shader);

            List<ObjectIdentifier> usedSet2 = new List<ObjectIdentifier>();

            for (int i = 0; i < usedTypes.Length; i++)
            {
                if (usedTypes[i] != shader)
                    continue;

                usedSet2.Add(usedSet[i]);
            }

            usedSet2.Sort((a, b) => (int)(a.localIdentifierInFile - b.localIdentifierInFile));

            var s = string.Join(",", usedSet2.Select(v => v.localIdentifierInFile).ToArray());

            var newBundleName = ShaderBundleName;//$"{Path.GetFileNameWithoutExtension(ShaderBundleName)}_{HashingMethods.Calculate(s)}{Path.GetExtension(ShaderBundleName)}";

            foreach (var identifier in usedSet2)
            {
                m_Layout.ExplicitObjectLocation.Add(identifier, newBundleName);
            }

            if (m_Layout.ExplicitObjectLocation.Count == 0)
                m_Layout = null;

            return ReturnCode.Success;
        }
    }
}