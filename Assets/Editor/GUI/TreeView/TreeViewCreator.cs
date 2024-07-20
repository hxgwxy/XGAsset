using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using XGAsset.Editor.GUI.Base;

namespace XGAsset.Editor.GUI
{
    public static class TreeViewCreator
    {
        private static Dictionary<Type, Func<BaseTreeView>> dict = new Dictionary<Type, Func<BaseTreeView>>()
        {
            { typeof(PackageGroupConfigWindowTreeView), PackageGroupConfigWindowTreeView },
            { typeof(AnalysisBundleBundleTreeView), AnalysisBundleBundleTreeView },
            { typeof(AnalysisBundleDependencyTreeView), AnalysisBundleDependencyTreeView },
            { typeof(AnalysisBundleIncludesTreeView), AnalysisBundleIncludesTreeView },
        };

        public static T Create<T>() where T : BaseTreeView
        {
            return dict[typeof(T)].Invoke() as T;
        }

        private static BaseTreeView PackageGroupConfigWindowTreeView()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Group Name/Address Name"),
                    contextMenuText = "Asset",
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 240,
                    minWidth = 80,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Asset Path"),
                    contextMenuText = "Type",
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 300,
                    minWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByType"), "Asset Type"),
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 30,
                    minWidth = 30,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Labels", "标签"),
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 160,
                    minWidth = 60,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Group/Address Rule", "分组/地址命名规则"),
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 150,
                    minWidth = 60,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Build Rule", "打包规则"),
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 150,
                    minWidth = 60,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Copy To StreamingAssets", ""),
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 160,
                    minWidth = 60,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Active", ""),
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 60,
                    minWidth = 30,
                    autoResize = true
                },
            };

            var state = new MultiColumnHeaderState(columns);
            var multiColumnHeader = new MultiColumnHeader(state);
            return new PackageGroupConfigWindowTreeView(new TreeViewState(), multiColumnHeader);
        }

        private static BaseTreeView AnalysisBundleBundleTreeView()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("AssetBundle"),
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    // sortedAscending = true,
                    width = 300,
                    minWidth = 300,
                    // autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Direct Deps", "直接依赖AssetBundle数量"),
                    headerTextAlignment = TextAlignment.Center,
                    autoResize = false,
                    width = 90,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Indirect Deps", "间接依赖AssetBundle数量"),
                    headerTextAlignment = TextAlignment.Center,
                    autoResize = false,
                    width = 90,
                },
            };
            var treeView = new AnalysisBundleBundleTreeView(new TreeViewState(), new MultiColumnHeader(new MultiColumnHeaderState(columns)));
            return treeView;
        }

        private static BaseTreeView AnalysisBundleDependencyTreeView()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Dependency BundleName"),
                    contextMenuText = "Asset",
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 500,
                    minWidth = 500,
                    autoResize = true,
                    allowToggleVisibility = true
                },
            };

            return new AnalysisBundleDependencyTreeView(new TreeViewState(), new MultiColumnHeader(new MultiColumnHeaderState(columns))
            {
                height = 30,
            });
        }

        private static BaseTreeView AnalysisBundleIncludesTreeView()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Asset"),
                    contextMenuText = "Asset",
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    sortedAscending = true,
                    width = 500,
                    minWidth = 500,
                    autoResize = true,
                    allowToggleVisibility = true
                },
            };

            return new AnalysisBundleIncludesTreeView(new TreeViewState(), new MultiColumnHeader(new MultiColumnHeaderState(columns)));
        }
    }
}