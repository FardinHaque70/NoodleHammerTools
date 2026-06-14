#if UNITY_EDITOR
using UnityEngine;

namespace NoodleHammer.Animator.Editor
{
	/// <summary>
	/// Static defaults and clamped property accessors for Animator Editor settings.
	/// </summary>
	public static class AnimatorEditorSettings
	{
		public const bool D_Enabled = true;
		public const float D_DefaultPlaybackSpeed = 1f;
		public const bool D_ExpandPlaybackByDefault = true;

		private const float MinPlaybackSpeed = 0.01f;
		private const float MaxPlaybackSpeed = 10f;

		/// <summary>
		/// Whether the Animator Editor custom inspector is enabled.
		/// </summary>
		public static bool Enabled
		{
			get => AnimatorEditorSettingsStorage.instance.enabled;
			set => AnimatorEditorSettingsStorage.instance.enabled = value;
		}

		/// <summary>
		/// Default playback speed for animation previews, clamped between 0.01 and 10.
		/// </summary>
		public static float DefaultPlaybackSpeed
		{
			get => AnimatorEditorSettingsStorage.instance.defaultPlaybackSpeed;
			set => AnimatorEditorSettingsStorage.instance.defaultPlaybackSpeed =
				Mathf.Clamp(value, MinPlaybackSpeed, MaxPlaybackSpeed);
		}

		/// <summary>
		/// Whether the playback section is expanded by default.
		/// </summary>
		public static bool ExpandPlaybackByDefault
		{
			get => AnimatorEditorSettingsStorage.instance.expandPlaybackByDefault;
			set => AnimatorEditorSettingsStorage.instance.expandPlaybackByDefault = value;
		}
	}
}
#endif
