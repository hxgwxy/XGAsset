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
    internal enum DownloadStatus
    {
        Pending,
        Processing,
        Finish,
        Retry,
    }

    public interface IDownloadTask : IDisposable
    {
        public float Percent { get; }
        public ulong DownloadBytes { get; }
        public ulong TotalBytes { get; }
        public UniTask WaitCompleted();
        public event Action<IDownloadTask> Completed;
        public bool Success { get; }
        public string ErrorMsg { get; }
        public string LocalPath { get; }
        public void Abort();
        public void SetTotalBytes(ulong size);
        public void SetMD5(string md5);
        public void SetCrc32(string crc32);
    }


    internal class DownloadTask : IDownloadTask
    {
        private UniTaskCompletionSource source;
        private Action<IDownloadTask> _complete;

        public string Url;
        public string Error;
        public int Priority;
        public ulong BeginBytes;
        public DownloadStatus Status;
        public UnityWebRequest Request;

        public int MaxRetry = 5;
        public int Retry = 0;

        public float Percent => 1f * DownloadBytes / TotalBytes;

        public ulong DownloadBytes { get; set; }
        public ulong TotalBytes { get; set; }
        public string MD5;
        public string CRC32;

        public event Action<IDownloadTask> Completed
        {
            add
            {
                if (Status == DownloadStatus.Finish)
                    value?.Invoke(this);
                else
                    _complete += value;
            }
            remove => _complete -= value;
        }

        public bool Success { get; set; }

        public string ErrorMsg { get; set; }
        public string LocalPath { get; set; }

        public void Abort()
        {
            Request?.Abort();
            Status = DownloadStatus.Finish;
        }

        public void SetTotalBytes(ulong size)
        {
            TotalBytes = size;
        }

        public void SetMD5(string md5)
        {
            MD5 = md5;
        }

        public void SetCrc32(string crc32)
        {
            CRC32 = crc32;
        }

        public string TempLocalPath => LocalPath + ".temp";

        public UniTask WaitCompleted()
        {
            source ??= new UniTaskCompletionSource();
            return source.Task;
        }

        public void SetCompleted()
        {
            Status = DownloadStatus.Finish;
            _complete?.Invoke(this);
            source?.TrySetResult();
        }

        public void Dispose()
        {
        }
    }

    internal class DownloadServices : IDownloadServices
    {
        private List<DownloadTask> _downloadTasks = new List<DownloadTask>();
        private int _downloadCount;
        private int _maxDownload = 3;
        private bool _running;

        public IDownloadTask DownloadFile(string url, string localPath)
        {
            var existsTask = _downloadTasks.Find(task => task.Url.Equals(url) && task.LocalPath.Equals(localPath));

            if (existsTask == null)
            {
                var task = new DownloadTask()
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

        private async void RunTask()
        {
            if (_running)
            {
                if (_downloadTasks.Any(v => v.Status != DownloadStatus.Pending))
                {
                    return;
                }
            }

            _running = true;
            while (_downloadTasks.Count > 0)
            {
                while (_downloadCount < _maxDownload)
                {
                    var pendingTask = _downloadTasks.Find(v => v.Status == DownloadStatus.Pending);
                    if (pendingTask == null)
                    {
                        break;
                    }

                    ExecTask(pendingTask);
                    _downloadCount++;
                }

                await UniTask.NextFrame();

                for (var i = _downloadTasks.Count - 1; i >= 0; i--)
                {
                    var task = _downloadTasks[i];
                    if (task.Status == DownloadStatus.Finish)
                    {
                        _downloadCount--;
                        _downloadTasks.RemoveAt(i);
                    }
                }
            }

            _running = false;
        }

        private async void ExecTask(DownloadTask task)
        {
            if (task.Status == DownloadStatus.Finish)
            {
                return;
            }

            var url = task.Url;
            var localPath = task.LocalPath;

            using var request = UnityWebRequest.Get(url);

            ulong beginByte = 0;
            if (File.Exists(task.TempLocalPath))
            {
                beginByte = (ulong)(new FileInfo(task.TempLocalPath).Length);
            }

            task.Request = request;
            task.DownloadBytes = 0;
            task.BeginBytes = beginByte;

            request.downloadHandler = new DownloadHandlerFile(task.TempLocalPath, true);
            request.disposeDownloadHandlerOnDispose = true;
            if (beginByte > 0)
                request.SetRequestHeader("Range", $"bytes={beginByte}-");

            var operation = request.SendWebRequest();

            task.Status = DownloadStatus.Processing;

            // Debug.Log($"<color=#00F5FF>下载: {url}</color>");
            Debug.Log($"下载: {url}");

            while (!operation.isDone)
            {
                SetDownloadTotalBytes(task);
                await UniTask.NextFrame();
            }

            SetDownloadTotalBytes(task);

            ProcessTaskCompleted(task);
        }

        private void ProcessTaskCompleted(DownloadTask task)
        {
            var request = task.Request;

            task.Request = null;

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Debug.Log($"<color=#ff7500>下载完成: {task.Url}</color>");
                Debug.Log($"下载完成: {task.Url}");

                if (ValidFile(task))
                {
                    File.Delete(task.LocalPath);
                    File.Move(task.TempLocalPath, task.LocalPath);
                    task.Success = true;
                    task.SetCompleted();
                }
                else
                {
                    DelayAndRetry(task);
                }
            }
            else
            {
                var msg = $"下载失败 GET:{task.Url}, Error:{request.error}; Result:{request.result}; ResponseCode:{request.responseCode}";
                Debug.LogError(msg);

                switch (request.responseCode)
                {
                    case 200:
                        task.MaxRetry++;
                        DelayAndRetry(task);
                        break;
                    case 404:
                        File.Delete(task.TempLocalPath);
                        task.SetCompleted();
                        break;
                    case 503:
                        _maxDownload = Math.Max(2, --_maxDownload);
                        DelayAndRetry(task);
                        break;
                    default:
                        if (ValidFile(task))
                        {
                            File.Delete(task.LocalPath);
                            File.Move(task.TempLocalPath, task.LocalPath);
                            task.Success = true;
                            task.SetCompleted();
                        }
                        else
                        {
                            DelayAndRetry(task);
                        }

                        break;
                }
            }
        }

        private void SetDownloadTotalBytes(DownloadTask task)
        {
            if (task.TotalBytes == 0)
            {
                long.TryParse(task.Request.GetResponseHeader("Content-Length"), out var fileSize);
                if (fileSize > 0)
                {
                    task.TotalBytes = task.BeginBytes + (ulong)fileSize;
                }
            }

            if (task.Request.downloadedBytes > 0)
            {
                task.DownloadBytes = task.BeginBytes + task.Request.downloadedBytes;
            }
        }

        private async void DelayAndRetry(DownloadTask task)
        {
            if (++task.Retry < task.MaxRetry)
            {
                await UniTask.Delay(1000);
                ExecTask(task);
            }
            else
            {
                task.SetCompleted();
            }
        }

        private bool ValidFile(DownloadTask task)
        {
            if (task.TotalBytes > 0)
            {
                var fileSize = GetFileSize(task.TempLocalPath);
                if (task.TotalBytes != fileSize)
                {
                    if (fileSize > task.TotalBytes)
                    {
                        File.Delete(task.TempLocalPath);
                    }

                    Debug.LogError($"文件字节数不正确:{task.TempLocalPath} - {fileSize} - {task.TotalBytes}");
                    return false;
                }
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

            if (!string.IsNullOrEmpty(task.MD5))
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

        public void AbortAll()
        {
            foreach (var task in _downloadTasks)
            {
                task.Abort();
            }
        }
    }
}