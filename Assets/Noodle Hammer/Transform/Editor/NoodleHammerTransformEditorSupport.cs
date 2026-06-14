#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NoodleHammer.Transform.Editor
{
    internal static class ImprovedEditorTheme
    {
        public static readonly Color TooltipBackground = EditorGUIUtility.isProSkin
            ? new Color(0.13f, 0.13f, 0.13f, 0.97f)
            : new Color(0.98f, 0.98f, 0.98f, 0.97f);

        public static readonly Color BorderStrong = EditorGUIUtility.isProSkin
            ? new Color(0f, 0f, 0f, 0.5f)
            : new Color(0f, 0f, 0f, 0.24f);

        public static readonly Color Text = EditorGUIUtility.isProSkin
            ? new Color(0.92f, 0.92f, 0.92f, 1f)
            : new Color(0.16f, 0.16f, 0.16f, 1f);

        public static void DrawOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
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
}
#endif
