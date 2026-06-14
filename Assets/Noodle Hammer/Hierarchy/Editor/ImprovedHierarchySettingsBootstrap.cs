#if UNITY_EDITOR
using UnityEditor;

namespace NoodleHammer.Hierarchy.Editor
{
	/// <summary>
	/// Ensures the Hierarchy settings asset exists on editor startup.
	/// </summary>
	[InitializeOnLoad]
	internal static class ImprovedHierarchySettingsBootstrap
	{
		static ImprovedHierarchySettingsBootstrap()
		{
			NoodleHammer.Core.Editor.SettingsBootstrap.Register(() =>
			{
				var _ = ImprovedHierarchySettings.Asset;
			});
		}
	}
}
#endif
