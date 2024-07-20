using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using XGAsset.Editor.Settings;
using XGAsset.Runtime;
using XGAsset.Runtime.Provider;
using XGAsset.Editor.Settings;

namespace XGAsset.Editor.Load
{
    public class EditorSceneProvider : AsyncOperationBase
    {
        private string _sceneName;

        private LoadSceneMode _mode;

        public EditorSceneProvider(string sceneName, LoadSceneMode mode)
        {
            _sceneName = sceneName;
            _mode = mode;
        }

        protected override UniTask StartSelf()
        {
            var entry = AssetAddressDefaultSettings.GetEntry(_sceneName);
            if (entry != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(entry.AssetPath, new LoadSceneParameters() { loadSceneMode = _mode });
            }
            else
            {
                Debug.LogError($"entry {_sceneName} not found.");
            }

            return UniTask.CompletedTask;
        }
    }
}