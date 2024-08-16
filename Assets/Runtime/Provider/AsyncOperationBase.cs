using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        /// 处理中
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


    public abstract class AsyncOperationBase : IAsyncOperationBase
    {
        private object mAsset;

        protected UniTaskCompletionSource mSource;

        private List<IAsyncOperationBase> mDependOps;

        protected Action<AssetOperationHandle> mCompleted;

        private OperationStatus mOperationStatus;

        protected Exception mException;

        private int mRefCount;

        protected OperationStatus OperationStatus { get; set; }

        protected object Asset
        {
            get => mAsset;
            set => SetAsset(value);
        }

        public bool IsDone => (OperationStatus == OperationStatus.Succeeded || OperationStatus == OperationStatus.Failure);

        public List<IAsyncOperationBase> DependOps
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

        internal bool IsCanUnload { get; private set; }

        public Exception Exception => mException;

        public int RefCount => mRefCount;

        public virtual string DebugInfo => $"{GetType().Name}";

        public event Action<AssetOperationHandle> Completed
        {
            add
            {
                if (IsDone)
                    value?.Invoke(new AssetOperationHandle(this));
                else
                    mCompleted += value;
            }
            remove => mCompleted -= value;
        }

        protected virtual ProgressStatus GetProgressStatus()
        {
            return new ProgressStatus() { Id = GetHashCode() };
        }

        protected void SetAsset(object asset)
        {
            mAsset = asset;
            // Debug.Log($"operation complete {DebugInfo} {asset}");
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

        public void Start()
        {
            if (OperationStatus == OperationStatus.Pending)
            {
                OperationStatus = OperationStatus.Progress;
                InternalStart();
            }
        }

        public virtual T GetAsset<T>()
        {
            return (T)Convert.ChangeType(Asset, typeof(T));
        }

        public virtual List<T> GetAssets<T>()
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

        public virtual void Unload()
        {
            if (mRefCount <= 0)
            {
                IsCanUnload = true;
            }
        }

        public void WaitForCompleted()
        {
            if (!IsDone)
            {
                if (DependOps != null)
                {
                    foreach (var dependOp in DependOps)
                    {
                        dependOp.WaitForCompleted();
                    }
                }

                InternalWaitForCompleted();
            }
        }

        protected virtual void InternalWaitForCompleted()
        {
        }

        protected virtual void InternalStart()
        {
            StartDepends();
        }

        protected void StartDepends()
        {
            if (DependOps?.Count > 0)
            {
                var depends = 0;
                foreach (var op in DependOps)
                {
                    if (!op.IsDone)
                    {
                        depends++;
                        op.Completed += OnDependCompleted;
                        op.Start();
                    }
                }
                if (depends == 0)
                {
                    ProcessDependsCompleted();
                }
            }
            else
            {
                ProcessDependsCompleted();
            }
        }

        private void OnDependCompleted(AssetOperationHandle handle)
        {
            if (DependOps?.Count > 0)
            {
                foreach (var op in DependOps)
                {
                    if (!op.IsDone)
                    {
                        return;
                    }
                }
            }

            ProcessDependsCompleted();
        }

        protected virtual void ProcessDependsCompleted()
        {
            CompleteSuccess();
        }

        protected virtual void CompleteFailure()
        {
            OperationStatus = OperationStatus.Failure;
            mCompleted?.Invoke(new AssetOperationHandle(this));
            mSource?.TrySetResult();
            mCompleted = null;
        }

        protected virtual void CompleteSuccess()
        {
            OperationStatus = OperationStatus.Succeeded;
            mCompleted?.Invoke(new AssetOperationHandle(this));
            mSource?.TrySetResult();
            mCompleted = null;
        }
    }
}