#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Hierarchy.Editor
{
	public sealed class ImprovedHierarchySettingsAsset : ScriptableObject
	{
		public bool isActive = ImprovedHierarchySettings.D_IsActive;
		public bool alwaysShowFirstComponentIcon = ImprovedHierarchySettings.D_AlwaysShowFirstComponentIcon;
		public bool overridePrefabIcons = ImprovedHierarchySettings.D_OverridePrefabIcons;
		public bool enableTooltips = ImprovedHierarchySettings.D_EnableTooltips;
		public bool enableAlternatingRows = ImprovedHierarchySettings.D_EnableAlternatingRows;
		public bool enableRowDividers = ImprovedHierarchySettings.D_EnableRowDividers;
		public bool enableTreeGuides = ImprovedHierarchySettings.D_EnableTreeGuides;
		public ImprovedHierarchySettings.UnityNativeDetectionMode nativeDetection = ImprovedHierarchySettings.D_NativeDetection;
		public ImprovedHierarchySettings.IconDisplayMode missingScriptsIconMode = ImprovedHierarchySettings.D_MissingScriptsIconMode;
		public ImprovedHierarchySettings.IconDisplayMode noScriptsIconMode = ImprovedHierarchySettings.D_NoScriptsIconMode;
		public ImprovedHierarchySettings.IconDisplayMode singleUserScriptIconMode = ImprovedHierarchySettings.D_SingleUserScriptIconMode;
		public ImprovedHierarchySettings.IconDisplayMode unityScriptsOnlyIconMode = ImprovedHierarchySettings.D_UnityScriptsOnlyIconMode;
		public ImprovedHierarchySettings.IconDisplayMode containsUserScriptsIconMode = ImprovedHierarchySettings.D_ContainsUserScriptsIconMode;
		public ImprovedHierarchySettings.IconDisplayMode prefabIconMode = ImprovedHierarchySettings.D_PrefabIconMode;

		private void OnValidate()
		{
			EditorApplication.RepaintHierarchyWindow();
		}
	}

	public static class ImprovedHierarchySettings
	{
		public enum IconDisplayMode
		{
			SmallIcon,
			LargeIcon,
			UnityDefault
		}

		public enum UnityNativeDetectionMode
		{
			UnityEngine,
			Unity,
			None
		}

		private const string AssetPath = "ProjectSettings/NoodleHammer/ImprovedHierarchySettings.asset";
		private const string LegacyAssetPath = "Assets/Noodle Hammer/Hierarchy/Settings/Noodle Hammer Hierarchy Settings.asset";

		public const bool D_IsActive = true;
		public const bool D_AlwaysShowFirstComponentIcon = false;
		public const bool D_OverridePrefabIcons = false;
		public const bool D_EnableTooltips = true;
		public const bool D_EnableAlternatingRows = true;
		public const bool D_EnableRowDividers = true;
		public const bool D_EnableTreeGuides = true;
		public const UnityNativeDetectionMode D_NativeDetection = UnityNativeDetectionMode.Unity;
		public const IconDisplayMode D_MissingScriptsIconMode = IconDisplayMode.LargeIcon;
		public const IconDisplayMode D_NoScriptsIconMode = IconDisplayMode.LargeIcon;
		public const IconDisplayMode D_SingleUserScriptIconMode = IconDisplayMode.SmallIcon;
		public const IconDisplayMode D_UnityScriptsOnlyIconMode = IconDisplayMode.LargeIcon;
		public const IconDisplayMode D_ContainsUserScriptsIconMode = IconDisplayMode.SmallIcon;
		public const IconDisplayMode D_PrefabIconMode = IconDisplayMode.SmallIcon;

		private static ImprovedHierarchySettingsAsset s_cachedAsset;

		public static bool Active => Asset != null && Asset.isActive;
		public static bool AlwaysShowFirstComponentIcon => Asset.alwaysShowFirstComponentIcon;
		public static bool OverridePrefabIcons => Asset.overridePrefabIcons;
		public static bool EnableTooltips => Asset.enableTooltips;
		public static bool EnableAlternatingRows => Asset.enableAlternatingRows;
		public static bool EnableRowDividers => Asset.enableRowDividers;
		public static bool EnableTreeGuides => Asset.enableTreeGuides;
		public static UnityNativeDetectionMode NativeDetection => Asset.nativeDetection;
		public static IconDisplayMode MissingScriptsIconMode => Asset.missingScriptsIconMode;
		public static IconDisplayMode NoScriptsIconMode => Asset.noScriptsIconMode;
		public static IconDisplayMode SingleUserScriptIconMode => Asset.singleUserScriptIconMode;
		public static IconDisplayMode UnityScriptsOnlyIconMode => Asset.unityScriptsOnlyIconMode;
		public static IconDisplayMode ContainsUserScriptsIconMode => Asset.containsUserScriptsIconMode;
		public static IconDisplayMode PrefabIconMode => Asset.prefabIconMode;

		public static ImprovedHierarchySettingsAsset Asset
		{
			get
			{
				if (s_cachedAsset == null)
				{
					s_cachedAsset = NoodleHammer.Core.Editor.ProjectSettingsAssetUtility
						.LoadOrCreate<ImprovedHierarchySettingsAsset>(AssetPath, ApplyDefaults, LegacyAssetPath);
				}

				return s_cachedAsset;
			}
		}

		internal static IconDisplayMode GetIconMode(ImprovedHierarchyRuleCategory category)
		{
			switch (category)
			{
				case ImprovedHierarchyRuleCategory.MissingScripts:
					return MissingScriptsIconMode;
				case ImprovedHierarchyRuleCategory.NoScripts:
					return NoScriptsIconMode;
				case ImprovedHierarchyRuleCategory.SingleUserScript:
					return SingleUserScriptIconMode;
				case ImprovedHierarchyRuleCategory.ContainsUserScripts:
					return ContainsUserScriptsIconMode;
				case ImprovedHierarchyRuleCategory.UnityScriptsOnly:
				default:
					return UnityScriptsOnlyIconMode;
			}
		}

		public static void ApplyDefaults(ImprovedHierarchySettingsAsset asset)
		{
			if (asset == null)
				return;

			asset.isActive = D_IsActive;
			asset.alwaysShowFirstComponentIcon = D_AlwaysShowFirstComponentIcon;
			asset.overridePrefabIcons = D_OverridePrefabIcons;
			asset.enableTooltips = D_EnableTooltips;
			asset.enableAlternatingRows = D_EnableAlternatingRows;
			asset.enableRowDividers = D_EnableRowDividers;
			asset.enableTreeGuides = D_EnableTreeGuides;
			asset.nativeDetection = D_NativeDetection;
			asset.missingScriptsIconMode = D_MissingScriptsIconMode;
			asset.noScriptsIconMode = D_NoScriptsIconMode;
			asset.singleUserScriptIconMode = D_SingleUserScriptIconMode;
			asset.unityScriptsOnlyIconMode = D_UnityScriptsOnlyIconMode;
			asset.containsUserScriptsIconMode = D_ContainsUserScriptsIconMode;
			asset.prefabIconMode = D_PrefabIconMode;
		}
	}
}
#endif
