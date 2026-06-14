#if UNITY_EDITOR
using UnityEngine;

namespace NoodleHammer.Transform.Editor
{
	/// <summary>
	/// ScriptableObject singleton for Transform Editor settings, persisted via ProjectSettingsAssetUtility.
	/// </summary>
	public sealed class TransformEditorSettingsStorage : ScriptableObject
	{
		private const string AssetPath = "ProjectSettings/NoodleHammer/TransformEditorSettings.asset";
		private const string LegacyAssetPath = "Assets/Noodle Hammer/Transform/Settings/Transform Editor Settings.asset";

		[SerializeField] internal bool enabled = TransformEditorSettings.D_Enabled;

		private static TransformEditorSettingsStorage s_instance;

		/// <summary>
		/// Lazy singleton backed by ProjectSettingsAssetUtility.LoadOrCreate.
		/// </summary>
		public static TransformEditorSettingsStorage instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = NoodleHammer.Core.Editor.ProjectSettingsAssetUtility
						.LoadOrCreate<TransformEditorSettingsStorage>(AssetPath, Initialize, LegacyAssetPath);
				}

				return s_instance;
			}
		}

		private static void Initialize(TransformEditorSettingsStorage storage)
		{
			storage.enabled = TransformEditorSettings.D_Enabled;
		}
	}
}
#endif
