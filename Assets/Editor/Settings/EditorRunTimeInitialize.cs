using UnityEditor;
using UnityEngine;
using XGAsset.Editor.Load;
using XGAsset.Editor.Other;
using XGAsset.Runtime;
using XGAsset.Runtime.Implement;
using XGFramework.XGAsset.Editor.Settings;
using PlayMode = XGAsset.Runtime.PlayMode;

namespace XGAsset.Editor.Settings
{
    public class EditorRunTimeInitialize : IEditorRunTimeInitialize
    {
        private static AssetImplBase _assetImplEditor;

        public PlayMode RunTimePlayMode => AssetAddressDefaultSettings.Setting.PlayMode;
        public AssetImplBase AssetImplEditor => _assetImplEditor ??= new AssetImplEditor();

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            AssetLoader.EditorRunTimeInitialize = new EditorRunTimeInitialize();
            EditorApplication.playModeStateChanged -= OnPlayerModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayerModeStateChanged;
        }

        private static void OnPlayerModeStateChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.EnteredPlayMode)
            {
            }

            if (playModeState == PlayModeStateChange.ExitingPlayMode)
            {
            }
        }
    }
}