#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Hierarchy.Editor
{
	/// <summary>
	/// Re-exports core utilities into the Hierarchy namespace for backward compatibility.
	/// </summary>
	internal static class ImprovedEditorTheme
	{
		public static readonly Color Accent = NoodleHammer.Core.Editor.ImprovedEditorTheme.Accent;
		public static readonly Color HierarchyGuide = NoodleHammer.Core.Editor.ImprovedEditorTheme.HierarchyGuide;

		public static void DrawInspectorHeader(string title, string description, bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawInspectorHeader(title, description, enabled);

		public static void DrawToggleHeader(SerializedProperty property)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawToggleHeader(property);

		public static bool DrawSectionHeader(bool expanded, string title, string subtitle, bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawCompactSectionHeader(expanded, title, subtitle, enabled);

		public static void BeginSectionBody(bool padTop)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.BeginSectionBody(padTop);

		public static void EndSectionBody()
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.EndSectionBody();

		public static float GetStyledToggleHeight(float width, GUIContent content, float toggleWidth)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.GetStyledToggleHeight(width, content, toggleWidth);

		public static void DrawAlternatingRowBackground(Rect rect, int rowIndex, float horizontalExpand, float verticalExpand)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawSubtleAlternatingRowBackground(rect, rowIndex, horizontalExpand, verticalExpand);

		public static void DrawHierarchyGuide(Rect rect, float guideX)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawHierarchyGuide(rect, guideX);

		public static void DrawInlineToggle(Rect rect, SerializedProperty property)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawInlineToggle(rect, property);

		public static void DrawOutline(Rect rect, Color color)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawOutline(rect, color);
	}
}
#endif
