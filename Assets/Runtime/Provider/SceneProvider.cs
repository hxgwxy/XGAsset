using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XGAsset.Runtime.Provider
{
    public class SceneProvider : AsyncOperationBase
    {
        private string mSceneName;

        private LoadSceneMode mMode;

        private AsyncOperation _mAsyncOperation;

        public override string DebugInfo => $"{GetType().Name}-{mSceneName}";

        public SceneProvider(string sceneName, LoadSceneMode mode)
        {
            mSceneName = sceneName;
            mMode = mode;

            var addressInfo = ResourcesManager.GetAddressInfo(sceneName);
            var bundleProvider = ResourcesManager.CreateBundleProvider(addressInfo.PackageName, addressInfo.BundleName);
            if (bundleProvider != null)
            {
                DependOps ??= new List<IAsyncOperationBase>();
                DependOps.Add(bundleProvider);
            }
        }

        protected override void ProcessDependsCompleted()
        {
            _mAsyncOperation = SceneManager.LoadSceneAsync(mSceneName, mMode);
            _mAsyncOperation.completed += OnLoadSceneCompleted;
        }

        private void OnLoadSceneCompleted(AsyncOperation op)
        {
            CompleteSuccess();
        }

        protected override void InternalWaitForCompleted()
        {
        }

        protected override ProgressStatus GetProgressStatus()
        {
            return new ProgressStatus()
            {
                Id = GetHashCode(),
                IsValid = true,
                Percent = _mAsyncOperation?.progress ?? 0,
            };
        }
    }
}