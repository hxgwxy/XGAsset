using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XGAsset.Editor.Settings;
using XGFramework.XGAsset.Editor.Settings;

namespace XGAsset.Editor.GUI
{
    public class AssetAddressBuildWindow : BaseWindow
    {
        private readonly string[] _tabs = {"Package分组配置", "AssetBundle分析"};

        private int _selectedTab;

        private List<BaseSubWindow> _subWindows;

        private List<BaseSubWindow> SubWindows
        {
            get
            {
                if (_subWindows?.Any(v => !v.IsValid) ?? false)
                {
                    _subWindows.Clear();
                }

                if (_subWindows == null || _subWindows.Count == 0)
                {
                    _subWindows = new List<BaseSubWindow>
                    {
                        new PackageGroupConfigWindow(this),
                        new AnalysisBundleWindow(this),
                    };
                }

                return _subWindows;
            }
        }

        private BaseSubWindow CurrSubWindow => SubWindows[_selectedTab];

        private static AssetAddressBuildWindow _buildWindow;

        private static AssetAddressBuildWindow BuildWindow => _buildWindow ??= GetWindow<AssetAddressBuildWindow>();


        private const int PaddingTop = 30; 
        public override Rect DrawRect => new Rect(0, PaddingTop, position.width, position.height - PaddingTop);

        [MenuItem("XGAsset/Build Window", priority = 110)]
        public static void ShowWindow()
        {
            BuildWindow.minSize = new Vector2(800, 500);
            BuildWindow.ShowUtility();
        }

        private void OnEnable()
        {
            AssetAddressDefaultSettings.OnDataModify -= OnDataModify; 
            AssetAddressDefaultSettings.OnDataModify += OnDataModify;
        }

        private void OnDisable()
        {
            _buildWindow = null;
            AssetAddressDefaultSettings.OnDataModify -= OnDataModify;
        }

        private void OnDataModify()
        {
            CurrSubWindow.Reload();
        }

        public void OnGUI()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return;

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);

            GUILayout.BeginArea(DrawRect);

            CurrSubWindow.Draw();

            GUILayout.EndArea();
        }
    }
}