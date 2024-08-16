using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using XGAsset.Editor.Settings;
using XGAsset.Runtime.Provider;
using UnityEditor.SceneManagement;
using XGAsset.Runtime;

namespace XGAsset.Editor.Load
{
    public class EditorSceneProvider : AsyncOperationBase
    {
        private string _sceneName;

        private LoadSceneMode _mode;

        private AsyncOperation mAasyncOperation;

        public EditorSceneProvider(string sceneName, LoadSceneMode mode)
        {
            _sceneName = sceneName;
            _mode = mode;
        }

        protected override void InternalStart()
        {
            var entry = AssetAddressDefaultSettings.GetEntry(_sceneName);
            if (entry != null)
            {
                mAasyncOperation = EditorSceneManager.LoadSceneAsyncInPlayMode(entry.AssetPath, new LoadSceneParameters() { loadSceneMode = _mode });
                mAasyncOperation.completed += OnOperationCompleted;
            }
            else
            {
                Debug.LogError($"无法找到地址:{_sceneName}");
                CompleteFailure();
            }
        }

        private void OnOperationCompleted(AsyncOperation operation)
        {
            CompleteSuccess();
        }

        protected override ProgressStatus GetProgressStatus()
        {
            return new ProgressStatus()
            {
                Id = GetHashCode(),
                IsValid = true,
                Percent = mAasyncOperation.progress,
            };
        }
    }
}