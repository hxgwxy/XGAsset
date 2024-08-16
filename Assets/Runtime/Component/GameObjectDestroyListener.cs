using System;
using UnityEngine;

namespace XGAsset.Runtime.Component
{
    public class GameObjectDestroyListener : MonoBehaviour
    {
        private GameObject m_Self;
        public Action OnDestroyEvent;
        public Action<GameObject> OnDestroyWithObject;

        private void Awake()
        {
            m_Self = gameObject;
        }

        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
            OnDestroyEvent = null;

            OnDestroyWithObject?.Invoke(m_Self);
            OnDestroyWithObject = null;
        }
    }
}