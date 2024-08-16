using System.Collections.Generic;
using System.Linq;
using XGAsset.Editor.Settings;
using XGAsset.Runtime.Provider;

namespace XGAsset.Editor.Load
{
    public class EditorBatchProvider : AsyncOperationBase
    {
        public EditorBatchProvider(List<string> address)
        {
            var list = new List<AssetAddressEntry>();
            foreach (var addr in address)
            {
                list.AddRange(AssetAddressDefaultSettings.GetEntries(addr));
            }
            DependOps = list.Select(entry => new EditorAssetProvider(entry.Address)).Cast<IAsyncOperationBase>().ToList();
        }
    }
}