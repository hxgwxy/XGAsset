using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace XGAsset.Runtime.Implement
{
    public abstract class AssetImplBase
    {
        public abstract AssetOperationHandle Initialize();
        public abstract AssetOperationHandle LoadScene(string sceneName, LoadSceneMode mode);
        public abstract AssetOperationHandle LoadAsset(string address);
        public abstract AssetOperationHandle LoadAssets(IList<string> address);
        public abstract AssetOperationHandle AddPackage(string name, string version, bool ignoreCache);
        public abstract bool HasAsset(string address);
        public abstract void Unload();
    }
}