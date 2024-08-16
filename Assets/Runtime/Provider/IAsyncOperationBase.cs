using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace XGAsset.Runtime.Provider
{
    public interface IAsyncOperationBase
    {
        public T GetAsset<T>();

        public List<T> GetAssets<T>();

        public UniTask<T> GetAssetAsync<T>();

        public UniTask<IList<T>> GetAssetsAsync<T>();

        public bool IsDone { get; }

        public string DebugInfo { get; }

        public event Action<AssetOperationHandle> Completed;

        public List<IAsyncOperationBase> DependOps { get; set; }

        public UniTask Task { get; }

        public void Start();

        public Exception Exception { get; }

        public int RefCount { get; }

        public void WaitForCompleted();
    }

}