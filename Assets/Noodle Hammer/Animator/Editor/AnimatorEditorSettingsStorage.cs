#if UNITY_EDITOR
using UnityEngine;

namespace NoodleHammer.Animator.Editor
{
	/// <summary>
	/// ScriptableObject singleton for Animator Editor settings, persisted via ProjectSettingsAssetUtility.
	/// </summary>
	public sealed class AnimatorEditorSettingsStorage : ScriptableObject
	{
		private const string AssetPath = "ProjectSettings/NoodleHammer/AnimatorEditorSettings.asset";
		private const string LegacyAssetPath = "Assets/Noodle Hammer/Animator/Settings/Animator Editor Settings.asset";

		[SerializeField] internal bool enabled = AnimatorEditorSettings.D_Enabled;
		[SerializeField] internal float defaultPlaybackSpeed = AnimatorEditorSettings.D_DefaultPlaybackSpeed;
		[SerializeField] internal bool expandPlaybackByDefault = AnimatorEditorSettings.D_ExpandPlaybackByDefault;

		private static AnimatorEditorSettingsStorage s_instance;

		/// <summary>
		/// Lazy singleton backed by ProjectSettingsAssetUtility.LoadOrCreate.
		/// </summary>
		public static AnimatorEditorSettingsStorage instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = NoodleHammer.Core.Editor.ProjectSettingsAssetUtility
						.LoadOrCreate<AnimatorEditorSettingsStorage>(AssetPath, Initialize, LegacyAssetPath);
				}

				return s_instance;
			}
		}

		private static void Initialize(AnimatorEditorSettingsStorage storage)
		{
			storage.enabled = AnimatorEditorSettings.D_Enabled;
			storage.defaultPlaybackSpeed = AnimatorEditorSettings.D_DefaultPlaybackSpeed;
			storage.expandPlaybackByDefault = AnimatorEditorSettings.D_ExpandPlaybackByDefault;
		}
	}
}
#endif
