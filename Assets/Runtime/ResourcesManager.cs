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

    public class ResourcesManager
    {
        private static Dictionary<string, ManifestData> packages = new Dictionary<string, ManifestData>();
        private static Dictionary<string, AddressInfoWrap> addressInfoWraps = new Dictionary<string, AddressInfoWrap>(10000);
        private static Dictionary<string, BundleInfo> bundleInfos = new Dictionary<string, BundleInfo>();
        private static Dictionary<string, BundleProvider> bundleProviders = new Dictionary<string, BundleProvider>();
        private static Dictionary<string, AssetProvider> assetProviders = new Dictionary<string, AssetProvider>();
        private static Dictionary<string, SceneProvider> sceneProviders = new Dictionary<string, SceneProvider>();

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
        public static ILoaderServices LoaderServices = new LoaderServices();

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
            var packageName = GetAssetPackageName(sceneName);

            if (!sceneProviders.TryGetValue(sceneName, out var provider))
            {
                provider = new SceneProvider(sceneName, mode)
                {
                    DependOps = CreateBundleProvider(packageName, sceneName)
                };
                sceneProviders.Add(sceneName, provider);
            }

            var handle = new AssetOperationHandle(provider);
            provider.Start();
            return handle;
        }

        public static AssetOperationHandle CreateAssetHandle(string address)
        {
            var dependOps = new List<IAsyncOperationBase>();
            CreateAssetProviderHandles(address, dependOps);

            if (dependOps.Count > 0)
            {
                var op = dependOps[0];
                var handle = new AssetOperationHandle(op as AsyncOperationBase);
                op.Start();
                return handle;
            }

            return new AssetOperationHandle();
        }

        public static AssetOperationHandle CreateAssetHandle(IList<string> address)
        {
            var dependOps = new List<IAsyncOperationBase>();
            foreach (var addr in address)
            {
                CreateAssetProviderHandles(addr, dependOps);
            }

            var batchOp = new BatchProvider
            {
                DependOps = dependOps
            };
            var handle = new AssetOperationHandle(batchOp);
            batchOp.Start();
            return handle;
        }

        private static void CreateAssetProviderHandles(string address, List<IAsyncOperationBase> dependsOps)
        {
            var list = ObjectPool.Get<List<AddressInfo>>();

            GetAddressInfos(address, list);

            if (list.Count == 0)
            {
                Debug.LogError($"无法找到地址:{address}");
            }

            foreach (var addressInfo in list)
            {
                var assetProvider = InternalCreateAssetProviderHandle(addressInfo);
                dependsOps.Add(assetProvider);
            }

            ObjectPool.Put(list, (a) => { a.Clear(); });
        }

        private static AsyncOperationBase InternalCreateAssetProviderHandle(AddressInfo info)
        {
            if (!assetProviders.TryGetValue(info.Address, out var provider))
            {
                provider = new AssetProvider(info)
                {
                    DependOps = CreateBundleProvider(info.PackageName, info.Address)
                };
                assetProviders.Add(info.Address, provider);
            }

            return provider;
        }

        private static List<IAsyncOperationBase> CreateBundleProvider(string packageName, string address)
        {
            var bundleInfo = GetBundleInfoByAddress(address);
            if (bundleInfo != null)
            {
                var op = InternalCreateBundleProvider(packageName, bundleInfo.Name);
                op.DependOps ??= CreateBundleDependProvider(packageName, bundleInfo.Name);

                return new List<IAsyncOperationBase>() { op };
            }

            return null;
        }

        private static List<IAsyncOperationBase> CreateBundleDependProvider(string packageName, string bundleName)
        {
            var list = new List<string>() { };
            GetDependentAssetBundle(bundleName, list);
            list.Remove(bundleName);
            var providers = new List<IAsyncOperationBase>();

            if (list.Count > 0)
            {
                foreach (var depName in list)
                {
                    providers.Add(InternalCreateBundleProvider(packageName, depName));
                }
            }

            return providers;
        }

        private static IAsyncOperationBase InternalCreateBundleProvider(string packageName, string bundleName)
        {
            if (!bundleProviders.TryGetValue(bundleName, out var bundleProvider))
            {
                bundleProvider = new BundleProvider(packageName, bundleName);
                bundleProviders.Add(bundleName, bundleProvider);
            }

            return bundleProvider;
        }

        private static string GetAssetPackageName(string address)
        {
            return GetAddressInfo(address)?.PackageName ?? string.Empty;
        }

        private static void GetDependentAssetBundle(string bundleName, in List<string> list)
        {
            var bundleInfo = GetBundleInfo(bundleName);

            if (bundleInfo == null)
            {
                return;
            }

            foreach (var dep in bundleInfo.Dependencies)
            {
                if (!list.Contains(dep))
                {
                    list.Add(dep);
                    GetDependentAssetBundle(dep, list);
                }
            }
        }

        public static AddressInfo GetAddressInfo(string address)
        {
            var list = ObjectPool.Get<List<AddressInfo>>();
            GetAddressInfos(address, list);
            var info = list.Count > 0 ? list[0] : null;
            ObjectPool.Put<List<AddressInfo>>(list, infos => infos.Clear());
            return info;
        }

        private static void GetAddressInfos(string address, List<AddressInfo> list)
        {
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
        }

        internal static BundleInfo GetBundleInfo(string bundleName)
        {
            bundleInfos.TryGetValue(bundleName, out var bundleInfo);
            return bundleInfo;
        }

        private static BundleInfo GetBundleInfoByAddress(string address)
        {
            var addressInfo = GetAddressInfo(address);
            return addressInfo != null ? bundleInfos[addressInfo.BundleName] : null;
        }

        // private static void InternalUnload( Dictionary<string, IAsyncOperationBase> providers)
        // {
        //     foreach (var provider in providers)
        //     {
        //         ((AsyncOperationBase) provider.Value).Unload();
        //     }
        // }

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