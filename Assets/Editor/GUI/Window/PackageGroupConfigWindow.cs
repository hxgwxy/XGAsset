using System.Linq;
using UnityEditor;
using UnityEngine;
using XGAsset.Editor.GUI.Base;
using XGAsset.Editor.Settings;
using XGFramework.XGAsset.Editor.Settings;

namespace XGAsset.Editor.GUI
{
    public class PackageGroupConfigWindow : BaseSubWindow
    {
        private BaseTreeView _treeView;

        private const int limit = 110;

        private Rect treeViewRect => new Rect(0, limit, ParentRect.width, ParentRect.height - limit);

        private bool doReset = true;

        private string[] AllPackageName;

        private int selectPackageIndex;

        internal PackageGroupConfigWindow(BaseWindow parentWindow) : base(parentWindow)
        {
        }

        public override void Initialize()
        {
            _treeView ??= TreeViewCreator.Create<PackageGroupConfigWindowTreeView>();

            if (doReset)
            {
                doReset = false;
                AllPackageName = AssetAddressDefaultSettings.AllPackages.Select(v => v.PackageName).ToArray();
                selectPackageIndex = AllPackageName.ToList().IndexOf(AssetAddressDefaultSettings.CurrPackage.PackageName);
            }
        }

        public override void Draw()
        {
            Initialize();

            if (!ParentWindow)
                return;

            DrawAddPackage();

            EditorGUILayout.Separator();

            DrawSelectPackage();

            EditorGUILayout.Separator();

            DrawPath();

            _treeView.OnGUI(treeViewRect);
        }

        public override void Reload()
        {
            _treeView?.Reload();
        }

        private void DrawAddPackage()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("构建"))
                {
                    AssetAddressDefaultSettings.Build();
                }

                if (GUILayout.Button("刷新"))
                {
                    AssetAddressDefaultSettings.CurrPackage.Groups.Sort((a, b) => string.CompareOrdinal(a.FolderPath, b.FolderPath));
                    _treeView.Reload();
                }

                if (GUILayout.Button("修复"))
                {
                    AssetAddressDefaultSettings.Fix();

                    _treeView.Reload();
                }

                if (GUILayout.Button("保存"))
                {
                    AssetAddressDefaultSettings.Save();
                }

                if (GUILayout.Button("打开目录"))
                {
                    System.Diagnostics.Process.Start("explorer.exe", AssetAddressDefaultSettings.CurrPackage.BuildPath);
                }
            }
        }

        private void DrawPath()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var buildFolder = EditorGUILayout.TextField("构建输出目录:", AssetAddressDefaultSettings.CurrPackage.BuildPath);
                if (!buildFolder.Equals(AssetAddressDefaultSettings.CurrPackage.BuildPath))
                {
                    AssetAddressDefaultSettings.CurrPackage.BuildPath = buildFolder;
                }
            }

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                var loadFolder = EditorGUILayout.TextField("远程加载目录:", AssetAddressDefaultSettings.CurrPackage.LoadPath);
                if (!loadFolder.Equals(AssetAddressDefaultSettings.CurrPackage.LoadPath))
                {
                    AssetAddressDefaultSettings.CurrPackage.LoadPath = loadFolder;
                }
            }
        }

        private void DrawSelectPackage()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("资源包:", GUILayout.MaxWidth(50));


                EditorGUI.BeginChangeCheck();
                {
                    selectPackageIndex = EditorGUILayout.Popup(selectPackageIndex, AllPackageName, GUILayout.MaxWidth(200));
                    if (EditorGUI.EndChangeCheck())
                    {
                        AssetAddressDefaultSettings.SetDefaultPackage(AllPackageName[selectPackageIndex]);
                        _treeView.Reload();
                    }
                }
            }
        }
    }
}