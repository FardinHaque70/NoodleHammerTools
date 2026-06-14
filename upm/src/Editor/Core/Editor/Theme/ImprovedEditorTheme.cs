using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Shared visual theme for all Noodle Hammer editor tools.
	/// Provides consistent colors, section headers, row backgrounds, and common drawing utilities.
	/// </summary>
	public static class ImprovedEditorTheme
	{
		// ── Surface & Background ──

		public static readonly Color Surface = EditorGUIUtility.isProSkin
			? new Color(0.165f, 0.165f, 0.165f, 1f)
			: new Color(0.93f, 0.93f, 0.93f, 1f);

		public static readonly Color TooltipBackground = EditorGUIUtility.isProSkin
			? new Color(0.13f, 0.13f, 0.13f, 0.97f)
			: new Color(0.98f, 0.98f, 0.98f, 0.97f);

		public static readonly Color RowSurfaceA = EditorGUIUtility.isProSkin
			? new Color(0.20f, 0.20f, 0.20f, 1f)
			: new Color(0.965f, 0.965f, 0.965f, 1f);

		public static readonly Color RowSurfaceB = EditorGUIUtility.isProSkin
			? new Color(0.175f, 0.175f, 0.175f, 1f)
			: new Color(0.945f, 0.945f, 0.945f, 1f);

		// ── Borders ──

		public static readonly Color BorderStrong = EditorGUIUtility.isProSkin
			? new Color(0f, 0f, 0f, 0.48f)
			: new Color(0f, 0f, 0f, 0.16f);

		// ── Semantic Colors ──

		public static readonly Color Accent = new Color(0.22f, 0.55f, 0.95f, 1f);
		public static readonly Color AccentBright = new Color(0.78f, 0.88f, 1f, 1f);
		public static readonly Color Success = new Color(0.23f, 0.66f, 0.34f, 1f);
		public static readonly Color Warning = new Color(0.91f, 0.62f, 0.20f, 1f);
		public static readonly Color Error = new Color(0.80f, 0.29f, 0.24f, 1f);

		// ── Text ──

		public static readonly Color Text = EditorGUIUtility.isProSkin
			? new Color(0.92f, 0.92f, 0.92f, 1f)
			: new Color(0.16f, 0.16f, 0.16f, 1f);

		// ── Hierarchy Guides ──

		public static readonly Color HierarchyGuide = EditorGUIUtility.isProSkin
			? new Color(1f, 1f, 1f, 0.16f)
			: new Color(0f, 0f, 0f, 0.14f);

		// ── Cached Textures ──

		private static Texture2D s_sectionBodyBackground;

		// ── Drawing Utilities ──

		/// <summary>
		/// Draws a 1px outline around the given rect.
		/// </summary>
		public static void DrawOutline(Rect rect, Color color)
		{
			EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
			EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
			EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
			EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
		}

		// ── Inspector Header ──

		/// <summary>
		/// Draws a full-width header with title and description, used at the top of custom inspectors.
		/// </summary>
		public static void DrawInspectorHeader(string title, string description, bool enabled)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, 48f);
			EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
				? new Color(0.17f, 0.17f, 0.17f, 1f)
				: new Color(0.94f, 0.94f, 0.94f, 1f));
			DrawOutline(rect, BorderStrong);

			Rect titleRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 18f);
			Rect descriptionRect = new Rect(rect.x + 10f, rect.y + 24f, rect.width - 20f, 18f);

			EditorGUI.LabelField(titleRect, title, EditorStyles.boldLabel);
			using (new EditorGUI.DisabledScope(!enabled))
				EditorGUI.LabelField(descriptionRect, description, EditorStyles.miniLabel);
		}

		// ── Section Headers ──

		/// <summary>
		/// Draws a collapsible section header with foldout, title, and subtitle.
		/// </summary>
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

		/// <summary>
		/// Draws a compact collapsible section header with foldout style, used for settings sub-sections.
		/// </summary>
		public static bool DrawCompactSectionHeader(bool expanded, string title, string subtitle, bool enabled)
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

		// ── Section Body ──

		/// <summary>
		/// Begins a section body area with help box styling.
		/// </summary>
		public static void BeginSectionBody(bool padTop)
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			if (padTop)
				EditorGUILayout.Space(2f);
		}

		/// <summary>
		/// Ends a section body area.
		/// </summary>
		public static void EndSectionBody()
		{
			EditorGUILayout.EndVertical();
		}

		// ── Toggle Header ──

		/// <summary>
		/// Draws a toggle property field as a header element.
		/// </summary>
		public static void DrawToggleHeader(SerializedProperty property)
		{
			if (property == null)
				return;

			EditorGUILayout.PropertyField(property, new GUIContent("Enabled"));
			EditorGUILayout.Space(2f);
		}

		/// <summary>
		/// Draws an inline toggle control at the given rect.
		/// </summary>
		public static void DrawInlineToggle(Rect rect, SerializedProperty property)
		{
			if (property == null)
				return;

			EditorGUI.BeginChangeCheck();
			bool newValue = GUI.Toggle(rect, property.boolValue, GUIContent.none);
			if (EditorGUI.EndChangeCheck())
				property.boolValue = newValue;
		}

		// ── Row Backgrounds ──

		/// <summary>
		/// Draws an alternating row background using solid surface colors.
		/// </summary>
		public static void DrawAlternatingRowBackground(Rect rect, int rowIndex, float horizontalExpand, float verticalExpand)
		{
			Rect backgroundRect = new Rect(
				rect.x - horizontalExpand,
				rect.y - verticalExpand,
				rect.width + horizontalExpand * 2f,
				rect.height + verticalExpand * 2f);
			EditorGUI.DrawRect(backgroundRect, (rowIndex & 1) == 0 ? RowSurfaceA : RowSurfaceB);
		}

		/// <summary>
		/// Draws a subtle alternating row background using transparent overlays.
		/// </summary>
		public static void DrawSubtleAlternatingRowBackground(Rect rect, int rowIndex, float horizontalExpand, float verticalExpand)
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

		// ── Hierarchy Guides ──

		/// <summary>
		/// Draws vertical and horizontal tree guide lines at the given position.
		/// </summary>
		public static void DrawHierarchyGuide(Rect rect, float guideX)
		{
			EditorGUI.DrawRect(new Rect(guideX, rect.y + 2f, 1f, Mathf.Max(0f, rect.height - 4f)), HierarchyGuide);
			EditorGUI.DrawRect(new Rect(guideX, rect.center.y, 12f, 1f), HierarchyGuide);
		}

		// ── Sliders ──

		/// <summary>
		/// Returns the required height for a styled slider with label.
		/// </summary>
		public static float GetStyledSliderHeight(float width, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 2f + 8f;
		}

		/// <summary>
		/// Draws a slider with a label above it, returning the new value.
		/// </summary>
		public static float DrawStyledSlider(Rect rect, GUIContent label, float value, float min, float max, int decimals)
		{
			Rect labelRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
			Rect sliderRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 6f, rect.width, EditorGUIUtility.singleLineHeight);

			EditorGUI.LabelField(labelRect, label);
			float newValue = GUI.HorizontalSlider(sliderRect, value, min, max);
			return (float)System.Math.Round(newValue, decimals);
		}

		// ── Toggle Height ──

		/// <summary>
		/// Returns the required height for a styled toggle with label content.
		/// </summary>
		public static float GetStyledToggleHeight(float width, GUIContent content, float toggleWidth)
		{
			float labelHeight = EditorStyles.label.CalcHeight(content, Mathf.Max(40f, width - toggleWidth - 10f));
			return Mathf.Max(18f, labelHeight);
		}

		// ── Action Button Styling ──

		/// <summary>
		/// Returns the fill color for an action button based on its state.
		/// </summary>
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

		/// <summary>
		/// Returns the border color for an action button.
		/// </summary>
		public static Color GetActionBorder(Color accent, bool enabled)
		{
			return enabled ? Color.Lerp(accent, Color.black, 0.45f) : new Color(0f, 0f, 0f, 0.20f);
		}

		/// <summary>
		/// Returns the top highlight color for an action button.
		/// </summary>
		public static Color GetActionTopHighlight(bool hovered, bool pressed, bool enabled)
		{
			if (!enabled)
				return new Color(1f, 1f, 1f, 0.02f);

			return pressed ? new Color(1f, 1f, 1f, 0.02f) : hovered ? new Color(1f, 1f, 1f, 0.14f) : new Color(1f, 1f, 1f, 0.10f);
		}

		/// <summary>
		/// Returns the bottom shadow color for an action button.
		/// </summary>
		public static Color GetActionBottomShadow(bool pressed, bool enabled)
		{
			return !enabled ? new Color(0f, 0f, 0f, 0.05f) : pressed ? new Color(0f, 0f, 0f, 0.10f) : new Color(0f, 0f, 0f, 0.22f);
		}

		/// <summary>
		/// Returns the icon tint color for an action button.
		/// </summary>
		public static Color GetActionIconColor(bool enabled)
		{
			return enabled ? Color.white : new Color(1f, 1f, 1f, 0.38f);
		}

		/// <summary>
		/// Returns the text color for an action button.
		/// </summary>
		public static Color GetActionTextColor(bool enabled)
		{
			return enabled ? Color.white : new Color(1f, 1f, 1f, 0.42f);
		}

		// ── Cached Textures ──

		/// <summary>
		/// Returns a reusable 1x1 texture for section body backgrounds.
		/// </summary>
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
}
