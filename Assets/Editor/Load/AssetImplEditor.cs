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

        public override AssetOperationHandle LoadAssets(IList<string> address)
        {
            var list = new List<AssetAddressEntry>();
            foreach (var addr in address)
            {
                list.AddRange(AssetAddressDefaultSettings.GetEntries(addr));
            }

            var batchOp = new EditorBatchProvider()
            {
                DependOps = list.Select(entry => new EditorAssetProvider(entry.Address)).Cast<IAsyncOperationBase>().ToList()
            };
            var handle = new AssetOperationHandle(batchOp);
            batchOp.Start();
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