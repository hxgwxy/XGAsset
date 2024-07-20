using System;
using System.Linq;
using XGAsset.Runtime.Misc;

namespace XGAsset.Editor.GUI
{
    public static class AnalysisBundleModel
    {
        private static string _selectBundle;

        public static ManifestData Manifest;

        public static string SelectBundle
        {
            get => _selectBundle;
            set
            {
                _selectBundle = value;
                SelectBundleChanged?.Invoke();
            }
        }

        public static BundleInfo SelectBundleInfo => Manifest?.BundleInfos.FirstOrDefault(v => v.Name.Equals(AnalysisBundleModel.SelectBundle));


        public static Action SelectBundleChanged;
    }
}