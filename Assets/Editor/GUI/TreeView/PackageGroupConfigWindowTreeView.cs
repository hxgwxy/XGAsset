using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using XGAsset.Editor.Build;
using XGAsset.Editor.GUI.Base;
using XGAsset.Editor.Settings;
using XGAsset.Utility;
using XGFramework.XGAsset.Editor.Build;
using XGFramework.XGAsset.Editor.Settings;
using PopupWindow = UnityEditor.PopupWindow;

namespace XGAsset.Editor.GUI
{
    internal class GroupPackRuleMaskPopupContent : PopupWindowContent
    {
        private Vector2 scrollViewPos;

        private List<AssetAddressGroupInfo> _selectGroups;

        public GroupPackRuleMaskPopupContent(List<AssetAddressGroupInfo> selectGroups)
        {
            _selectGroups = selectGroups;
        }

        public override void OnGUI(Rect rect)
        {
            var packRuleNames = TypeCache.GetTypesDerivedFrom(typeof(IBundlePackRule)).Select(v => v.Name).ToList();

            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos);
            {
                foreach (var ruleName in packRuleNames)
                {
                    var toggleRect = EditorGUILayout.GetControlRect();
                    var oldState = GetPackRuleCount(ruleName) > 0;
                    var newState = EditorGUI.ToggleLeft(toggleRect, new GUIContent(ruleName), oldState);
                    if (oldState != newState)
                    {
                        ChangePackRule(ruleName);
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private int GetPackRuleCount(string name)
        {
            return _selectGroups.Count(group => group.PackRule.Equals(name));
        }

        private void ChangePackRule(string name)
        {
            foreach (var group in _selectGroups)
            {
                group.PackRule = name;
            }
        }
    }

    internal class GroupLabelMaskPopupContent : PopupWindowContent
    {
        private Vector2 scrollViewPos;

        private List<AssetAddressEntry> _selectEntries;

        public GroupLabelMaskPopupContent(List<AssetAddressEntry> selectEntries)
        {
            _selectEntries = selectEntries;
        }

        public override void OnGUI(Rect rect)
        {
            var labels = AssetAddressDefaultSettings.Setting.Labels;

            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos);
            {
                foreach (var label in labels)
                {
                    var toggleRect = EditorGUILayout.GetControlRect();
                    var oldState = GetEntryUseLabelCount(label) > 0;
                    var newState = EditorGUI.ToggleLeft(toggleRect, new GUIContent(label), oldState);
                    if (oldState != newState)
                    {
                        ChangeEntryLabel(label, newState);
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private int GetEntryUseLabelCount(string label)
        {
            return _selectEntries.Count(entry => entry.HasLabel(label));
        }

        private void ChangeEntryLabel(string label, bool addOrRemove)
        {
            foreach (var entry in _selectEntries)
            {
                if (addOrRemove)
                    entry.Addlabel(label);
                else
                    entry.RemoveLabel(label);
            }
        }
    }

    internal class PackageGroupConfigWindowTreeView : BaseTreeView
    {
        private List<AssetAddressEntry> selectEntries = new List<AssetAddressEntry>();
        private List<AssetAddressGroupInfo> selectGroups = new List<AssetAddressGroupInfo>();
        private string[] m_collectRuleNames;
        private int selectCollectRule;

        private string[] packRuleNames;

        internal PackageGroupConfigWindowTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            Reload();
        }

        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            var index = 0;
            var selectObjects = new List<UnityEngine.Object>();
            selectEntries.Clear();
            selectGroups.Clear();
            foreach (var group in AssetAddressDefaultSettings.CurrPackage.Groups)
            {
                var hash = group.FolderPath.GetHashCode();

                if (selectedIds.Contains(hash))
                {
                    index++;
                    selectObjects.Add(AssetDatabase.LoadMainAssetAtPath(group.FolderPath));
                    selectGroups.Add(group);
                }

                foreach (var entry in group.Entries)
                {
                    hash = entry.AssetPath.GetHashCode();

                    if (selectedIds.Contains(hash))
                    {
                        index++;
                        selectObjects.Add(AssetDatabase.LoadMainAssetAtPath(entry.AssetPath));
                        selectEntries.Add(entry);
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

            foreach (var group in AssetAddressDefaultSettings.CurrPackage.Groups)
            {
                var groupNode = new TreeViewItemData<AssetAddressGroupInfo>
                {
                    id = group.FolderPath.GetHashCode(), displayName = group.GroupName, Data = group,
                };

                foreach (var entry in group.Entries)
                {
                    var entryNode = new TreeViewItemData<AssetAddressEntry>
                    {
                        id = entry.AssetPath.GetHashCode(), displayName = entry.AssetPath, Data = entry,
                    };
                    groupNode.AddChild(entryNode);
                }

                root.AddChild(groupNode);
            }

            root.children ??= new List<TreeViewItem>();

            SetupDepthsFromParentsAndChildren(root);
            // SetupParentsAndChildrenFromDepths(root, root.children);

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is TreeViewItemData<AssetAddressGroupInfo> groupItem)
            {
                for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGroupGUI(args.GetCellRect(i), groupItem, args.GetColumn(i), ref args);
                }
            }

            if (args.item is TreeViewItemData<AssetAddressEntry> entryItem)
            {
                for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellEntryGUI(args.GetCellRect(i), entryItem, args.GetColumn(i), ref args);
                }
            }
        }

        public override void SetData(object data)
        {
            base.SetData(data);
        }

        void CellGroupGUI(Rect cellRect, TreeViewItemData<AssetAddressGroupInfo> item, int column, ref RowGUIArgs args)
        {
            switch (column)
            {
                case 0:
                    // cellRect.x += GetContentIndent(item);
                    // cellRect.width -= cellRect.x;
                    // cellRect.width += EditorStyles.label.CalcSize(new GUIContent(item.Data.GroupName)).y;
                    // EditorGUI.LabelField(cellRect, $"{item.Data.GroupName}");
                    base.RowGUI(args);
                    break;
                case 4:

                    // if (m_collectRuleNames == null)
                    // {
                    //     var objs = new List<string>();
                    //     var list = TypeCache.GetTypesDerivedFrom(typeof(ICollectRule)).ToList();
                    //     foreach (var type in list)
                    //     {
                    //         objs.Add(type.Name);
                    //     }
                    //
                    //     m_collectRuleNames = objs.ToArray();
                    // }
                    // selectCollectRule = EditorGUI.Popup(cellRect, selectCollectRule, m_collectRuleNames);

                    break;
                case 5: // pack rule
                    try
                    {
                        if (EditorGUI.DropdownButton(cellRect, new GUIContent(item.Data.PackRule), FocusType.Passive))
                        {
                            if (selectGroups.Count <= 1)
                                SelectionClick(item, false);

                            PopupWindow.Show(cellRect, new GroupPackRuleMaskPopupContent(selectGroups));
                        }
                    }
                    catch (UnityEngine.ExitGUIException)
                    {
                        GUIUtility.ExitGUI();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }

                    break;
                case 6:
                    cellRect.x += cellRect.width / 2 - GetContentIndent(item) / 2;

                    EditorGUI.BeginChangeCheck();

                    var copy = EditorGUI.Toggle(cellRect, "", item.Data.CopyToStreamingAssets);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (selectGroups.Count <= 1)
                            SelectionClick(item, false);

                        foreach (var groupInfo in selectGroups)
                        {
                            groupInfo.CopyToStreamingAssets = copy;
                        }
                    }

                    break;
                case 7:
                    cellRect.x += cellRect.width / 2 - GetContentIndent(item) / 2;

                    EditorGUI.BeginChangeCheck();

                    var active = EditorGUI.Toggle(cellRect, "", item.Data.Active);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (selectGroups.Count <= 1)
                            SelectionClick(item, false);

                        foreach (var groupInfo in selectGroups)
                        {
                            groupInfo.Active = active;
                        }
                    }

                    break;
                default:
                    break;
            }
        }

        void CellEntryGUI(Rect cellRect, TreeViewItemData<AssetAddressEntry> item, int column, ref RowGUIArgs args)
        {
            switch (column)
            {
                case 0:
                    cellRect.x += GetContentIndent(item);
                    cellRect.width -= cellRect.x;
                    EditorGUI.LabelField(cellRect, $"{item.Data.Address}");
                    break;
                case 1:
                    EditorGUI.LabelField(cellRect, $"{item.Data.AssetPath}");
                    break;
                case 2:
                    var icon = AssetDatabase.GetCachedIcon(item.Data.AssetPath) as Texture2D;
                    UnityEngine.GUI.DrawTexture(cellRect, icon, ScaleMode.ScaleToFit, true);
                    break;
                case 3: // label
                    try
                    {
                        if (EditorGUI.DropdownButton(cellRect, new GUIContent(string.Join(", ", item.Data.Labels)), FocusType.Passive))
                        {
                            if (selectEntries.Count <= 1)
                            {
                                SelectionClick(item, false);
                            }

                            PopupWindow.Show(cellRect, new GroupLabelMaskPopupContent(selectEntries));
                        }
                    }
                    catch (UnityEngine.ExitGUIException)
                    {
                        GUIUtility.ExitGUI();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }

                    break;
                case 7:
                    cellRect.x += cellRect.width / 2 - GetContentIndent(item) / 2;

                    EditorGUI.BeginChangeCheck();

                    var active = EditorGUI.Toggle(cellRect, "", item.Data.Active);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (selectEntries.Count <= 1)
                            SelectionClick(item, false);

                        foreach (var entry in selectEntries)
                        {
                            entry.Active = active;
                        }
                    }

                    break;
            }
        }
    }
}