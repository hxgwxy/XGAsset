using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using XGAsset.Editor.GUI.Base;

namespace XGAsset.Editor.GUI
{
    public class AnalysisBundleIncludesTreeView : BaseTreeView
    {
        public AnalysisBundleIncludesTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            useScrollView = true;
            AnalysisBundleModel.SelectBundleChanged += Reload;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItemData<string>
            {
                id = 0,
                depth = -1,
                displayName = "Root",
                Data = null,
                children = new List<TreeViewItem>()
            };
            root.children ??= new List<TreeViewItem>();

            var includes = AnalysisBundleModel.SelectBundleInfo?.IncludeAssets;
            var references = AnalysisBundleModel.SelectBundleInfo?.ReferenceAssets;

            if (includes != null && includes.Length > 0)
            {
                foreach (var data in includes)
                {
                    var bundleNode = new TreeViewItemData<string>()
                    {
                        id = data.GetHashCode(), displayName = data, Data = data,
                    };

                    root.AddChild(bundleNode);
                }
            }
            
            if (references != null && references.Length > 0)
            {
                var splitStr = "----------------------------------------------------------";
                root.AddChild(new TreeViewItemData<string>()
                {
                    id = splitStr.GetHashCode(), displayName = splitStr, Data = splitStr,
                });
                foreach (var data in references)
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