using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using XGAsset.Editor.Settings;
using XGAsset.Runtime;
using XGAsset.Runtime.Provider;
using XGAsset.Editor.Settings;

namespace XGAsset.Editor.Load
{
    public class EditorAssetProvider : AsyncOperationBase
    {
        private string _address;

        public EditorAssetProvider(string address)
        {
            _address = address;
        }

        protected override UniTask StartSelf()
        {
            var entry = AssetAddressDefaultSettings.GetEntry(_address);
            if (entry != null)
            {
                var asset = AssetDatabase.LoadAssetAtPath(entry.AssetPath, typeof(UnityEngine.Object));
                if (asset == null)
                {
                    Debug.LogError($"资源不存在 {_address}");
                }
            }
            else
            {
                Debug.LogError($"无法找到地址:{_address}");
            }

            return UniTask.CompletedTask;
        }

        public override UniTask<T> GetAssetAsync<T>()
        {
            return new UniTask<T>(GetAsset<T>());
        }

        public override T GetAsset<T>()
        {
            var entry = AssetAddressDefaultSettings.GetEntry(_address);
            if (entry != null)
                return (T)(object)AssetDatabase.LoadAssetAtPath(entry.AssetPath, typeof(T));
            return default;
        }
    }
}