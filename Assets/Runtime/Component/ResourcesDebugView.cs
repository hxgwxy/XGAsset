using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using XGAsset.Runtime.Provider;

namespace XGAsset.Runtime.Component
{
#if UNITY_EDITOR
    [CustomEditor(typeof(ResourcesDebugView))]
    internal class ResourcesDebugViewInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var t = (ResourcesDebugView)target;
            if (t.RefCountText != null)
            {
                foreach (var text in t.RefCountText)
                {
                    EditorGUILayout.LabelField("", text);
                }

                if (GUILayout.Button("释放"))
                {
                    t.Unload();
                }
            }
        }
    }

    [Serializable]
    public class ResourcesDebugView : MonoBehaviour
    {
        [Header("引用计数")]
        internal List<string> RefCountText;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            hideFlags = HideFlags.DontSaveInEditor;
            RefCountText = new List<string>();
            Method();
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                Method();
            }
        }

        private void Method()
        {
            RefCountText.Clear();
            foreach (var bundleInfo in ResourcesManager.BundleProviders)
            {
                RefCountText.Add($"{bundleInfo.Value.RefCount} {bundleInfo.Value.DebugInfo}");
            }

            foreach (var bundleInfo in ResourcesManager.AssetProviders)
            {
                RefCountText.Add($"{bundleInfo.Value.RefCount} {bundleInfo.Value.DebugInfo}");
            }

            foreach (var bundleInfo in ResourcesManager.SceneProviders)
            {
                RefCountText.Add($"{bundleInfo.Value.RefCount} {bundleInfo.Value.DebugInfo}");
            }
        }

        internal void Unload()
        {
            ResourcesManager.Unload();
        }
    }
#endif
}