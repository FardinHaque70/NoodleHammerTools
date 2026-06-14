#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Transform.Editor
{
	/// <summary>
	/// Registers Transform Editor settings under Edit > Project Settings > Noodle Hammer / Transform Editor.
	/// </summary>
	internal static class TransformEditorSettingsProvider
	{
		private static SerializedObject s_serializedObject;

		[SettingsProvider]
		public static SettingsProvider CreateProvider()
		{
			return new SettingsProvider("Project/Noodle Hammer/Transform Editor", SettingsScope.Project)
			{
				label = "Transform Editor",
				guiHandler = DrawSettingsGUI,
				keywords = new[] { "Noodle", "Hammer", "Transform", "Copy", "Paste" }
			};
		}

		private static void DrawSettingsGUI(string searchContext)
		{
			TransformEditorSettingsStorage storage = TransformEditorSettingsStorage.instance;
			if (storage == null)
				return;

			if (s_serializedObject == null || s_serializedObject.targetObject != storage)
				s_serializedObject = new SerializedObject(storage);

			s_serializedObject.Update();

			EditorGUILayout.Space(8f);

			EditorGUILayout.LabelField("Noodle Hammer Transform Editor", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Configure the custom Transform inspector toolbar and copy/paste behavior.", EditorStyles.wordWrappedMiniLabel);

			EditorGUILayout.Space(4f);

			SerializedProperty enabledProp = s_serializedObject.FindProperty("enabled");
			EditorGUILayout.PropertyField(enabledProp, new GUIContent("Enabled", "Enable or disable the custom Transform inspector."));

			EditorGUILayout.Space(12f);
			if (GUILayout.Button("Reset To Defaults", GUILayout.Height(28f)) &&
			    EditorUtility.DisplayDialog(
				    "Reset Transform Editor Settings",
				    "Reset all Transform Editor settings back to their default values?",
				    "Reset",
				    "Cancel"))
			{
				NoodleHammer.Core.Editor.ProjectSettingsUndoUtility.ResetToDefaultsWithUndo(
					storage,
					"Reset Transform Editor Settings",
					() => { storage.enabled = TransformEditorSettings.D_Enabled; },
					() => { NoodleHammer.Core.Editor.ProjectSettingsAssetUtility.Save(storage); });
				s_serializedObject.Update();
			}

			if (s_serializedObject.ApplyModifiedProperties())
			{
				NoodleHammer.Core.Editor.ProjectSettingsAssetUtility.Save(storage);
			}
		}
	}
}
#endif
