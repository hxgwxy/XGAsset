using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using XGAsset.Runtime.Misc;
using XGAsset.Runtime.Services;

namespace XGAsset.Runtime.Provider
{
    /// <summary>
    /// manifest 定位资源
    /// </summary>
    public class PackageProvider : AsyncOperationBase
    {
        private string _packageName;
        private string _version;
        private string _jsonFileName;
        private string _hashFileName;
        private string _zipFileName;
        private string _zipFileMD5;
        private bool _ignoreCache;
        private IDownloadTask _downloadTask;

        private Dictionary<string, string> urlParams;

        public PackageProvider(string packageName, string version, bool ignoreCache)
        {
            _packageName = packageName;
            _version = version;
            _jsonFileName = $"Manifest_{_packageName}_{_version}.json";
            _hashFileName = $"Manifest_{_packageName}_{_version}.hash";
            _zipFileName = $"Manifest_{_packageName}_{_version}.zip";
            _ignoreCache = ignoreCache;

            if (_ignoreCache)
                urlParams = new Dictionary<string, string> { { "t", DateTime.Now.Ticks.ToString() } };
        }

        protected override void InternalStart()
        {
            var path = ResourcesManager.BuildInQueryServices.GetPersistentPath(_packageName, _jsonFileName);

            if (ResourcesManager.HostServices.Enabled &&
                (_ignoreCache || string.IsNullOrEmpty(path) || !File.Exists(path)))
            {
                StartDownload();
            }
            else
            {
                LoadManifest(path);
            }
        }

        private void StartDownload()
        {
            var url = ResourcesManager.HostServices.GetMainUrl(_packageName, _hashFileName, urlParams);
            var path = ResourcesManager.BuildInQueryServices.GetPersistentPath(_packageName, _hashFileName);
            var hashDownloadTask = ResourcesManager.DownloadServices.DownloadFile(url, path);
            hashDownloadTask.Completed += OnHashDownloadCompleted;
        }

        private void OnHashDownloadCompleted(IDownloadTask task)
        {
            if (task.Success)
            {
                var zipMD5 = File.ReadAllText(task.LocalPath);
                var path = ResourcesManager.BuildInQueryServices.GetPersistentPath(_packageName, _zipFileName);
                var url = ResourcesManager.HostServices.GetMainUrl(_packageName, _zipFileName, urlParams);
                _downloadTask = ResourcesManager.DownloadServices.DownloadFile(url, path);
                _downloadTask.Completed += OnZipDownloadCompleted;
                _downloadTask.SetMD5(zipMD5);
            }
        }

        private void OnZipDownloadCompleted(IDownloadTask task)
        {
            if (_downloadTask.Success)
            {
                var folder = Path.GetDirectoryName(_downloadTask.LocalPath);
                AssetUtility.ExtractZipFile(_downloadTask.LocalPath, folder);
                var jsonFile = $"{folder}/{_jsonFileName}";
                if (File.Exists(jsonFile))
                {
                    LoadManifest(jsonFile);
                }
                else
                {
                    Debug.LogError($"{_packageName}:无法找到json文件 {jsonFile}");
                }
            }
            else
            {
                StartDownload();
            }
        }

        private void LoadManifest(string path)
        {
            if (File.Exists(path))
            {
                ParseJson(File.ReadAllText(path));
            }
            else
            {
                using var request = UnityWebRequest.Get(path);
                var operation = request.SendWebRequest();
                operation.completed += OnLoadManifestCompleted;
            }
        }

        private void LoadFromStreaming()
        {
            var path = ResourcesManager.BuildInQueryServices.GetStreamingAssetsPath(_packageName, _jsonFileName);
            var request = UnityWebRequest.Get(path);
            var operation = request.SendWebRequest();
            operation.completed += OnLoadManifestCompleted;
        }

        private void OnLoadManifestCompleted(AsyncOperation op)
        {
            if (op is UnityWebRequestAsyncOperation operation)
            {
                var text = operation.webRequest.downloadHandler.text;

                operation.webRequest.Dispose();
                if (string.IsNullOrEmpty(text))
                {
                    LoadFromStreaming();
                }
                else
                {
                    ParseJson(text);
                }
            }
        }

        private void ParseJson(string json)
        {
            SetAsset(JsonUtility.FromJson<ManifestData>(json));
            CompleteSuccess();
        }
    }
}