using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XGAsset.Runtime;
using XGAsset.Utility;
using XGAsset.Editor.Settings;
using PlayMode = XGAsset.Runtime.PlayMode;

namespace XGAsset.Editor.Settings
{
    [CreateAssetMenu(fileName = "AssetAddressSettings1", menuName = "XGAsset/Create Settings")]
    public class AssetAddressSettings : ScriptableObject
    {
        [SerializeField]
        public AssetAddressPackage DefaultPackage;

        [SerializeField]
        public PlayMode PlayMode;

        [SerializeField]
        public List<string> Labels = new List<string>();
    }
}