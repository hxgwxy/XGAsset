using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XGAsset.Runtime.Misc;

namespace XGAsset.Runtime
{
    public class ResourcePackage
    {
        private static Dictionary<string, ManifestData> _manifestDatas;

        private static SortedDictionary<string, AssetBundle> _assetBundles;

        public static AddressInfo GetAddressInfo(string address)
        {
            foreach (var data in _manifestDatas)
            {
                var info = data.Value.AddressInfos.Find(entry => entry.Address.Equals(address));
                if (info != null)
                    return info;
            }

            return null;
        }

        public static AssetBundle GetAssetBundle(string bundleName)
        {
            var bundleInfo = FindBundleDetail(bundleName);

            if (bundleInfo == null)
                return default;

            LoadAssetBundle(bundleName);

            return _assetBundles[bundleInfo.Name];
        }

        private static BundleInfo FindBundleDetail(string bundleName)
        {
            foreach (var data in _manifestDatas)
            {
                var info = data.Value.BundleInfos.Find(bundle => bundle.Name.Equals(bundleName));
                if (info != null)
                    return info;
            }

            return null;
        }

        private static void LoadAssetBundle(string bundleName)
        {
            if (!_assetBundles.ContainsKey(bundleName))
            {
                var useBundleList = new List<string> { bundleName };

                GetDependentAssetBundle(bundleName, useBundleList);

                foreach (var bundle in useBundleList)
                {
                    _assetBundles.Add(bundle, AssetBundle.LoadFromFile($"TestBuild/{bundle}"));
                }
            }
        }

        private static void GetDependentAssetBundle(string bundleName, in List<string> list)
        {
            var bundleInfo = FindBundleDetail(bundleName);

            if (bundleInfo == null)
            {
                return;
            }

            foreach (var dep in bundleInfo.Dependencies)
            {
                if (!list.Contains(dep) && !_assetBundles.ContainsKey(dep))
                {
                    list.Add(dep);
                    GetDependentAssetBundle(dep, list);
                }
            }
        }
    }
}