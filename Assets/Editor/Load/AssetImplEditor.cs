using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using XGAsset.Editor.Settings;
using XGAsset.Runtime;
using XGAsset.Runtime.Implement;
using XGAsset.Runtime.Provider;
using XGAsset.Editor.Load;
using XGAsset.Editor.Settings;

namespace XGAsset.Editor.Load
{
    public class AssetImplEditor : AssetImplBase
    {
        public override AssetOperationHandle Initialize()
        {
            return ResourcesManager.CreateHandle(new CompleteProvider());
        }

        public override AssetOperationHandle LoadScene(string sceneName, LoadSceneMode mode)
        {
            return ResourcesManager.CreateHandle(new EditorSceneProvider(sceneName, mode));
        }

        public override AssetOperationHandle LoadAsset(string address)
        {
            return ResourcesManager.CreateHandle(new EditorAssetProvider(address));
        }

        public override AssetOperationHandle LoadAssets(List<string> address)
        {
            var provider = new EditorBatchProvider(address);
            var handle = new AssetOperationHandle(provider);
            provider.Start();
            return handle;
        }

        public override AssetOperationHandle AddPackage(string name, string version, bool ignoreCache)
        {
            return ResourcesManager.CreateHandle(new CompleteProvider());
        }

        public override bool HasAsset(string address)
        {
            return AssetAddressDefaultSettings.GetEntry(address) != null;
        }

        public override void Unload()
        {
        }
    }
}