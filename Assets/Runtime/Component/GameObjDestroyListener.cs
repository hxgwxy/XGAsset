using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace XGAsset.Runtime
{
    public class GameObjDestroyListener : MonoBehaviour
    {
        public Action Destroy;

        private void OnDestroy()
        {
            Destroy?.Invoke();
            Destroy = null;
        }
    }
}