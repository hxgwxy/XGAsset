using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace XGAsset.Runtime.Provider
{
    public class SceneProvider : AsyncOperationBase
    {
        private string _sceneName;

        private LoadSceneMode _mode;

        public override string DebugInfo => $"{GetType().Name}-{_sceneName}";

        public SceneProvider(string sceneName, LoadSceneMode mode)
        {
            _sceneName = sceneName;
            _mode = mode;
        }

        protected override UniTask StartSelf()
        {
            var source = new UniTaskCompletionSource();
            var asyncOperation = SceneManager.LoadSceneAsync(_sceneName, _mode);
            asyncOperation.completed += op =>
            {
                OperationStatus = OperationStatus.Succeeded;
                source.TrySetResult();
            };
            return source.Task;
        }
    }
}