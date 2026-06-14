using UnityEditor;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Centralizes detection of unsafe editor transitions so editor hooks can fail open
	/// while inspector targets are rebuilding.
	/// </summary>
	public static class EditorTransitionGuard
	{
		/// <summary>
		/// Returns true if the editor is compiling, updating, or transitioning play mode.
		/// </summary>
		public static bool IsUnsafeTransition()
		{
			return EditorApplication.isCompiling
			       || EditorApplication.isUpdating
			       || EditorApplication.isPlayingOrWillChangePlaymode;
		}
	}
}
