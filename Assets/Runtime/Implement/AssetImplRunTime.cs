using System.Collections.Generic;
using UnityEngine.SceneManagement;
using XGAsset.Runtime.Provider;

namespace XGAsset.Runtime.Implement
{
    internal class AssetImplRunTime : AssetImplBase
    {
        public override AssetOperationHandle Initialize()
        {
            var handle = ResourcesManager.CreateHandle(new CompleteProvider());
            return handle;
        }

        public override AssetOperationHandle LoadScene(string sceneName, LoadSceneMode mode)
        {
            var handle = ResourcesManager.CreateSceneHandle(sceneName, mode);
            handle.AddRef();
            return handle;
        }

        public override AssetOperationHandle LoadAsset(string address)
        {
            var handle = ResourcesManager.CreateAssetHandle(address);
            handle.AddRef();
            return handle;
        }

        public override AssetOperationHandle LoadAssets(IList<string> address)
        {
            var handle = ResourcesManager.CreateAssetHandle(address);
            handle.AddRef();
            return handle;
        }

        public override AssetOperationHandle AddPackage(string packageName, string version, bool ignoreCache = false)
        {
            var handle = ResourcesManager.AddPackage(packageName, version, ignoreCache);
            return handle;
        }

        public override bool HasAsset(string address)
        {
            return ResourcesManager.GetAddressInfo(address) != null;
        }

        public override void Unload()
        {
            ResourcesManager.Unload();
        }
    }
}