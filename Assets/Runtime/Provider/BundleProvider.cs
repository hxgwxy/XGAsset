using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
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
        private string _fileName;
        private string _localFile;
        private IDownloadTask _bundleTask;
        private BundleInfo _bundleInfo;
        private UniTaskCompletionSource _source;

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
            };
        }

        public override string DebugInfo => $"{GetType().Name}-{_packageName}-{_bundleName}";

        public BundleProvider(string packageName, string bundleName)
        {
            _packageName = packageName;
            _bundleName = bundleName;
            _fileName = bundleName;
            _bundleInfo = ResourcesManager.GetBundleInfo(_bundleName);
        }

        protected override async UniTask StartSelf()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StartDownload();
#else
            var path = await ResourcesManager.BuildInQueryServices.QueryAsset(_packageName, _fileName);
            if (string.IsNullOrEmpty(path))
            {
                var retry = 3;
                while (retry-- > 0)
                {
                    if (await StartDownload())
                    {
                        break;
                    }
                }

                await LoadBundle();
            }
            else
            {
                _localFile = path;
                await LoadBundle();
            }
#endif
        }

        private UniTask<bool> StartDownload()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return UniTask.FromResult(true);
#else
            var path = ResourcesManager.BuildInQueryServices.GetPersistentPath(_packageName, _fileName);
            var url = ResourcesManager.HostServices.GetMainUrl(_packageName, _fileName);
            var source = new UniTaskCompletionSource<bool>();

            void OnDownloadBundleCompleted(IDownloadTask task)
            {
                source.TrySetResult(ValidFile(_localFile));
            }

            _bundleTask = ResourcesManager.DownloadServices.DownloadFile(url, path);
            _bundleTask.Completed -= OnDownloadBundleCompleted;
            _bundleTask.Completed += OnDownloadBundleCompleted;

            _bundleTask.SetCrc32(_bundleInfo.Crc);
            _bundleTask.SetMD5(_bundleInfo.MD5);
            _bundleTask.SetTotalBytes(_bundleInfo.Size);
            _localFile = _bundleTask.LocalPath;
            return source.Task;
#endif
        }

        private async UniTask LoadBundle()
        {
            var path = _localFile;
#if UNITY_WEBGL && !UNITY_EDITOR
            var request = UnityWebRequestAssetBundle.GetAssetBundle(path);
            request.SendWebRequest();
            var t = new Stopwatch();
            t.Start();
            while (!request.isDone)
            {
                await UniTask.NextFrame();
            }

            t.Stop();
            Debug.Log($"下载ab:{path} {(t.ElapsedMilliseconds * 0.001f).ToString(CultureInfo.InvariantCulture)}s");

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"request is failure:{path}");
            }

            var ab = DownloadHandlerAssetBundle.GetContent(request);
            if (ab == null)
            {
                Debug.LogError($"ab is null:{path}");
            }
            else
            {
                LoadBundleCompleted(ab);
            }
#else
            var request = AssetBundle.LoadFromFileAsync(_localFile, 0);
            _asyncOperation = request;
            if (request.isDone)
            {
                LoadBundleCompleted(request.assetBundle);
            }
            else
            {
                Debug.Log($"加载assetbundle {DebugInfo}");
                var source = new UniTaskCompletionSource();
                request.completed += op =>
                {
                    LoadBundleCompleted(request.assetBundle);
                    OperationStatus = OperationStatus.Succeeded;
                    source.TrySetResult();
                };
                await source.Task;
            }
#endif
        }

        protected override async UniTask StartDepends()
        {
            if (DependOps == null || DependOps.Count == 0)
            {
                await UniTask.CompletedTask;
                return;
            }

            var list = ObjectPool.Get<List<UniTask>>();
            foreach (var op in DependOps)
            {
                if (!op.IsDone)
                {
                    list.Add(op.Start());
                }
            }

            await UniTask.WhenAll(list);

            list.Clear();
            ObjectPool.Put(list);
        }

        private bool ValidFile(string path)
        {
            var valid = true;
            if (!AssetUtility.GetFileCRC32(path).Equals(_bundleInfo.Crc))
            {
                Debug.Log("缓存文件CRC32校验不通过,重新下载");
                valid = false;
            }

            if (!AssetUtility.GetFileMD5(path).Equals(_bundleInfo.MD5))
            {
                Debug.Log("缓存文件CRC32校验不通过,重新下载");
                valid = false;
            }

            if (!valid && File.Exists(path)) File.Delete(path);

            return valid;
        }

        private void LoadBundleCompleted(AssetBundle assetBundle)
        {
            if (assetBundle == null)
            {
                Debug.LogError($"AssetBundle {_bundleName} 无法加载!!!");
                if (File.Exists(_localFile)) File.Delete(_localFile);
                StartDownload();
            }
            else
            {
                LoadAssetBundleMaterialForEditor(assetBundle);
                SetAsset(assetBundle);
            }
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