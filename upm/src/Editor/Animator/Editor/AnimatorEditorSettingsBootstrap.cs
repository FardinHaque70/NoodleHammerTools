#if UNITY_EDITOR
using UnityEditor;

namespace NoodleHammer.Animator.Editor
{
	/// <summary>
	/// Triggers Animator Editor settings asset creation on editor startup.
	/// </summary>
	[InitializeOnLoad]
	internal static class AnimatorEditorSettingsBootstrap
	{
		static AnimatorEditorSettingsBootstrap()
		{
			NoodleHammer.Core.Editor.SettingsBootstrap.Register(() =>
			{
				var _ = AnimatorEditorSettingsStorage.instance;
			});
		}
	}
}
#endif
