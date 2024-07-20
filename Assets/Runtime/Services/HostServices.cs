using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XGAsset.Runtime.Misc;

namespace XGAsset.Runtime.Services
{
    internal class HostServices : IHostServices
    {
        private List<string> _urls = new List<string>() { };

        private RuntimeSettings _settings;

        public bool Enabled => _urls.Count > 0;

        public void AddDownloadHost(string host)
        {
            _urls ??= new List<string>();
            if (!_urls.Contains(host))
            {
                _urls.Add(host);
            }

            LoadConfig();
        }

        public void SetDownloadHost(string host)
        {
            _urls ??= new List<string>();
            _urls.Clear();
            AddDownloadHost(host);
        }

        private void LoadConfig()
        {
            _settings ??= Resources.Load<RuntimeSettings>(CommonString.RuntimeSettingsLoadPath);
        }

        public string GetMainUrl(string packageName, string fileName)
        {
            if (Enabled)
            {
                LoadConfig();
                var setting = _settings.PackageSettings.FirstOrDefault(v => packageName.Equals(v.PackageName));
                if (setting != null)
                {
                    return $"{_urls[0]}/{StringPlaceholderUtil.GetString(setting.RemoteLoadPath)}/{fileName}";
                }
                else
                {
                    Debug.LogError($"{packageName}缺少运行时配置, 必须构建一次资源!");
                }
            }

            return string.Empty;
        }

        public string GetMainUrl(string packageName, string fileName, Dictionary<string, string> param = null)
        {
            var url = GetMainUrl(packageName, fileName);
            if (!string.IsNullOrEmpty(url) && param != null)
            {
                var paramstr = string.Empty;
                foreach (var item in param)
                {
                    paramstr += $"{item.Key}={item.Value}&";
                }

                if (!string.IsNullOrEmpty(paramstr))
                {
                    url = $"{url}?{paramstr.Substring(0, paramstr.Length - 1)}";
                }
            }

            return url;
        }
    }
}