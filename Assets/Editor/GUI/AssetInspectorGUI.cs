using System.Linq;
using UnityEditor;
using UnityEngine;
using XGAsset.Editor.Settings;
using XGFramework.XGAsset.Editor.Other;
using XGFramework.XGAsset.Editor.Settings;
using Object = UnityEngine.Object;

namespace XGAsset.Editor.GUI
{
    [InitializeOnLoad]
    public class AssetInspectorGUI
    {
        private static GUIStyle s_ToggleNormalStyle;
        private static GUIStyle s_ToggleBoldStyle;
        private static GUIStyle s_ToggleMixedStyle;

        private static GUIContent s_AddressToggleText;
        private static GUIContent s_GroupToggleText;

        static AssetInspectorGUI()
        {
            s_ToggleMixedStyle = null;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;

            s_GroupToggleText = new GUIContent("分组", "");
            s_AddressToggleText = new GUIContent("地址", "");
        }

        private static void OnPostHeaderGUI(UnityEditor.Editor obj)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            foreach (var target in obj.targets)
            {
                if (!AssetUtilityEditor.TryGetPathAndGUIDFromTarget(target, out var path, out var guid))
                    return;
            }

            s_ToggleNormalStyle ??= new GUIStyle("Toggle");
            s_ToggleBoldStyle ??= new GUIStyle("BoldToggle");
            s_ToggleMixedStyle ??= new GUIStyle("ToggleMixed");

            HandleFolder(obj.targets);
            HandleAssets(obj.targets);
        }


        private static void HandleAssets(Object[] targets)
        {
            if (targets[0] is DefaultAsset)
            {
                return;
            }

            var hasCount = targets.Select(AssetDatabase.GetAssetPath).Count(AssetAddressDefaultSettings.HasAssetInfo);
            var isMixed = hasCount > 0 && hasCount < targets.Length;
            var hasAddress = hasCount > 0 && !isMixed;

            UnityEngine.GUI.enabled = true;
            foreach (var target in targets)
            {
                var isMainAsset = target is AssetImporter || AssetDatabase.IsMainAsset(target);
                if (!isMainAsset)
                {
                    UnityEngine.GUI.enabled = false; // 在BeginHorizontal之前禁用
                }
            }

            GUILayout.BeginHorizontal();

            var isModify = false;
            var style = s_ToggleNormalStyle;
            if (isMixed) style = s_ToggleMixedStyle;

            var oldToggle = hasAddress;
            var isToggle = GUILayout.Toggle(oldToggle, s_AddressToggleText, style, GUILayout.ExpandWidth(false));

            if (isToggle != oldToggle)
            {
                foreach (var target in targets)
                {
                    var path = AssetDatabase.GetAssetPath(target);
                    if (isToggle)
                        AssetAddressDefaultSettings.AddAssetInfo(path);
                    else
                        AssetAddressDefaultSettings.RemoveEntry(path);
                }

                isModify = true;
            }

            var showTextField = !isMixed && isToggle && targets.Length == 1;
            var assetPath = AssetDatabase.GetAssetPath(targets[0]);
            var oldAddress = assetPath;
            if (showTextField)
            {
                if (AssetAddressDefaultSettings.HasAssetInfo(assetPath))
                {
                    var assetInfo = AssetAddressDefaultSettings.GetAssetInfo(assetPath);
                    var packageName = AssetAddressDefaultSettings.GetGroup(assetInfo.GroupName).PackageName;

                    EditorGUILayout.LabelField(packageName, GUILayout.MaxWidth(EditorStyles.label.CalcSize(new GUIContent(packageName)).x + 2));

                    var address = assetInfo.Address;
                    var newAddress = EditorGUILayout.DelayedTextField(address, GUILayout.ExpandWidth(true));
                    if (!newAddress.Equals(address))
                    {
                        AssetAddressDefaultSettings.ChangeAssetAddress(address, newAddress);
                        isModify = true;
                    }
                }
            }


            if (isToggle)
            {
                if (GUILayout.Button("简化地址"))
                {
                    foreach (var target in targets)
                    {
                        AssetAddressDefaultSettings.SetAssetAddress(target);
                    }
                }
            }


            GUILayout.EndHorizontal();

            UnityEngine.GUI.enabled = true;

            if (isModify)
            {
                AssetAddressDefaultSettings.DispatchModifyMsg();
                AssetAddressDefaultSettings.SavePackage();
            }
        }


        private static void HandleFolder(Object[] targets)
        {
            if (!(targets[0] is DefaultAsset))
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();

                var groupCount = CalcSelectGroupsCount(targets);
                bool isToggle;
                if (groupCount > 0 && groupCount != targets.Length)
                {
                    isToggle = ShowMixedGroup();
                }
                else
                {
                    isToggle = ShowNormalGroup(groupCount, targets[0]);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var obj in targets)
                    {
                        var target = (DefaultAsset)obj;

                        var folder = AssetDatabase.GetAssetPath(target);

                        if (isToggle)
                        {
                            AssetAddressDefaultSettings.AddGroup(folder);
                        }
                        else
                        {
                            AssetAddressDefaultSettings.RemoveGroup(folder);
                        }
                    }

                    AssetAddressDefaultSettings.DispatchModifyMsg();
                    AssetAddressDefaultSettings.Save();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 选中的组数量
        /// </summary>
        /// <param name="targets"></param>
        /// <returns></returns>
        private static int CalcSelectGroupsCount(Object[] targets)
        {
            var groupCount = 0;
            foreach (var obj in targets)
            {
                var target = (DefaultAsset)obj;

                var folder = AssetDatabase.GetAssetPath(target);

                var group = AssetAddressDefaultSettings.GetGroup(folder);

                if (group != null)
                    groupCount++;
            }

            return groupCount;
        }

        private static bool ShowNormalGroup(int groupCount, Object target)
        {
            var hasGroup = groupCount > 0;
            var toggle = GUILayout.Toggle(hasGroup, s_GroupToggleText, s_ToggleNormalStyle, GUILayout.ExpandWidth(false));
            if (toggle)
            {
                var group = AssetAddressDefaultSettings.GetGroup(AssetDatabase.GetAssetPath(target));
                if (group != null)
                {
                    EditorGUILayout.LabelField($"{group.PackageName}: {group.GroupName}", GUILayout.ExpandWidth(true));
                }
            }

            return toggle;
        }

        private static bool ShowMixedGroup()
        {
            var toggle = GUILayout.Toggle(false, s_GroupToggleText, s_ToggleMixedStyle, GUILayout.ExpandWidth(false));
            return toggle;
        }
    }
}