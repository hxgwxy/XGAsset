using System.Collections.Generic;

namespace XGAsset.Runtime.Provider
{
    public class BatchAssetsProvider : AsyncOperationBase
    {
        public BatchAssetsProvider(IList<string> addressList)
        {
            DependOps ??= new List<IAsyncOperationBase>();
            foreach (var address in addressList)
            {
                DependOps.AddRange(ResourcesManager.CreateAssetProviders(address));
            }
        }
    }
}