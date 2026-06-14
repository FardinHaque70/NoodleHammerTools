#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NoodleHammer.Animator.Editor
{
    internal static class ImprovedEditorTheme
    {
        public static readonly Color Surface = EditorGUIUtility.isProSkin
            ? new Color(0.165f, 0.165f, 0.165f, 1f)
            : new Color(0.93f, 0.93f, 0.93f, 1f);

        public static readonly Color BorderStrong = EditorGUIUtility.isProSkin
            ? new Color(0f, 0f, 0f, 0.48f)
            : new Color(0f, 0f, 0f, 0.16f);

        public static readonly Color Accent = new Color(0.22f, 0.55f, 0.95f, 1f);
        public static readonly Color AccentBright = new Color(0.78f, 0.88f, 1f, 1f);
        public static readonly Color Success = new Color(0.23f, 0.66f, 0.34f, 1f);
        public static readonly Color Warning = new Color(0.91f, 0.62f, 0.20f, 1f);
        public static readonly Color Error = new Color(0.80f, 0.29f, 0.24f, 1f);
        public static readonly Color Text = EditorGUIUtility.isProSkin
            ? new Color(0.92f, 0.92f, 0.92f, 1f)
            : new Color(0.16f, 0.16f, 0.16f, 1f);
        public static readonly Color RowSurfaceA = EditorGUIUtility.isProSkin
            ? new Color(0.20f, 0.20f, 0.20f, 1f)
            : new Color(0.965f, 0.965f, 0.965f, 1f);
        public static readonly Color RowSurfaceB = EditorGUIUtility.isProSkin
            ? new Color(0.175f, 0.175f, 0.175f, 1f)
            : new Color(0.945f, 0.945f, 0.945f, 1f);
        public static readonly Color HierarchyGuide = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.16f)
            : new Color(0f, 0f, 0f, 0.14f);

        private static Texture2D s_sectionBodyBackground;

        public static void DrawOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        public static bool DrawSectionHeader(bool expanded, string title, string subtitle, bool enabled)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 40f);
            Color fill = enabled
                ? (EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.90f, 0.90f, 0.90f, 1f))
                : (EditorGUIUtility.isProSkin ? new Color(0.14f, 0.14f, 0.14f, 1f) : new Color(0.94f, 0.94f, 0.94f, 1f));

            EditorGUI.DrawRect(rect, fill);
            DrawOutline(rect, BorderStrong);

            Rect foldoutRect = new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 18f);
            Rect subtitleRect = new Rect(rect.x + 24f, rect.y + 20f, rect.width - 30f, 16f);

            expanded = EditorGUI.Foldout(foldoutRect, expanded, title, true, EditorStyles.boldLabel);
            EditorGUI.LabelField(subtitleRect, subtitle, EditorStyles.miniLabel);
            return expanded;
        }

        public static void DrawAlternatingRowBackground(Rect rect, int rowIndex, float horizontalExpand, float verticalExpand)
        {
            Rect backgroundRect = new Rect(
                rect.x - horizontalExpand,
                rect.y - verticalExpand,
                rect.width + horizontalExpand * 2f,
                rect.height + verticalExpand * 2f);
            EditorGUI.DrawRect(backgroundRect, (rowIndex & 1) == 0 ? RowSurfaceA : RowSurfaceB);
        }

        public static void DrawHierarchyGuide(Rect rect, float guideX)
        {
            EditorGUI.DrawRect(new Rect(guideX, rect.y + 2f, 1f, Mathf.Max(0f, rect.height - 4f)), HierarchyGuide);
            EditorGUI.DrawRect(new Rect(guideX, rect.center.y, 12f, 1f), HierarchyGuide);
        }

        public static float GetStyledSliderHeight(float width, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + 8f;
        }

        public static float DrawStyledSlider(Rect rect, GUIContent label, float value, float min, float max, int decimals)
        {
            Rect labelRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            Rect sliderRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 6f, rect.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(labelRect, label);
            float newValue = GUI.HorizontalSlider(sliderRect, value, min, max);
            return (float)System.Math.Round(newValue, decimals);
        }

        public static Color GetActionFill(Color accent, bool hovered, bool pressed, bool enabled)
        {
            if (!enabled)
                return new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, 0.55f);

            if (pressed)
                return Color.Lerp(accent, Color.black, 0.28f);

            if (hovered)
                return Color.Lerp(accent, Color.white, 0.12f);

            return accent;
        }

        public static Color GetActionBorder(Color accent, bool enabled)
        {
            return enabled ? Color.Lerp(accent, Color.black, 0.45f) : new Color(0f, 0f, 0f, 0.20f);
        }

        public static Color GetActionTopHighlight(bool hovered, bool pressed, bool enabled)
        {
            if (!enabled)
                return new Color(1f, 1f, 1f, 0.02f);

            return pressed ? new Color(1f, 1f, 1f, 0.02f) : hovered ? new Color(1f, 1f, 1f, 0.14f) : new Color(1f, 1f, 1f, 0.10f);
        }

        public static Color GetActionBottomShadow(bool pressed, bool enabled)
        {
            return !enabled ? new Color(0f, 0f, 0f, 0.05f) : pressed ? new Color(0f, 0f, 0f, 0.10f) : new Color(0f, 0f, 0f, 0.22f);
        }

        public static Color GetActionIconColor(bool enabled)
        {
            return enabled ? Color.white : new Color(1f, 1f, 1f, 0.38f);
        }

        public static Color GetActionTextColor(bool enabled)
        {
            return enabled ? Color.white : new Color(1f, 1f, 1f, 0.42f);
        }

        public static Texture2D GetSectionBodyBackgroundTexture()
        {
            if (s_sectionBodyBackground != null)
                return s_sectionBodyBackground;

            s_sectionBodyBackground = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            s_sectionBodyBackground.SetPixel(0, 0, EditorGUIUtility.isProSkin
                ? new Color(0.145f, 0.145f, 0.145f, 1f)
                : new Color(0.975f, 0.975f, 0.975f, 1f));
            s_sectionBodyBackground.Apply();
            return s_sectionBodyBackground;
        }
    }

    internal static class EditorCompatibilityUtility
    {
        public static void RepaintAllViews()
        {
            InternalEditorUtility.RepaintAllViews();
            SceneView.RepaintAll();
        }
    }

    internal static class ImprovedEditorNotifications
    {
        public static void Info(string title, string message, float duration)
        {
            Show(title, message, duration);
        }

        public static void Success(string title, string message, float duration)
        {
            Show(title, message, duration);
        }

        public static void Warning(string title, string message, float duration)
        {
            Show(title, message, duration);
        }

        private static void Show(string title, string message, float duration)
        {
            EditorWindow window = EditorWindow.focusedWindow ?? SceneView.lastActiveSceneView;
            if (window == null)
                return;

            window.ShowNotification(new GUIContent(title + ": " + message), duration);
        }
    }
}
#endif
