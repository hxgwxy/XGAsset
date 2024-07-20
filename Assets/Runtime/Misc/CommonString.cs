using UnityEngine;

namespace XGAsset.Runtime.Misc
{
    public class CommonString
    {
        public static string RuntimeSettingsPath => $"Assets/Resources/{TargetString}/RuntimeSettings.asset";
        public static string RuntimeSettingsLoadPath => $"{TargetString}/RuntimeSettings";
        public static string TargetString => "XGAssets";
        public static string StreamingAssets => $"{Application.streamingAssetsPath}/{TargetString}/";

        public static string Platform
        {
            get
            {
#if UNITY_ANDROID
                return "Android";
#elif UNITY_IOS
                return "iOS";
#elif UNITY_WEBGL
                return "WebGL";
#else
                return "StandaloneWindows";
#endif
            }
        }

        public static string BuildOutput => "BuildOutput/";
    }
}