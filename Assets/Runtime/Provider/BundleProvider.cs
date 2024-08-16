using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using XGAsset.Runtime.Misc;
using XGAsset.Runtime.Pool;
using XGAsset.Runtime.Services;
using Debug = UnityEngine.Debug;

namespace XGAsset.Runtime.Provider
{
    public sealed class BundleProvider : AsyncOperationBase
    {
        private string _bundleName;
        private string _packageName;
        private IDownloadTask _bundleTask;
        private BundleInfo _bundleInfo;
        private UniTaskCompletionSource _source;
        private AsyncOperation _loadBuiltInOperation;
        private AsyncOperation _downloadOperation;
        private AsyncOperation _loadBundledOperation;

        private ulong DownloadBytes
        {
            get
            {
                if (IsDone)
                    return _bundleInfo.Size;
                return _bundleTask?.DownloadBytes ?? 0;
            }
        }

        private ulong TotalBytes
        {
            get
            {
                if (IsDone)
                    return _bundleInfo.Size;
                return _bundleTask?.TotalBytes ?? _bundleInfo.Size;
            }
        }

        private ulong _totalBytes;

        protected override ProgressStatus GetProgressStatus()
        {
            return new ProgressStatus()
            {
                IsValid = true,
                CompletedBytes = DownloadBytes,
                TotalBytes = TotalBytes,
                Percent = TotalBytes == 0 ? 0 : (float)DownloadBytes / (float)TotalBytes
            };
        }

        public override string DebugInfo => $"{GetType().Name}-{_packageName}-{_bundleName}";

        public BundleProvider(string packageName, string bundleName)
        {
            _packageName = packageName;
            _bundleName = bundleName;
            _bundleInfo = ResourcesManager.GetBundleInfo(_bundleName);
            DependOps = ResourcesManager.CreateDependBundleProvider(packageName, bundleName);
        }

        protected override void InternalStart()
        {
            var path = ResourcesManager.BuildInQueryServices.GetPersistentPath(_packageName, _bundleName);
            _bundleInfo = ResourcesManager.GetBundleInfo(_bundleName);

            if (File.Exists(path))
                LoadFromFile(path);
            else
                LoadFromBuiltId();
        }

        private void LoadFromBuiltId()
        {
            var streamingPath = ResourcesManager.BuildInQueryServices.GetStreamingAssetsPath(_packageName, _bundleName);
            var request = UnityWebRequestAssetBundle.GetAssetBundle(streamingPath);
            _loadBuiltInOperation = request.SendWebRequest();
            _loadBuiltInOperation.completed += OnLoadFromBuiltIdComplete;
        }

        private void OnLoadFromBuiltIdComplete(AsyncOperation op)
        {
            if (op is UnityWebRequestAsyncOperation asyncOperation)
            {
                if (string.IsNullOrEmpty(asyncOperation.webRequest.error))
                {
                    SetAsset(DownloadHandlerAssetBundle.GetContent(asyncOperation.webRequest));
                    StartDepends();
                }
                else
                {
                    LoadFromDownload();
                }

                asyncOperation.webRequest.Dispose();
            }
        }

        private void LoadFromDownload()
        {
            var url = ResourcesManager.HostServices.GetMainUrl(_packageName, _bundleName);
            var path = ResourcesManager.BuildInQueryServices.GetPersistentPath(_packageName, _bundleName);
            _bundleTask = ResourcesManager.DownloadServices.DownloadFile(url, path);
            _bundleTask.Completed -= OnDownloadBundleCompleted;
            _bundleTask.Completed += OnDownloadBundleCompleted;
            _bundleTask.SetCrc32(_bundleInfo.Crc);
            _bundleTask.SetMD5(_bundleInfo.MD5);
            _bundleTask.SetTotalBytes(_bundleInfo.Size);
            _downloadOperation = _bundleTask.AsyncOperation;
        }

        private void OnDownloadBundleCompleted(IDownloadTask task)
        {
            LoadFromFile(task.LocalPath);
        }

        private void LoadFromFile(string path)
        {
            _loadBundledOperation = AssetBundle.LoadFromFileAsync(path, 0);
            _loadBundledOperation.completed += OnLoadBundleCompleted;
        }

        private void OnLoadBundleCompleted(AsyncOperation op)
        {
            if (op is AssetBundleCreateRequest operation)
            {
                SetAsset(operation.assetBundle);
                StartDepends();
            }
        }

        protected override void ProcessDependsCompleted()
        {
            LoadAssetBundleMaterialForEditor((AssetBundle)Asset);
            CompleteSuccess();
        }

        public override void Unload()
        {
            base.Unload();
            if (IsCanUnload)
            {
                GetAsset<AssetBundle>().Unload(true);
                Debug.Log($"卸载AssetBundle:{_bundleName}");
            }
        }

        protected override void InternalWaitForCompleted()
        {
            if (_loadBuiltInOperation is UnityWebRequestAsyncOperation op)
            {
                while (!op.isDone)
                {
                    var a = op.webRequest.downloadHandler.data;
                    Thread.Sleep(1);
                }
            }
            if (_downloadOperation is UnityWebRequestAsyncOperation op2)
            {
                while (!op2.isDone)
                {
                    var a = op2.webRequest.downloadHandler.data;
                    Thread.Sleep(1);
                }
            }
            if (_loadBundledOperation is AssetBundleCreateRequest op3)
            {
                while (!op3.isDone)
                {
                    var a = op3.assetBundle;
                    Thread.Sleep(1);
                }
            }
        }


        [Conditional("UNITY_EDITOR")]
        private async void LoadAssetBundleMaterialForEditor(AssetBundle assetBundle)
        {
            if (!assetBundle.isStreamedSceneAssetBundle)
            {
                var request = assetBundle.LoadAllAssetsAsync<Material>();
                await request;
                var materials = request.allAssets.Select(v => (Material)v).ToList();
                foreach (var material in materials)
                {
                    var shader = Shader.Find(material.shader.name);
                    if (shader && !shader.name.Contains("Error"))
                    {
                        material.shader = shader;
                    }
                }
            }
        }
    }
}