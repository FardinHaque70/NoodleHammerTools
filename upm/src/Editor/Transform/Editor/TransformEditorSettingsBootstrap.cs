#if UNITY_EDITOR
using UnityEditor;

namespace NoodleHammer.Transform.Editor
{
	/// <summary>
	/// Ensures the Transform Editor settings asset exists on editor startup.
	/// </summary>
	[InitializeOnLoad]
	internal static class TransformEditorSettingsBootstrap
	{
		static TransformEditorSettingsBootstrap()
		{
			NoodleHammer.Core.Editor.SettingsBootstrap.Register(() =>
			{
				var _ = TransformEditorSettingsStorage.instance;
			});
		}
	}
}
#endif
