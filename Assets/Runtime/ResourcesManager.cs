using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using XGAsset.Runtime.Misc;
using XGAsset.Runtime.Pool;
using XGAsset.Runtime.Provider;
using XGAsset.Runtime.Services;

namespace XGAsset.Runtime
{
    internal class AddressInfoWrap
    {
        public bool IsArray => AddressInfos != null && AddressInfos.Count > 0;
        public AddressInfo AddressInfo;
        public List<AddressInfo> AddressInfos;

        public void AddToList(AddressInfo info)
        {
            AddressInfos ??= new List<AddressInfo>();
            if (!AddressInfos.Contains(info))
                AddressInfos.Add(info);
        }
    }

    public static class ResourcesManager
    {
        private static Dictionary<string, ManifestData> packages = new Dictionary<string, ManifestData>();
        private static Dictionary<string, AddressInfoWrap> addressInfoWraps = new Dictionary<string, AddressInfoWrap>(200);
        private static Dictionary<string, BundleInfo> bundleInfos = new Dictionary<string, BundleInfo>(10);
        private static Dictionary<string, BundleProvider> bundleProviders = new Dictionary<string, BundleProvider>(10);
        private static Dictionary<string, AssetProvider> assetProviders = new Dictionary<string, AssetProvider>(10);
        private static Dictionary<string, SceneProvider> sceneProviders = new Dictionary<string, SceneProvider>(10);

#if UNITY_EDITOR
        internal static Dictionary<string, AddressInfoWrap> AddressInfoWraps => addressInfoWraps;
        internal static Dictionary<string, ManifestData> Packages => packages;
        internal static Dictionary<string, BundleInfo> BundleInfos => bundleInfos;
        internal static Dictionary<string, BundleProvider> BundleProviders => bundleProviders;
        internal static Dictionary<string, AssetProvider> AssetProviders => assetProviders;
        internal static Dictionary<string, SceneProvider> SceneProviders => sceneProviders;
#endif

        public static IHostServices HostServices = new HostServices();
        public static IBuildInQueryServices BuildInQueryServices = new BuildInQueryServices();
        public static IDownloadServices DownloadServices = new DownloadServices();

        public static AssetOperationHandle AddPackage(string packageName, string version, bool ignoreCache)
        {
            var handle = CreateHandle(new PackageProvider(packageName, version, ignoreCache));

            handle.Completed += OnPackageLoadCompleted;

            return handle;
        }

        private static void OnPackageLoadCompleted(AssetOperationHandle handle)
        {
            if (handle.GetAsset<ManifestData>() is { } data)
            {
                if (!packages.TryGetValue(data.PackageName, out var package))
                {
                    Debug.Log($"添加资源清单:{data.PackageName}-{data.Version}");

                    packages.Add(data.PackageName, data);

                    foreach (var bundleInfo in data.BundleInfos)
                    {
                        if (!bundleInfos.TryGetValue(bundleInfo.Name, out var value))
                        {
                            bundleInfos.Add(bundleInfo.Name, bundleInfo);
                        }
                        else if (!bundleInfo.MD5.Equals(value.MD5))
                        {
                            Debug.LogError($"重复的bundleInfo:{bundleInfo.Name}");
                        }
                    }

                    foreach (var addressInfo in data.AddressInfos)
                    {
                        addressInfo.PackageName = data.PackageName;

                        if (!addressInfoWraps.TryGetValue(addressInfo.Address, out var value))
                        {
                            value = new AddressInfoWrap
                            {
                                AddressInfo = addressInfo
                            };
                            addressInfoWraps.Add(addressInfo.Address, value);
                        }

                        foreach (var label in addressInfo.Label)
                        {
                            if (!addressInfoWraps.TryGetValue(label, out var labelValue))
                            {
                                labelValue = new AddressInfoWrap();
                                addressInfoWraps.Add(label, labelValue);
                            }

                            labelValue.AddToList(addressInfo);
                        }
                    }
                }
                else
                {
                    ///TODO 更新manifest
                }
            }
        }

        public static AssetOperationHandle CreateHandle<T>(T op) where T : AsyncOperationBase
        {
            var handle = new AssetOperationHandle(op);
            op.Start();
            return handle;
        }

        public static AssetOperationHandle CreateSceneHandle(string sceneName, LoadSceneMode mode)
        {
            if (!sceneProviders.TryGetValue(sceneName, out var provider))
            {
                provider = new SceneProvider(sceneName, mode);
                sceneProviders.Add(sceneName, provider);
            }

            var handle = new AssetOperationHandle(provider);
            provider.Start();
            return handle;
        }

        public static AssetOperationHandle CreateAssetHandle(string address)
        {
            var provider = CreateAssetProvider(address);
            var handle = new AssetOperationHandle(provider);
            provider.Start();
            return handle;
        }

        public static AssetOperationHandle CreateAssetHandle(IList<string> address)
        {
            var provider = new BatchAssetsProvider(address);
            var handle = new AssetOperationHandle(provider);
            provider.Start();
            return handle;
        }

        internal static AsyncOperationBase CreateAssetProvider(string address)
        {
            var info = GetAddressInfo(address);
            if (!assetProviders.TryGetValue(info.Address, out var provider))
            {
                provider = new AssetProvider(info);
                assetProviders.Add(info.Address, provider);
            }

            return provider;
        }

        internal static List<AsyncOperationBase> CreateAssetProviders(string address)
        {
            var infos = GetAddressInfos(address);
            var list = new List<AsyncOperationBase>();
            foreach (var info in infos)
            {
                if (!assetProviders.TryGetValue(info.Address, out var provider))
                {
                    provider = new AssetProvider(info);
                    assetProviders.Add(info.Address, provider);
                }

                list.Add(provider);
            }

            return list;
        }

        internal static AsyncOperationBase CreateBundleProvider(string packageName, string bundleName)
        {
            if (!bundleProviders.TryGetValue(bundleName, out var bundleProvider))
            {
                bundleProvider = new BundleProvider(packageName, bundleName);
                bundleProviders.Add(bundleName, bundleProvider);
            }

            return bundleProvider;
        }

        internal static List<IAsyncOperationBase> CreateDependBundleProvider(string packageName, string bundleName)
        {
            var hashSet = ReferencePool.Get<HashSet<string>>();
            GetDependentAssetBundle(bundleName, hashSet);
            hashSet.Remove(bundleName);
            var providers = new List<IAsyncOperationBase>();
            foreach (var depName in hashSet)
            {
                providers.Add(CreateBundleProvider(packageName, depName));
            }

            return providers;
        }

        private static void GetDependentAssetBundle(string bundleName, HashSet<string> hashSet)
        {
            var bundleInfo = GetBundleInfo(bundleName);

            if (bundleInfo != null)
            {
                foreach (var dep in bundleInfo.Dependencies)
                {
                    if (!hashSet.Contains(dep))
                    {
                        hashSet.Add(dep);
                        GetDependentAssetBundle(dep, hashSet);
                    }
                }
            }
        }

        public static AddressInfo GetAddressInfo(string address)
        {
            var list = GetAddressInfos(address);
            var info = list.Count > 0 ? list[0] : null;
            ReferencePool.Put(list);
            return info;
        }

        private static List<AddressInfo> GetAddressInfos(string address)
        {
            var list = ReferencePool.Get<List<AddressInfo>>();
            if (addressInfoWraps.ContainsKey(address))
            {
                if (addressInfoWraps[address].IsArray)
                {
                    foreach (var addressInfo in addressInfoWraps[address].AddressInfos)
                    {
                        list.Add(addressInfo);
                    }
                }
                else
                {
                    list.Add(addressInfoWraps[address].AddressInfo);
                }
            }

            return list;
        }

        internal static BundleInfo GetBundleInfo(string bundleName)
        {
            bundleInfos.TryGetValue(bundleName, out var bundleInfo);
            return bundleInfo;
        }

        public static void Unload()
        {
            foreach (var provider in assetProviders)
            {
                provider.Value.Unload();
            }

            foreach (var provider in sceneProviders)
            {
                provider.Value.Unload();
            }

            foreach (var provider in bundleProviders)
            {
                provider.Value.Unload();
            }

            var keys = assetProviders.Keys.ToList();
            foreach (var t in keys)
            {
                if (assetProviders[t].IsCanUnload)
                    assetProviders.Remove(t);
            }

            keys = sceneProviders.Keys.ToRefList(keys);
            foreach (var t in keys)
            {
                if (sceneProviders[t].IsCanUnload)
                    sceneProviders.Remove(t);
            }

            keys = bundleProviders.Keys.ToRefList(keys);
            foreach (var t in keys)
            {
                if (bundleProviders[t].IsCanUnload)
                    bundleProviders.Remove(t);
            }
        }
    }
}