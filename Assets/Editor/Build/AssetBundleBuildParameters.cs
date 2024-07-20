using System;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEngine;

namespace XGAsset.Editor.Build
{
    [Serializable]
    public class AssetBundleBuildParameters : BundleBuildParameters
    {
        public AssetBundleBuildParameters(BuildTarget target, BuildTargetGroup group, string outputFolder) : base(target, group, outputFolder)
        {
        }

        public override BuildCompression GetCompressionForIdentifier(string identifier)
        {
            // // BundleCompression = BuildCompression.LZMA, // 默认，占用磁盘空间小，只支持顺序读取，所以加载AB包时，需要将整个包解压，适用于一个完整的资源单独一个包的情况, 缺点是解压占用CPU容易卡顿
            // BundleCompression = BuildCompression.LZ4, // 占用磁盘空间大，分块加载效率高，适用于多个资源打在一个ab包的情况
            return base.GetCompressionForIdentifier(identifier);
        }
    }
}