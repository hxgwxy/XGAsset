using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using XGAsset.Runtime.Misc;

namespace XGAsset.Runtime.Services
{
    internal class BuildInQueryServices : IBuildInQueryServices
    {
        private string ReleaseDir
        {
            get
            {
#if DEBUG
                return "Debug";
#else
                return "Release";
#endif
            }
        }

        private async UniTask<string> QueryStreamingAssets(string packageName, string fileName)
        {
            var url = Application.platform switch
            {
                RuntimePlatform.WindowsEditor => $"{CommonString.StreamingAssets}/{packageName}/{fileName}",
                RuntimePlatform.Android => $"jar:file://{Application.dataPath}!/assets/{CommonString.TargetString}/{packageName}/{fileName}",
                RuntimePlatform.IPhonePlayer => $"file://{Application.dataPath}/Raw/{CommonString.TargetString}/{packageName}/{fileName}",
                RuntimePlatform.WebGLPlayer => Path.Combine(Application.streamingAssetsPath, CommonString.TargetString, packageName, fileName),
                _ => throw new ArgumentOutOfRangeException()
            };

            var exists = false;
            using var request = UnityWebRequest.Get(new Uri(url));
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                if (operation.webRequest.downloadedBytes > 0)
                {
                    operation.webRequest.Abort();
                    exists = true;
                    break;
                }

                await UniTask.NextFrame();
            }

            if (operation.webRequest.downloadedBytes > 0)
                exists = true;

            operation.webRequest.Abort();
            request.Abort();

            return exists ? url : string.Empty;
        }

        private string QueryPersistentAssets(string packageName, string fileName)
        {
            var path = GetPersistentPath(packageName, fileName);
            return File.Exists(path) ? path : string.Empty;
        }

        public string GetPersistentPath(string packageName, string fileName)
        {
            return $"{Application.persistentDataPath}/{CommonString.TargetString}/{ReleaseDir}/{packageName}/{fileName}";
        }

        public async UniTask<string> QueryAsset(string packageName, string fileName, bool forceCopy = false)
        {
            await CopyToPersistentDataPath(packageName, fileName, forceCopy);
            return QueryPersistentAssets(packageName, fileName);
        }

        private async UniTask CopyToPersistentDataPath(string packageName, string fileName, bool forceCopy = false)
        {
#if UNITY_WEBGL
            return;
#endif
            var path = QueryPersistentAssets(packageName, fileName);
            if (string.IsNullOrEmpty(path) || forceCopy)
            {
                var streamingPath = await QueryStreamingAssets(packageName, fileName);
                if (!string.IsNullOrEmpty(streamingPath))
                {
                    using var request = UnityWebRequest.Get(streamingPath);
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        await UniTask.NextFrame();
                    }


                    if (operation.webRequest.downloadedBytes > 0)
                    {
                        var filePath = GetPersistentPath(packageName, fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        await File.WriteAllBytesAsync(filePath, operation.webRequest.downloadHandler.data);
                    }
                }
            }
        }
    }
}