using System.IO;
using UnityEditor;
using UnityEngine;
using XGAsset.Editor.GUI.Base;
using XGAsset.Runtime.Misc;
using XGFramework.LitJson;

namespace XGAsset.Editor.GUI
{
    public class AnalysisBundleWindow : BaseSubWindow
    {
        private float _horizontalSplitPercent = 0.38f;

        private float _verticalSplitPercent = 0.38f;

        private bool _isResizingHorizontal;

        private bool _isResizingVertical;

        private float PaddingBottom = 30;

        #region 左侧树视图

        private BaseTreeView _treeViewLeft;

        private Rect TreeViewRectLeft => new Rect(ParentRect.x,
            ParentRect.y,
            ParentRect.width * _horizontalSplitPercent,
            ParentRect.height - PaddingBottom);

        #endregion

        #region 右侧树视图

        private BaseTreeView _treeViewRight;


        private Rect TreeViewRectRight => new Rect(ParentRect.x + ParentRect.width * _horizontalSplitPercent,
            ParentRect.y,
            ParentRect.width * (1 - _horizontalSplitPercent),
            ParentRect.height - PaddingBottom - 600);

        #endregion

        private BaseTreeView _treeViewIncludes;

        private Rect TreeViewIncludesRect => new Rect(ParentRect.x + ParentRect.width * _horizontalSplitPercent,
            TreeViewRectRight.height + PaddingBottom,
            ParentRect.width * (1 - _horizontalSplitPercent),
            ParentRect.height - PaddingBottom - TreeViewRectRight.height);

        #region 绘制相关

        public AnalysisBundleWindow(BaseWindow parentWindow) : base(parentWindow)
        {
            LoadManifest();
        }

        public override void Initialize()
        {
            _treeViewLeft ??= TreeViewCreator.Create<AnalysisBundleBundleTreeView>();
            _treeViewRight ??= TreeViewCreator.Create<AnalysisBundleDependencyTreeView>();
            _treeViewIncludes ??= TreeViewCreator.Create<AnalysisBundleIncludesTreeView>();
        }

        public override void Draw()
        {
            Initialize();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("加载Manifest"))
            {
                LoadManifest();
            }

            if (GUILayout.Button("刷新"))
            {
                Reload();
            }

            EditorGUILayout.EndHorizontal();

            _treeViewLeft?.OnGUI(TreeViewRectLeft);
            _treeViewRight?.OnGUI(TreeViewRectRight);
            _treeViewIncludes?.OnGUI(TreeViewIncludesRect);

            SplitHorizontal();
            if (_isResizingHorizontal)
            {
                ParentWindow.Repaint();
            }
        }

        /// <summary>
        /// 水平分割的矩形
        /// </summary>
        private Rect HorizontalSplitRect;

        private void SplitHorizontal()
        {
            HorizontalSplitRect.Set(ParentRect.width * _horizontalSplitPercent, ParentRect.y, 20, ParentRect.height);
            EditorGUIUtility.AddCursorRect(HorizontalSplitRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && HorizontalSplitRect.Contains(Event.current.mousePosition))
            {
                _isResizingHorizontal = true;
            }

            if (_isResizingHorizontal)
            {
                _horizontalSplitPercent = Mathf.Clamp(Event.current.mousePosition.x / ParentWindow.DrawRect.width, 0.1f, 0.9f);
            }

            if (Event.current.type == EventType.MouseUp)
            {
                _isResizingHorizontal = false;
            }
        }

        public override void Reload()
        {
            _treeViewLeft?.Reload();
            _treeViewRight?.Reload();
            _treeViewIncludes?.Reload();
        }

        #endregion

        #region 逻辑相关

        private void LoadManifest()
        {
            // var path = EditorUtility.OpenFilePanel("LoadManifest", Application.dataPath, "json");
            var path = "Assets/StreamingAssets/XGAssets/DefaultPackage/Manifest_DefaultPackage_1.0.0.json";
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var manifest = JsonMapper.ToObject<ManifestData>(File.ReadAllText(path));
                AnalysisBundleModel.Manifest = manifest;
                _treeViewLeft?.SetData(manifest);
                _treeViewRight?.SetData(manifest);
                Reload();
            }
        }

        #endregion
    }
}