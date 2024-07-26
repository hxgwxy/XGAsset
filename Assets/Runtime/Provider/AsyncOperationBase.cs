using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XGAsset.Runtime.Pool;

namespace XGAsset.Runtime.Provider
{
    public enum OperationStatus
    {
        /// <summary>
        /// 待定
        /// </summary>
        Pending,

        /// <summary>
        /// 处理自身
        /// </summary>
        Progress,

        /// <summary>
        /// 加载成功
        /// </summary>
        Succeeded,

        /// <summary>
        /// 加载失败
        /// </summary>
        Failure,
    }

    public interface IAsyncOperationBase
    {
        public T GetAsset<T>();

        public IList<T> GetAssets<T>();

        public UniTask<T> GetAssetAsync<T>();

        public UniTask<IList<T>> GetAssetsAsync<T>();

        public bool IsDone { get; }

        public string DebugInfo { get; }

        public event Action<AssetOperationHandle> Completed;

        public IList<IAsyncOperationBase> DependOps { get; set; }

        public UniTask Task { get; }

        public UniTask Start();

        public Exception Exception { get; }

        public int RefCount { get; }
    }

    public abstract class AsyncOperationBase : IAsyncOperationBase
    {
        private object _asset;

        private UniTaskCompletionSource mSource;

        private IList<IAsyncOperationBase> mDependOps;

        private Action<AssetOperationHandle> mComplete;

        private OperationStatus mOperationStatus;

        private Exception mException;

        private int mRefCount;

        protected AsyncOperation mAasyncOperation;

        protected OperationStatus OperationStatus { get; set; }

        protected object Asset
        {
            get => _asset;
            set => _asset = value;
        }

        protected virtual ProgressStatus GetProgressStatus()
        {
            return new ProgressStatus() { id = GetHashCode() };
        }

        protected void SetAsset(object asset)
        {
            _asset = asset;
        }

        internal void GetProgressStatusSet(HashSet<ProgressStatus> set)
        {
            if (set == null)
                return;
            if (set.Add(GetProgressStatus()))
            {
                if (DependOps != null)
                {
                    foreach (var dependOp in DependOps)
                    {
                        (dependOp as AsyncOperationBase)?.GetProgressStatusSet(set);
                    }
                }
            }
        }

        #region public

        public async UniTask Start()
        {
            await StartDepends();
            if (OperationStatus < OperationStatus.Progress)
            {
                OperationStatus = OperationStatus.Progress;
                await StartSelf();
            }

            OperationStatus = OperationStatus.Succeeded;
            mComplete?.Invoke(new AssetOperationHandle(this));
            mSource?.TrySetResult();
        }

        protected virtual async UniTask StartDepends()
        {
            if (DependOps == null || DependOps.Count == 0)
            {
                await UniTask.CompletedTask;
                return;
            }

            var list = ReferencePool.Get<List<UniTask>>();
            foreach (var op in DependOps)
            {
                if (!op.IsDone)
                {
                    list.Add(op.Start());
                }
            }

            await UniTask.WhenAll(list);

            list.Clear();
            ReferencePool.Put(list);
        }

        protected virtual UniTask StartSelf()
        {
            return UniTask.CompletedTask;
        }

        public virtual T GetAsset<T>()
        {
            return (T)Convert.ChangeType(Asset, typeof(T));
        }

        public virtual IList<T> GetAssets<T>()
        {
            if (DependOps != null)
            {
                var list = new List<T>();
                foreach (var depOp in DependOps)
                {
                    list.Add(depOp.GetAsset<T>());
                }

                return list;
            }

            return default;
        }

        public virtual UniTask<T> GetAssetAsync<T>()
        {
            throw new NotImplementedException();
        }

        public virtual async UniTask<IList<T>> GetAssetsAsync<T>()
        {
            if (DependOps != null)
            {
                var list = new List<UniTask<T>>();
                foreach (var depOp in DependOps)
                {
                    list.Add(depOp.GetAssetAsync<T>());
                }

                return await list;
            }

            return default;
        }

        public bool IsDone
        {
            get
            {
                if (mAasyncOperation != null)
                {
                    _asset = (mAasyncOperation as AssetBundleCreateRequest)?.assetBundle; // 同步加载的关键
                    if (mAasyncOperation.isDone)
                    {
                        OperationStatus = OperationStatus.Succeeded;
                    }
                }

                if (DependOps != null)
                {
                    foreach (var op in DependOps)
                    {
                        if (!op.IsDone)
                        {
                            return false;
                        }
                    }
                }

                return (OperationStatus == OperationStatus.Succeeded || OperationStatus == OperationStatus.Failure);
            }
        }

        public virtual string DebugInfo => $"{GetType().Name}";

        public event Action<AssetOperationHandle> Completed
        {
            add
            {
                if (IsDone)
                    value?.Invoke(new AssetOperationHandle(this));
                else
                    mComplete += value;
            }
            remove => mComplete -= value;
        }

        public IList<IAsyncOperationBase> DependOps
        {
            get => mDependOps;
            set => mDependOps = value;
        }

        public UniTask Task
        {
            get
            {
                if (IsDone)
                    return UniTask.CompletedTask;
                mSource ??= new UniTaskCompletionSource();
                return mSource.Task;
            }
        }

        public Exception Exception => mException;

        public int RefCount => mRefCount;

        public virtual void AddRef(HashSet<int> set)
        {
            if (set.Add(GetHashCode()))
            {
                mRefCount++;
                if (DependOps != null)
                {
                    foreach (var dependOp in DependOps)
                    {
                        (dependOp as AsyncOperationBase)?.AddRef(set);
                    }
                }
            }
        }

        public virtual void DecRef(HashSet<int> set)
        {
            if (set.Add(GetHashCode()))
            {
                mRefCount--;
                if (DependOps != null)
                {
                    foreach (var dependOp in DependOps)
                    {
                        (dependOp as AsyncOperationBase)?.DecRef(set);
                    }
                }
            }
        }

        internal bool IsCanUnload { get; private set; }

        public virtual void Unload()
        {
            if (mRefCount <= 0)
            {
                IsCanUnload = true;
            }
        }

        #endregion
    }
}