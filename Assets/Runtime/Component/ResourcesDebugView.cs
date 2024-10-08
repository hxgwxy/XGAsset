using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

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

        private StringBuilder stringBuilder;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            stringBuilder = new StringBuilder();
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
            stringBuilder.Clear();
            
            foreach (var bundleInfo in ResourcesManager.BundleProviders)
            {
                stringBuilder.Clear();
                stringBuilder.Append(bundleInfo.Value.RefCount.ToString());
                stringBuilder.Append(" ");
                stringBuilder.Append(bundleInfo.Value.DebugInfo);
                RefCountText.Add(stringBuilder.ToString());
            }

            foreach (var bundleInfo in ResourcesManager.AssetProviders)
            {
                stringBuilder.Clear();
                stringBuilder.Append(bundleInfo.Value.RefCount.ToString());
                stringBuilder.Append(" ");
                stringBuilder.Append(bundleInfo.Value.DebugInfo);
                RefCountText.Add(stringBuilder.ToString());
            }

            foreach (var bundleInfo in ResourcesManager.SceneProviders)
            {
                stringBuilder.Clear();
                stringBuilder.Append(bundleInfo.Value.RefCount.ToString());
                stringBuilder.Append(" ");
                stringBuilder.Append(bundleInfo.Value.DebugInfo);
                RefCountText.Add(stringBuilder.ToString());
            }
        }

        internal void Unload()
        {
            ResourcesManager.Unload();
        }
    }
#endif
}