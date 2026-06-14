#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace NoodleHammer.Hierarchy.Editor
{
	/// <summary>
	/// Registers Hierarchy settings under Edit > Project Settings > Noodle Hammer / Improved Hierarchy.
	/// </summary>
	internal static class ImprovedHierarchySettingsProvider
	{
		private const float ResetButtonWidth = 24f;
		private const float ResetButtonGap = 6f;
		private const float ToggleWidth = 46f;
		private const float ToggleHeight = 18f;
		private const float RowHorizontalPadding = 8f;
		private const float RowVerticalPadding = 6f;
		private const float RowBackgroundHorizontalExpand = 12f;
		private const float RowBackgroundVerticalExpand = 0f;
		private const float RowHierarchyIndent = 24f;

		private static SerializedObject s_serializedObject;
		private static ImprovedHierarchySettingsAsset s_defaultValues;
		private static GUIContent s_resetButtonContent;
		private static int s_sectionRowIndex;

		[SettingsProvider]
		public static SettingsProvider CreateProvider()
		{
			return new SettingsProvider("Project/Noodle Hammer/Improved Hierarchy", SettingsScope.Project)
			{
				label = "Improved Hierarchy",
				guiHandler = DrawSettingsGUI,
				keywords = new[] { "Noodle", "Hammer", "Hierarchy", "Icon", "Tree" }
			};
		}

		private static void DrawSettingsGUI(string searchContext)
		{
			ImprovedHierarchySettingsAsset asset = ImprovedHierarchySettings.Asset;
			if (asset == null)
				return;

			if (s_serializedObject == null || s_serializedObject.targetObject != asset)
				s_serializedObject = new SerializedObject(asset);

			if (s_defaultValues == null)
			{
				s_defaultValues = ScriptableObject.CreateInstance<ImprovedHierarchySettingsAsset>();
				ImprovedHierarchySettings.ApplyDefaults(s_defaultValues);
			}

			if (s_resetButtonContent == null)
			{
				s_resetButtonContent = EditorGUIUtility.IconContent("TreeEditor.Refresh");
				s_resetButtonContent.text = string.Empty;
				s_resetButtonContent.tooltip = "Reset this setting to its default value.";
			}

			s_serializedObject.Update();

			EditorGUILayout.Space(8f);

			bool isActive = s_serializedObject.FindProperty("isActive").boolValue;
			ImprovedEditorTheme.DrawInspectorHeader(
				"Noodle Hammer Hierarchy",
				"Replace hierarchy row icons with smarter component-aware icons.",
				isActive);

			ImprovedEditorTheme.DrawToggleHeader(s_serializedObject.FindProperty("isActive"));

			// General section
			EditorGUILayout.Space(4f);
			EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
			BeginSectionBody();
			DrawProperty("alwaysShowFirstComponentIcon", "Always Show First Component Icon", "Use the first non-Transform component icon more aggressively.");
			DrawProperty("enableTooltips", "Enable Hierarchy Icon Tooltips", "Show the component type name when hovering the hierarchy icon.");
			DrawProperty("enableAlternatingRows", "Enable Alternating Rows", "Draw subtle alternating row backgrounds across the Hierarchy.");
			DrawProperty("enableRowDividers", "Enable Row Dividers", "Draw a divider line after each hierarchy row.");
			DrawProperty("enableTreeGuides", "Enable Tree Guides", "Draw vertical and horizontal guide lines to show parent-child relationships.");

			// Classification section
			EditorGUILayout.Space(8f);
			EditorGUILayout.LabelField("Classification", EditorStyles.boldLabel);
			s_sectionRowIndex = 0;
			DrawProperty("nativeDetection", "Unity Native Script Keyword", "Controls how namespaces are treated as Unity/native code.");
			DrawProperty("missingScriptsIconMode", "Contains Missing Scripts", "Icon style for objects with one or more missing scripts.");
			DrawProperty("noScriptsIconMode", "Contains No Scripts", "Icon style for objects that only contain Transform.");
			DrawProperty("singleUserScriptIconMode", "Contains Single User Script", "Icon style for objects with a single user-authored component.");
			DrawProperty("unityScriptsOnlyIconMode", "Contains Unity Scripts Only", "Icon style for objects that only contain Unity/native components.");
			DrawProperty("containsUserScriptsIconMode", "Contains User Scripts", "Icon style for objects that contain user-authored components.");

			// Prefab Override section
			EditorGUILayout.Space(8f);
			EditorGUILayout.LabelField("Prefab Override", EditorStyles.boldLabel);
			s_sectionRowIndex = 0;
			DrawProperty("overridePrefabIcons", "Override Prefab Icons", "When enabled, prefab instances use the prefab icon rule instead of the general category rule.");
			using (new EditorGUI.DisabledScope(!s_serializedObject.FindProperty("overridePrefabIcons").boolValue))
				DrawProperty("prefabIconMode", "Prefab Icon Style", "Icon style to use for prefab instances when override is enabled.");

			EditorGUILayout.Space(12f);
			if (GUILayout.Button("Reset To Defaults", GUILayout.Height(28f)) &&
			    EditorUtility.DisplayDialog(
				    "Reset Noodle Hammer Hierarchy Settings",
				    "Reset all Noodle Hammer Hierarchy settings back to their default values?",
				    "Reset",
				    "Cancel"))
			{
				NoodleHammer.Core.Editor.ProjectSettingsUndoUtility.ResetToDefaultsWithUndo(
					asset,
					"Reset Improved Hierarchy Settings",
					() => ImprovedHierarchySettings.ApplyDefaults(asset),
					() =>
					{
						NoodleHammer.Core.Editor.ProjectSettingsAssetUtility.Save(asset);
						EditorApplication.RepaintHierarchyWindow();
					});
				s_serializedObject.Update();
			}

			if (s_serializedObject.ApplyModifiedProperties())
			{
				NoodleHammer.Core.Editor.ProjectSettingsAssetUtility.Save(asset);
				EditorApplication.RepaintHierarchyWindow();
			}
		}

		private static void BeginSectionBody()
		{
			s_sectionRowIndex = 0;
		}

		private static void DrawProperty(string propertyName, string label, string tooltip)
		{
			SerializedProperty property = s_serializedObject.FindProperty(propertyName);
			if (property == null)
				return;

			GUIContent content = new GUIContent(label, tooltip);
			float fieldHeight = EditorGUI.GetPropertyHeight(property, content, true);
			Rect rowRect = EditorGUILayout.GetControlRect(true, fieldHeight + RowVerticalPadding * 2f);
			int rowIndex = s_sectionRowIndex++;

			Rect fieldRect = new Rect(
				rowRect.x + RowHorizontalPadding,
				rowRect.y + RowVerticalPadding,
				rowRect.width - RowHorizontalPadding * 2f - ResetButtonWidth - ResetButtonGap,
				fieldHeight);
			Rect buttonRect = new Rect(
				rowRect.xMax - RowHorizontalPadding - ResetButtonWidth,
				rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f,
				ResetButtonWidth,
				EditorGUIUtility.singleLineHeight);

			EditorGUI.PropertyField(fieldRect, property, content, true);
			DrawResetButton(buttonRect, propertyName);
		}

		private static void DrawResetButton(Rect rect, string propertyName)
		{
			bool canReset = CanResetProperty(propertyName);
			Color previousBackground = GUI.backgroundColor;
			if (canReset)
				GUI.backgroundColor = ImprovedEditorTheme.Accent;

			bool clicked = GUI.Button(rect, s_resetButtonContent, EditorStyles.miniButton);
			GUI.backgroundColor = previousBackground;

			if (clicked)
				ResetPropertyToDefault(propertyName);
		}

		private static bool CanResetProperty(string propertyName)
		{
			SerializedProperty property = s_serializedObject.FindProperty(propertyName);
			if (property == null || s_defaultValues == null)
				return false;

			FieldInfo field = typeof(ImprovedHierarchySettingsAsset).GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
				return false;

			object defaultValue = field.GetValue(s_defaultValues);
			return !IsPropertyAtDefault(property, defaultValue);
		}

		private static bool IsPropertyAtDefault(SerializedProperty property, object defaultValue)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Boolean:
					return property.boolValue == (bool)defaultValue;
				case SerializedPropertyType.Enum:
					return property.enumValueIndex == (int)defaultValue;
				default:
					return true;
			}
		}

		private static void ResetPropertyToDefault(string propertyName)
		{
			SerializedProperty property = s_serializedObject.FindProperty(propertyName);
			if (property == null || s_defaultValues == null)
				return;

			FieldInfo field = typeof(ImprovedHierarchySettingsAsset).GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
				return;

			Undo.RecordObject(s_serializedObject.targetObject, $"Reset {propertyName}");
			object defaultValue = field.GetValue(s_defaultValues);

			switch (property.propertyType)
			{
				case SerializedPropertyType.Boolean:
					property.boolValue = (bool)defaultValue;
					break;
				case SerializedPropertyType.Enum:
					property.enumValueIndex = (int)defaultValue;
					break;
			}
		}
	}
}
#endif
