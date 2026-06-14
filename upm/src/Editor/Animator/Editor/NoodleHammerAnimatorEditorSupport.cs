#if UNITY_EDITOR
namespace NoodleHammer.Animator.Editor
{
	/// <summary>
	/// Re-exports core utilities into the Animator namespace for backward compatibility.
	/// </summary>
	internal static class ImprovedEditorTheme
	{
		// ── Inspector Header ──
		public static void DrawInspectorHeader(string title, string description, bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawInspectorHeader(title, description, enabled);

		// ── Surface & Background ──
		public static readonly UnityEngine.Color Surface = NoodleHammer.Core.Editor.ImprovedEditorTheme.Surface;
		public static readonly UnityEngine.Color RowSurfaceA = NoodleHammer.Core.Editor.ImprovedEditorTheme.RowSurfaceA;
		public static readonly UnityEngine.Color RowSurfaceB = NoodleHammer.Core.Editor.ImprovedEditorTheme.RowSurfaceB;

		// ── Borders ──
		public static readonly UnityEngine.Color BorderStrong = NoodleHammer.Core.Editor.ImprovedEditorTheme.BorderStrong;

		// ── Semantic Colors ──
		public static readonly UnityEngine.Color Accent = NoodleHammer.Core.Editor.ImprovedEditorTheme.Accent;
		public static readonly UnityEngine.Color AccentBright = NoodleHammer.Core.Editor.ImprovedEditorTheme.AccentBright;
		public static readonly UnityEngine.Color Success = NoodleHammer.Core.Editor.ImprovedEditorTheme.Success;
		public static readonly UnityEngine.Color Warning = NoodleHammer.Core.Editor.ImprovedEditorTheme.Warning;
		public static readonly UnityEngine.Color Error = NoodleHammer.Core.Editor.ImprovedEditorTheme.Error;

		// ── Text ──
		public static readonly UnityEngine.Color Text = NoodleHammer.Core.Editor.ImprovedEditorTheme.Text;

		// ── Hierarchy Guides ──
		public static readonly UnityEngine.Color HierarchyGuide = NoodleHammer.Core.Editor.ImprovedEditorTheme.HierarchyGuide;

		// ── Delegated Methods ──
		public static void DrawOutline(UnityEngine.Rect rect, UnityEngine.Color color)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawOutline(rect, color);

		public static bool DrawSectionHeader(bool expanded, string title, string subtitle, bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawSectionHeader(expanded, title, subtitle, enabled);

		public static void DrawAlternatingRowBackground(UnityEngine.Rect rect, int rowIndex, float horizontalExpand, float verticalExpand)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawAlternatingRowBackground(rect, rowIndex, horizontalExpand, verticalExpand);

		public static void DrawHierarchyGuide(UnityEngine.Rect rect, float guideX)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawHierarchyGuide(rect, guideX);

		public static float GetStyledSliderHeight(float width, UnityEngine.GUIContent label)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.GetStyledSliderHeight(width, label);

		public static float DrawStyledSlider(UnityEngine.Rect rect, UnityEngine.GUIContent label, float value, float min, float max, int decimals)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawStyledSlider(rect, label, value, min, max, decimals);

		public static UnityEngine.Color GetActionFill(UnityEngine.Color accent, bool hovered, bool pressed, bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.GetActionFill(accent, hovered, pressed, enabled);

		public static UnityEngine.Color GetActionBorder(UnityEngine.Color accent, bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.GetActionBorder(accent, enabled);

		public static UnityEngine.Color GetActionTopHighlight(bool hovered, bool pressed, bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.GetActionTopHighlight(hovered, pressed, enabled);

		public static UnityEngine.Color GetActionBottomShadow(bool pressed, bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.GetActionBottomShadow(pressed, enabled);

		public static UnityEngine.Color GetActionIconColor(bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.GetActionIconColor(enabled);

		public static UnityEngine.Color GetActionTextColor(bool enabled)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.GetActionTextColor(enabled);

		public static UnityEngine.Texture2D GetSectionBodyBackgroundTexture()
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.GetSectionBodyBackgroundTexture();
	}

	internal static class EditorCompatibilityUtility
	{
		public static void RepaintAllViews()
			=> NoodleHammer.Core.Editor.EditorCompatibilityUtility.RepaintAllViews();
	}

	internal static class ImprovedEditorNotifications
	{
		public static void Info(string title, string message, float duration)
			=> NoodleHammer.Core.Editor.ImprovedEditorNotifications.Info(title, message, duration);

		public static void Success(string title, string message, float duration)
			=> NoodleHammer.Core.Editor.ImprovedEditorNotifications.Success(title, message, duration);

		public static void Warning(string title, string message, float duration)
			=> NoodleHammer.Core.Editor.ImprovedEditorNotifications.Warning(title, message, duration);
	}

	internal static class EditorTransitionGuard
	{
		public static bool IsUnsafeTransition()
			=> NoodleHammer.Core.Editor.EditorTransitionGuard.IsUnsafeTransition();
	}

	internal static class EditorUpdateLoop
	{
		public static void EnsureRegistered(ref bool isRegistered, UnityEditor.EditorApplication.CallbackFunction callback)
			=> NoodleHammer.Core.Editor.EditorUpdateLoop.EnsureRegistered(ref isRegistered, callback);

		public static void EnsureUnregistered(ref bool isRegistered, UnityEditor.EditorApplication.CallbackFunction callback)
			=> NoodleHammer.Core.Editor.EditorUpdateLoop.EnsureUnregistered(ref isRegistered, callback);
	}

	internal static class NoodleHammerDiagnostics
	{
		public static void Log(string area, string message)
			=> NoodleHammer.Core.Editor.NoodleHammerDiagnostics.Log(area, message);

		public static void Warn(string area, string message)
			=> NoodleHammer.Core.Editor.NoodleHammerDiagnostics.Warn(area, message);

		public static void Error(string area, string message)
			=> NoodleHammer.Core.Editor.NoodleHammerDiagnostics.Error(area, message);
	}
}
#endif
