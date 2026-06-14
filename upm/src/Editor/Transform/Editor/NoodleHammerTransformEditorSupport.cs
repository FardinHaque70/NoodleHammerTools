#if UNITY_EDITOR
namespace NoodleHammer.Transform.Editor
{
	/// <summary>
	/// Re-exports core utilities into the Transform namespace for backward compatibility.
	/// </summary>
	internal static class ImprovedEditorTheme
	{
		public static readonly UnityEngine.Color TooltipBackground = NoodleHammer.Core.Editor.ImprovedEditorTheme.TooltipBackground;
		public static readonly UnityEngine.Color BorderStrong = NoodleHammer.Core.Editor.ImprovedEditorTheme.BorderStrong;
		public static readonly UnityEngine.Color Text = NoodleHammer.Core.Editor.ImprovedEditorTheme.Text;

		public static void DrawOutline(UnityEngine.Rect rect, UnityEngine.Color color)
			=> NoodleHammer.Core.Editor.ImprovedEditorTheme.DrawOutline(rect, color);
	}

	internal static class EditorCompatibilityUtility
	{
		public static void RepaintAllViews()
			=> NoodleHammer.Core.Editor.EditorCompatibilityUtility.RepaintAllViews();
	}
}
#endif
