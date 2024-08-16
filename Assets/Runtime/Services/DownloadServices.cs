using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using XGAsset.Runtime.Misc;

namespace XGAsset.Runtime.Services
{
    public interface IDownloadTask : IDisposable
    {
        public AsyncOperation AsyncOperation { get; set; }
        public float Percent { get; }
        public ulong DownloadBytes { get; }
        public ulong TotalBytes { get; }
        public Action<IDownloadTask> Completed { get; set; }
        public bool Success { get; }
        public string ErrorMsg { get; }
        public string LocalPath { get; }
        public void Abort();
        public void SetTotalBytes(ulong size);
        public void SetMD5(string md5);
        public void SetCrc32(string crc32);
    }

    public class DownloadServices : IDownloadServices
    {
        public enum DownloadStatus
        {
            Pending,
            Processing,
            Finish,
            Retry,
        }

        private List<DownloadTaskHandle> _downloadTasks = new List<DownloadTaskHandle>();
        private int _downloadCount;
        private int _maxDownload = 3;
        private bool _running;

        public class DownloadTaskHandle : IDownloadTask
        {
            internal string MD5;
            internal string CRC32;
            public int MaxRetry = 5;
            public int Retry = 0;
            public AsyncOperation AsyncOperation { get; set; }
            public string Url;
            public string LocalPath { get; set; }
            public DownloadStatus Status;
            public string TempLocalPath => LocalPath + ".temp";
            public UnityWebRequest Request;
            public float Percent => 1f * DownloadBytes / TotalBytes;
            public ulong BeginBytes;
            public ulong DownloadBytes { get; set; }
            public ulong TotalBytes { get; internal set; }
            public Action<IDownloadTask> Completed { get; set; }
            public bool Success { get; set; }
            public string ErrorMsg { get; set; }

            public void SetMD5(string md5)
            {
                MD5 = md5;
            }

            public void SetCrc32(string crc32)
            {
                CRC32 = crc32;
            }

            public void Abort()
            {
                throw new NotImplementedException();
            }

            public void SetTotalBytes(ulong size)
            {
                TotalBytes = size;
            }

            public void Dispose()
            {
            }
        }

        public IDownloadTask DownloadFile(string url, string localPath)
        {
            var existsTask = _downloadTasks.Find(task => task.Url.Equals(url) && task.LocalPath.Equals(localPath));

            if (existsTask == null)
            {
                var task = new DownloadTaskHandle()
                {
                    Url = url,
                    LocalPath = localPath,
                };
                _downloadTasks.Add(task);
                existsTask = task;
            }

            RunTask();

            return existsTask;
        }

        private void RunTask()
        {
            var processCount = 0;
            foreach (var task in _downloadTasks)
            {
                if (task.Status == DownloadStatus.Processing)
                {
                    processCount += 1;
                }

                if (processCount >= _maxDownload)
                {
                    return;
                }
            }

            var pendingTask = _downloadTasks.Find(v => v.Status == DownloadStatus.Pending);
            if (pendingTask != null)
            {
                pendingTask.Status = DownloadStatus.Processing;
                ExecTask(pendingTask);
            }
        }

        private void ExecTask(DownloadTaskHandle task)
        {
            var url = task.Url;
            var request = UnityWebRequest.Get(url);
            ulong beginBytes = 0;
            if (File.Exists(task.TempLocalPath))
            {
                beginBytes = (ulong)(new FileInfo(task.TempLocalPath).Length);
            }

            task.Request = request;
            task.DownloadBytes = 0;
            task.BeginBytes = beginBytes;
            request.downloadHandler = new DownloadHandlerFile(task.TempLocalPath, true);
            request.disposeDownloadHandlerOnDispose = true;
            if (beginBytes > 0)
            {
                request.SetRequestHeader("Range", $"bytes={beginBytes}-");
            }

            task.AsyncOperation = request.SendWebRequest();
            task.Status = DownloadStatus.Processing;
            task.AsyncOperation.completed += OnDownloadCompleted;
            Debug.Log($"开始下载: {url}");
        }

        private void OnDownloadCompleted(AsyncOperation op)
        {
            if (op is UnityWebRequestAsyncOperation webRequestAsyncOperation)
            {
                var task = _downloadTasks.Find(v => v.Request == webRequestAsyncOperation.webRequest);
                if (task != null)
                {
                    ProcessTaskCompleted(task);
                }
                else
                {
                    webRequestAsyncOperation.webRequest.Dispose();
                }
            }
        }

        private void ProcessTaskCompleted(DownloadTaskHandle task)
        {
            var request = task.Request;
            var error = request.error;
            var result = request.result;
            var responseCode = request.responseCode;
            var downloadedBytes = request.downloadedBytes;
            request.Dispose();

            task.DownloadBytes = task.BeginBytes + downloadedBytes;
            task.Request = null;

            if (result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"下载成功: {task.Url}");

                if (ValidFile(task))
                {
                    SetTaskSuccess(task);
                }
                else
                {
                    DelayAndRetry(task);
                }
            }
            else
            {
                var msg = $"下载失败 GET:{task.Url}, Error:{error}; Result:{result}; ResponseCode:{responseCode}";
                Debug.LogError(msg);

                switch (responseCode)
                {
                    case 404:
                        SetTaskFailure(task);
                        break;
                    default:
                        if (ValidFile(task))
                        {
                            SetTaskSuccess(task);
                        }
                        else
                        {
                            DelayAndRetry(task);
                        }

                        break;
                }
            }
        }

        private void DelayAndRetry(DownloadTaskHandle task)
        {
            if (++task.Retry < task.MaxRetry)
            {
                ExecTask(task);
            }
            else
            {
                SetTaskFailure(task);
                RunTask();
            }
        }

        private bool ValidFile(DownloadTaskHandle task)
        {
            var fileSize = GetFileSize(task.TempLocalPath);
            if (task.TotalBytes > 0 && task.TotalBytes != fileSize)
            {
                if (fileSize > task.TotalBytes)
                {
                    File.Delete(task.TempLocalPath);
                }

                Debug.LogError($"文件字节数不正确:{task.TempLocalPath} - {fileSize} - {task.TotalBytes}");
                return false;
            }

            if (!string.IsNullOrEmpty(task.CRC32))
            {
                var fileCrc32 = AssetUtility.GetFileCRC32(task.TempLocalPath);
                var isValid = task.CRC32.Equals(fileCrc32);
                if (!isValid)
                {
                    File.Delete(task.TempLocalPath);
                    Debug.LogError($"文件CRC32校验不通过:{task.TempLocalPath}");
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(task.MD5))
            {
                var fileMD5 = AssetUtility.GetFileMD5(task.TempLocalPath);
                var isValid = task.MD5.Equals(fileMD5);
                if (!isValid)
                {
                    File.Delete(task.TempLocalPath);
                    Debug.LogError($"文件MD5校验不通过:{task.TempLocalPath}");
                    return false;
                }
            }

            return true;
        }

        private ulong GetFileSize(string filePath)
        {
            return (ulong)(new FileInfo(filePath).Length);
        }

        private void SetTaskSuccess(DownloadTaskHandle task)
        {
            _downloadTasks.Remove(task);
            File.Delete(task.LocalPath);
            File.Move(task.TempLocalPath, task.LocalPath);
            RunTask();
            task.Success = true;
            task.Completed.Invoke(task);
        }

        private void SetTaskFailure(DownloadTaskHandle task)
        {
            File.Delete(task.TempLocalPath);
            task.Completed.Invoke(task);
        }
    }
}