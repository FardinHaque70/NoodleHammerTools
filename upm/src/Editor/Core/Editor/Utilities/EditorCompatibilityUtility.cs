using System;
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
		private static readonly Type EntityIdType =
			typeof(UnityEngine.Object).Assembly.GetType("UnityEngine.EntityId");
		private static readonly MethodInfo IntToEntityIdImplicitMethod =
			EntityIdType?.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);

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
		public static UnityEngine.Object InstanceIDToObject(int instanceId)
		{
			if (EntityIdToObjectMethod != null)
			{
				ParameterInfo[] parameters = EntityIdToObjectMethod.GetParameters();
				if (parameters.Length == 1)
				{
					Type parameterType = parameters[0].ParameterType;
					if (parameterType == typeof(int))
							return EntityIdToObjectMethod.Invoke(null, new object[] { instanceId }) as UnityEngine.Object;

					if (EntityIdType != null &&
					    parameterType == EntityIdType &&
					    IntToEntityIdImplicitMethod != null)
					{
						object entityId = IntToEntityIdImplicitMethod.Invoke(null, new object[] { instanceId });
						return EntityIdToObjectMethod.Invoke(null, new[] { entityId }) as UnityEngine.Object;
					}
				}
			}

			return EditorUtility.InstanceIDToObject(instanceId);
		}
	}
}
