using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using XGAsset.Runtime.Implement;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace XGAsset.Runtime
{
    public enum PlayMode
    {
        Editor,
        Simulate,
        RealEnv,
    }

    public interface IEditorRunTimeInitialize
    {
        public PlayMode RunTimePlayMode { get; }
        public AssetImplBase AssetImplEditor { get; }
    }

    public static class AssetLoader
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void Onload()
        {
            EditorApplication.playModeStateChanged += OnPlayerModeStateChanged;
        }

        private static void OnPlayerModeStateChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.ExitingPlayMode || playModeState == PlayModeStateChange.EnteredPlayMode)
            {
                _assetImpl = null;
            }
        }
#endif

        private static PlayMode _playMode = PlayMode.RealEnv;

        private static AssetImplBase _assetImpl;

        private static AssetImplBase AssetImpl
        {
            get
            {
                if (_assetImpl != null)
                    return _assetImpl;

#if UNITY_EDITOR
                _playMode = EditorRunTimeInitialize.RunTimePlayMode;
                if (!Application.isPlaying)
                {
                    _playMode = PlayMode.Editor;
                }
#else
                _playMode = PlayMode.RealEnv;
#endif

                switch (_playMode)
                {
                    case PlayMode.Editor:
                        _assetImpl = EditorRunTimeInitialize.AssetImplEditor;
                        break;
                    case PlayMode.Simulate:
                        _assetImpl = new AssetImplSimulationEditor();
                        break;
                    case PlayMode.RealEnv:
                        _assetImpl = new AssetImplRunTime();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return _assetImpl;
            }
        }

        public static IEditorRunTimeInitialize EditorRunTimeInitialize;

        public static PlayMode Mode => _playMode;

        public static AssetOperationHandle Initialize()
        {
            return AssetImpl.Initialize();
        }

        public static bool HasAsset(string address)
        {
            return AssetImpl.HasAsset(address);
        }

        public static void AddDownloadHost(string url)
        {
            ResourcesManager.HostServices.AddDownloadHost(url);
        }

        public static void SetDownloadHost(string url)
        {
            ResourcesManager.HostServices.SetDownloadHost(url);
        }

        public static AssetOperationHandle AddPackage(string packageName, string version, bool ignoreCache = false)
        {
            return AssetImpl.AddPackage(packageName, version, ignoreCache);
        }

        public static AssetOperationHandle DownloadDependenciesAsync(string address)
        {
            return AssetImpl.LoadAsset(address);
        }

        public static AssetOperationHandle DownloadDependenciesAsync(List<string> address)
        {
            return AssetImpl.LoadAssets(address);
        }

        public static AssetOperationHandle LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            return AssetImpl.LoadScene(sceneName, mode);
        }

        public static AssetOperationHandle LoadAssetAsync(string address)
        {
            return AssetImpl.LoadAsset(address);
        }

        public static AssetOperationHandle LoadAssetsAsync(IList<string> address)
        {
            return AssetImpl.LoadAssets(address);
        }

        public static AssetOperationHandle LoadAssetsAsync(params string[] address)
        {
            return AssetImpl.LoadAssets(address.ToList());
        }

        public static void Unload()
        {
            AssetImpl.Unload();
        }
    }
}