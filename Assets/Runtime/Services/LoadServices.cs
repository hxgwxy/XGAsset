using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace XGAsset.Runtime.Services
{
    internal class LoaderServices : ILoaderServices
    {
        public async UniTask<string> LoadText(string path)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            Debug.Log($"LoaderServices Begin:{path}");
            using var request = UnityWebRequest.Get(path);
            var operation = request.SendWebRequest();
            try
            {
                await operation;
            }
            catch (Exception e)
            {
                Debug.Log($"{e.Message}, {path}");
            }

            Debug.Log($"LoaderServices Completed:{path}");
            Debug.Log($"LoaderServices Result:{operation.webRequest.downloadHandler.text}");
            Debug.Log($"LoaderServices Status:{operation.isDone},{request.result},{request.error}");

            return operation.webRequest.downloadHandler.text;
        }
    }
}