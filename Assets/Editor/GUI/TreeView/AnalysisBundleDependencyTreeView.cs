using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using XGAsset.Editor.Build;
using XGAsset.Editor.GUI.Base;
using XGAsset.Runtime.Misc;
using XGAsset.Utility;
using XGFramework.XGAsset.Editor.Build;
using XGFramework.XGAsset.Editor.Settings;
using Object = UnityEngine.Object;

namespace XGAsset.Editor.GUI
{
    internal class AnalysisBundleDependencyTreeView : BaseTreeView
    {
        private string[] packRuleNames;

        internal AnalysisBundleDependencyTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            rowHeight = 25;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            useScrollView = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            AnalysisBundleModel.SelectBundleChanged += OnSelectBundleChanged;
            Reload();
        }

        private void OnSelectBundleChanged()
        {
            Reload();
        }

        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            var selectObjects = new List<UnityEngine.Object>();

            if (selectedIds.Count == 1)
            {
                var rows = GetRows();
                foreach (var r in rows)
                {
                    if (r.id == selectedIds[0])
                    {
                        var obj = AssetDatabase.LoadMainAssetAtPath((r as TreeViewItemData<string>)?.Data);
                        if (obj) selectObjects.Add(obj);
                    }
                }
            }

            Selection.objects = selectObjects.ToArray();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItemData<AssetAddressGroupInfo>
            {
                id = 0,
                depth = -1,
                displayName = "Root",
                Data = null,
                children = new List<TreeViewItem>()
            };
            root.children ??= new List<TreeViewItem>();

            var deps = AnalysisBundleModel.SelectBundleInfo?.Dependencies;
            var indirectDeps = AnalysisBundleModel.SelectBundleInfo?.IndirectDependencies;

            if (deps != null && deps.Length > 0)
            {
                foreach (var data in deps)
                {
                    var bundleNode = new TreeViewItemData<string>()
                    {
                        id = data.GetHashCode(), displayName = data, Data = data,
                    };

                    root.AddChild(bundleNode);
                }
            }

            if (indirectDeps != null && indirectDeps.Length > 0)
            {
                var splitStr = "----------------------------------------------------------";
                root.AddChild(new TreeViewItemData<string>()
                {
                    id = splitStr.GetHashCode(), displayName = splitStr, Data = splitStr,
                });
                foreach (var data in indirectDeps)
                {
                    var bundleNode = new TreeViewItemData<string>()
                    {
                        id = data.GetHashCode(), displayName = data, Data = data,
                    };

                    root.AddChild(bundleNode);
                }
            }

            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is TreeViewItemData<string> entryItem)
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    DrawRow(args.GetCellRect(i), entryItem, args.GetColumn(i), ref args);
                }
            }
        }

        private ManifestData _manifest;

        public override void SetData(object data)
        {
            if (data is ManifestData manifestData)
            {
                _manifest = manifestData;
            }

            base.SetData(data);
        }

        void DrawRow(Rect cellRect, TreeViewItemData<string> item, int column, ref RowGUIArgs args)
        {
            switch (column)
            {
                case 0:
                    cellRect.x += GetContentIndent(item);
                    cellRect.width -= cellRect.x;
                    EditorGUI.LabelField(cellRect, $"{item.Data}");
                    break;
            }
        }
    }
}