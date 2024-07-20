using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace XGAsset.Runtime.Implement
{
    internal class AssetImplSimulationEditor : AssetImplRunTime
    {
        public override AssetOperationHandle Initialize()
        {
            ResourcesManager.HostServices.AddDownloadHost("http:/127.0.0.1:59999");
            return base.Initialize();
        }
    }
}