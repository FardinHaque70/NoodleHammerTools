#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Animator.Editor
{
	/// <summary>
	/// Registers Animator Editor settings under Edit > Project Settings > Noodle Hammer / Animator Editor.
	/// </summary>
	internal static class AnimatorEditorSettingsProvider
	{
		[SettingsProvider]
		public static SettingsProvider CreateProvider()
		{
			return new SettingsProvider("Project/Noodle Hammer/Animator Editor", SettingsScope.Project)
			{
				label = "Animator Editor",
				guiHandler = DrawSettingsGUI,
				keywords = new[] { "Noodle", "Hammer", "Animator", "Playback", "Speed" }
			};
		}

		private static SerializedObject s_serializedObject;

		private static void DrawSettingsGUI(string searchContext)
		{
			AnimatorEditorSettingsStorage storage = AnimatorEditorSettingsStorage.instance;
			if (storage == null)
				return;

			if (s_serializedObject == null || s_serializedObject.targetObject != storage)
				s_serializedObject = new SerializedObject(storage);

			s_serializedObject.Update();

			EditorGUILayout.Space(8f);

			ImprovedEditorTheme.DrawInspectorHeader(
				"Noodle Hammer Animator Editor",
				"Configure the custom Animator inspector's playback and preview behavior.",
				storage.enabled);

			EditorGUILayout.Space(4f);

			SerializedProperty enabledProp = s_serializedObject.FindProperty("enabled");
			SerializedProperty speedProp = s_serializedObject.FindProperty("defaultPlaybackSpeed");
			SerializedProperty expandProp = s_serializedObject.FindProperty("expandPlaybackByDefault");

			EditorGUILayout.PropertyField(enabledProp, new GUIContent("Enabled", "Enable or disable the custom Animator inspector."));

			EditorGUI.BeginDisabledGroup(!enabledProp.boolValue);
			EditorGUILayout.Slider(speedProp, 0.01f, 10f, new GUIContent("Default Playback Speed", "Default speed for animation preview playback."));
			EditorGUILayout.PropertyField(expandProp, new GUIContent("Expand Playback By Default", "Whether the playback section is expanded by default."));
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.Space(12f);
			if (GUILayout.Button("Reset To Defaults", GUILayout.Height(28f)) &&
			    EditorUtility.DisplayDialog(
				    "Reset Animator Editor Settings",
				    "Reset all Animator Editor settings back to their default values?",
				    "Reset",
				    "Cancel"))
			{
				NoodleHammer.Core.Editor.ProjectSettingsUndoUtility.ResetToDefaultsWithUndo(
					storage,
					"Reset Animator Editor Settings",
					() =>
					{
						storage.enabled = AnimatorEditorSettings.D_Enabled;
						storage.defaultPlaybackSpeed = AnimatorEditorSettings.D_DefaultPlaybackSpeed;
						storage.expandPlaybackByDefault = AnimatorEditorSettings.D_ExpandPlaybackByDefault;
					},
					() =>
					{
						NoodleHammer.Core.Editor.ProjectSettingsAssetUtility.Save(storage);
					});
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
