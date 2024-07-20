using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XGAsset.Runtime.Misc;

namespace XGAsset.Runtime.Provider
{
    public sealed class AssetProvider : AsyncOperationBase
    {
        private AddressInfo _addressInfo;

        public override string DebugInfo => $"{GetType().Name}-{_addressInfo.Address}";

        public AssetProvider(AddressInfo addressInfo)
        {
            _addressInfo = addressInfo;
        }

        public override T GetAsset<T>()
        {
            var asset = DependOps[0].GetAsset<AssetBundle>()?.LoadAsset(_addressInfo.AssetPath, typeof(T));
            if (asset != null)
                return (T)Convert.ChangeType(asset, typeof(T));
            Debug.LogError($"{DebugInfo}无法获取资源,{typeof(T)}");
            return default;
        }

        public override async UniTask<T> GetAssetAsync<T>()
        {
            if (Asset != null)
                return (T)Convert.ChangeType(Asset, typeof(T));

            var bundleRequest = DependOps[0].GetAsset<AssetBundle>()?.LoadAssetAsync(_addressInfo.AssetPath, typeof(T));
            if (bundleRequest != null)
            {
                await bundleRequest;
                if (bundleRequest.asset)
                {
                    Asset = bundleRequest.asset;
                    return (T)Convert.ChangeType(bundleRequest.asset, typeof(T));
                }
            }

            Debug.LogError($"{DebugInfo}无法获取资源,{typeof(T)}");
            return default;
        }
    }
}