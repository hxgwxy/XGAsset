using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XGAsset.Runtime.Component;
using XGAsset.Runtime.Misc;
using XGAsset.Runtime.Pool;
using XGAsset.Runtime.Provider;

namespace XGAsset.Runtime
{
    public struct AssetOperationHandle
    {
        private AsyncOperationBase op;

        public AssetOperationHandle(AsyncOperationBase op) : this()
        {
            this.op = op;
        }

        public bool IsDone => op?.IsDone ?? true;

        public T GetAsset<T>()
        {
            return op.GetAsset<T>();
        }

        public List<T> GetAssets<T>()
        {
            return op?.GetAssets<T>();
        }

        public async UniTask Await()
        {
            if (op != null)
                await op.Task;
            await UniTask.CompletedTask;
        }

        public async UniTask<T> AwaitResultAsync<T>()
        {
            await op.Task;
            return await op.GetAssetAsync<T>();
        }

        public async UniTask<IList<T>> AwaitResultsAsync<T>()
        {
            if (op != null)
            {
                await op.Task;
                return await op.GetAssetsAsync<T>();
            }

            return default;
        }

        public Exception Exception => op.Exception;

        public event Action<AssetOperationHandle> Completed
        {
            add => op.Completed += value;
            remove => op.Completed -= value;
        }

        public ProgressStatus GetProgressStatus()
        {
            var set = ReferencePool.Get<HashSet<ProgressStatus>>();
            op.GetProgressStatusSet(set);

            ulong totalBytes = 0;
            ulong completedBytes = 0;
            float percent = 0;
            foreach (var status in set)
            {
                if (status.IsValid)
                {
                    completedBytes += status.CompletedBytes;
                    totalBytes += status.TotalBytes;
                    percent += status.Percent;
                }
            }

            percent /= set.Count;

            set.Clear();
            ReferencePool.Put(set);

            return new ProgressStatus()
            {
                IsValid = true,
                TotalBytes = totalBytes,
                CompletedBytes = completedBytes,
                Percent = percent,
            };
        }

        public void Release()
        {
            DecRef();
        }

        public void AutoRelease(GameObject bindObj)
        {
            if (!bindObj.TryGetComponent<GameObjectDestroyListener>(out var comp))
            {
                comp = bindObj.AddComponent<GameObjectDestroyListener>();
            }

            comp.OnDestroyEvent += Release;
        }

        internal void AddRef()
        {
            var set = ReferencePool.Get<HashSet<int>>();
            op?.AddRef(set);
            set.Clear();
            ReferencePool.Put(set);
        }

        internal void DecRef()
        {
            var set = ReferencePool.Get<HashSet<int>>();
            op?.DecRef(set);
            set.Clear();
            ReferencePool.Put(set);
        }

        public void AwaitSync()
        {
#if UNITY_WEBGL
            throw new Exception("AwaitSync no support webgl");
#endif
            op.WaitForCompleted();
        }

        public T AwaitAssetSync<T>()
        {
#if UNITY_WEBGL
            throw new Exception("AwaitSync no support webgl");
#endif
            op.WaitForCompleted();

            return GetAsset<T>();
        }

        public IList<T> AwaitAssetsSync<T>()
        {
#if UNITY_WEBGL
            throw new Exception("AwaitSync no support webgl");
#endif
            op.WaitForCompleted();

            return GetAssets<T>();
        }
    }
}