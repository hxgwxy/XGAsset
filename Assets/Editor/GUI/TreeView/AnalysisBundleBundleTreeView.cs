using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using XGAsset.Editor.Build;
using XGAsset.Editor.GUI.Base;
using XGAsset.Runtime.Misc;
using XGAsset.Utility;
using XGAsset.Editor.Build;
using XGAsset.Editor.Settings;

namespace XGAsset.Editor.GUI
{
    internal class AnalysisBundleBundleTreeView : BaseTreeView
    {
        private string[] packRuleNames;

        internal AnalysisBundleBundleTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            rowHeight = 30;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            useScrollView = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            Reload();
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

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 1)
            {
                var rows = GetRows();
                foreach (var r in rows)
                {
                    if (r.id == selectedIds[0])
                    {
                        AnalysisBundleModel.SelectBundle = (r as TreeViewItemData<BundleInfo>)?.Data.Name;
                    }
                }
            }
        }

        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
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


            if (_manifest != null)
            {
                foreach (var data in _manifest.BundleInfos)
                {
                    var bundleNode = new TreeViewItemData<BundleInfo>()
                    {
                        id = data.Name.GetHashCode(), displayName = data.Name, Data = data,
                    };

                    root.AddChild(bundleNode);
                }
            }

            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is TreeViewItemData<BundleInfo> bundleItem)
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    ShowBundleInfo(args.GetCellRect(i), bundleItem, args.GetColumn(i), ref args);
                }
            }
        }

        private void ShowBundleInfo(Rect cellRect, TreeViewItemData<BundleInfo> item, int column, ref RowGUIArgs args)
        {
            cellRect.x += GetContentIndent(item);
            switch (column)
            {
                case 0:
                    EditorGUI.LabelField(cellRect, item.Data.Name);
                    break;
                case 1:
                    EditorGUI.LabelField(cellRect, $"{GetBundleInfo(item.Data.Name)?.Dependencies.Length ?? 0}");
                    break;
                case 2:
                    EditorGUI.LabelField(cellRect, $"{GetBundleInfo(item.Data.Name)?.IndirectDependencies?.Length ?? 0}");
                    break;
            }
        }

        private BundleInfo GetBundleInfo(string name)
        {
            return AnalysisBundleModel.Manifest?.BundleInfos.FirstOrDefault(v => v.Name.Equals(name));
        }
    }
}