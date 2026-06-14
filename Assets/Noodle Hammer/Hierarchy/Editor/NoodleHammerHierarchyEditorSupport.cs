#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Hierarchy.Editor
{
    internal static class ImprovedEditorTheme
    {
        public static readonly Color Accent = new Color(0.22f, 0.55f, 0.95f, 1f);
        public static readonly Color HierarchyGuide = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.18f)
            : new Color(0f, 0f, 0f, 0.15f);

        public static void DrawInspectorHeader(string title, string description, bool enabled)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 48f);
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(0.17f, 0.17f, 0.17f, 1f)
                : new Color(0.94f, 0.94f, 0.94f, 1f));
            DrawOutline(rect, EditorGUIUtility.isProSkin
                ? new Color(0f, 0f, 0f, 0.48f)
                : new Color(0f, 0f, 0f, 0.16f));

            Rect titleRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 18f);
            Rect descriptionRect = new Rect(rect.x + 10f, rect.y + 24f, rect.width - 20f, 18f);

            EditorGUI.LabelField(titleRect, title, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!enabled))
                EditorGUI.LabelField(descriptionRect, description, EditorStyles.miniLabel);
        }

        public static void DrawToggleHeader(SerializedProperty property)
        {
            if (property == null)
                return;

            EditorGUILayout.PropertyField(property, new GUIContent("Enabled"));
            EditorGUILayout.Space(2f);
        }

        public static bool DrawSectionHeader(bool expanded, string title, string subtitle, bool enabled)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 38f);
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(0.15f, 0.15f, 0.15f, 1f)
                : new Color(0.96f, 0.96f, 0.96f, 1f));
            DrawOutline(rect, EditorGUIUtility.isProSkin
                ? new Color(0f, 0f, 0f, 0.45f)
                : new Color(0f, 0f, 0f, 0.14f));

            Rect foldoutRect = new Rect(rect.x + 8f, rect.y + 5f, rect.width - 16f, 18f);
            Rect subtitleRect = new Rect(rect.x + 24f, rect.y + 19f, rect.width - 28f, 14f);

            using (new EditorGUI.DisabledScope(!enabled))
            {
                expanded = EditorGUI.Foldout(foldoutRect, expanded, title, true, EditorStyles.foldoutHeader);
                EditorGUI.LabelField(subtitleRect, subtitle, EditorStyles.miniLabel);
            }

            return expanded;
        }

        public static void BeginSectionBody(bool padTop)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (padTop)
                EditorGUILayout.Space(2f);
        }

        public static void EndSectionBody()
        {
            EditorGUILayout.EndVertical();
        }

        public static float GetStyledToggleHeight(float width, GUIContent content, float toggleWidth)
        {
            float labelHeight = EditorStyles.label.CalcHeight(content, Mathf.Max(40f, width - toggleWidth - 10f));
            return Mathf.Max(18f, labelHeight);
        }

        public static void DrawAlternatingRowBackground(Rect rect, int rowIndex, float horizontalExpand, float verticalExpand)
        {
            Rect backgroundRect = new Rect(
                rect.x - horizontalExpand,
                rect.y - verticalExpand,
                rect.width + horizontalExpand * 2f,
                rect.height + verticalExpand * 2f);
            Color fill = (rowIndex & 1) == 0
                ? (EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.02f) : new Color(0f, 0f, 0f, 0.025f))
                : (EditorGUIUtility.isProSkin ? new Color(0f, 0f, 0f, 0.08f) : new Color(0f, 0f, 0f, 0.045f));
            EditorGUI.DrawRect(backgroundRect, fill);
        }

        public static void DrawHierarchyGuide(Rect rect, float guideX)
        {
            EditorGUI.DrawRect(new Rect(guideX, rect.y + 2f, 1f, Mathf.Max(0f, rect.height - 4f)), HierarchyGuide);
            EditorGUI.DrawRect(new Rect(guideX, rect.center.y, 12f, 1f), HierarchyGuide);
        }

        public static void DrawInlineToggle(Rect rect, SerializedProperty property)
        {
            if (property == null)
                return;

            EditorGUI.BeginChangeCheck();
            bool newValue = GUI.Toggle(rect, property.boolValue, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
                property.boolValue = newValue;
        }

        public static void DrawOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }
    }

    internal static class ScriptRelativeAssetUtility
    {
        public static void EnsureFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            string[] parts = assetPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                return;

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static string FindFirstAssetPathOfType<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" });
            if (guids == null || guids.Length == 0)
                return null;

            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        public static string GetScriptDirectory(string scriptFileName, string fallbackPath)
        {
            return fallbackPath;
        }

        public static string CombineAssetPath(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
                return right ?? string.Empty;

            if (string.IsNullOrEmpty(right))
                return left;

            return left.TrimEnd('/') + "/" + right.TrimStart('/');
        }
    }
}
#endif
