using System;
using UnityEngine;

namespace XGAsset.Runtime.Component
{
    public class GameObjectDestroyListener : MonoBehaviour
    {
        public Action DestroyEvent;

        private void OnDestroy()
        {
            DestroyEvent?.Invoke();
            DestroyEvent = null;
        }
    }
}