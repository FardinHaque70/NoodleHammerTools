using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Cross-version compatibility helpers for Unity editor operations.
	/// Supports Unity 2022 through Unity 6 (6000.x).
	/// </summary>
	public static class EditorCompatibilityUtility
	{
		/// <summary>
		/// Forces a repaint on all editor views including the Scene View.
		/// </summary>
		public static void RepaintAllViews()
		{
			InternalEditorUtility.RepaintAllViews();
			SceneView.RepaintAll();
		}

		/// <summary>
		/// Resolves a UnityEngine.Object from an instance/entity ID.
		/// Uses EditorUtility.EntityIdToObject on Unity 6+ and EditorUtility.InstanceIDToObject on older versions.
		/// </summary>
		public static Object InstanceIDToObject(int instanceId)
		{
#if UNITY_6000_0_OR_NEWER
			return EditorUtility.EntityIdToObject(instanceId);
#else
			return EditorUtility.InstanceIDToObject(instanceId);
#endif
		}
	}
}
