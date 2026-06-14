#if UNITY_EDITOR
namespace NoodleHammer.Transform.Editor
{
	/// <summary>
	/// Static defaults and property accessors for Transform Editor settings.
	/// </summary>
	public static class TransformEditorSettings
	{
		public const bool D_Enabled = true;

		/// <summary>
		/// Whether the Transform Editor custom inspector is enabled.
		/// </summary>
		public static bool Enabled
		{
			get => TransformEditorSettingsStorage.instance.enabled;
			set => TransformEditorSettingsStorage.instance.enabled = value;
		}
	}
}
#endif
