using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Reflection;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Cross-version compatibility helpers for Unity editor operations.
	/// Supports Unity 2022 through Unity 6 (6000.x).
	/// </summary>
	public static class EditorCompatibilityUtility
	{
		private static readonly MethodInfo EntityIdToObjectMethod =
			typeof(EditorUtility).GetMethod("EntityIdToObject", BindingFlags.Public | BindingFlags.Static);

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
			if (EntityIdToObjectMethod != null)
			{
				return EntityIdToObjectMethod.Invoke(null, new object[] { instanceId }) as Object;
			}

			return EditorUtility.InstanceIDToObject(instanceId);
		}
	}
}
