using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using XGAsset.Runtime.Services;

namespace XGAsset.Runtime
{
    public interface IHostServices
    {
        public bool Enabled { get; }
        public void AddDownloadHost(string host);
        public void SetDownloadHost(string host);
        public string GetMainUrl(string packageName, string fileName);
        public string GetMainUrl(string packageName, string fileName, Dictionary<string, string> param);
    }

    public interface IBuildInQueryServices
    {
        public UniTask<string> QueryAsset(string packageName, string fileName, bool forceCopy = false);
        public string GetPersistentPath(string packageName, string fileName);
        public string GetStreamingAssetsPath(string packageName, string fileName);
    }

    public interface IDownloadServices
    {
        public IDownloadTask DownloadFile(string url, string localPath);
    }

    public interface ILoaderServices
    {
        public UniTask<string> LoadText(string path);
    }
}