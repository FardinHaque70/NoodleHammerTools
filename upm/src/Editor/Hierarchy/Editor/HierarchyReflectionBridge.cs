#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Hierarchy.Editor
{
	/// <summary>
	/// Wraps all reflection-based internal Unity API access into a safe, lazily-initialized bridge.
	/// Uses cached delegates where possible to avoid per-call MethodInfo.Invoke overhead.
	/// Logs warnings once per domain load on binding failure.
	/// </summary>
	internal static class HierarchyReflectionBridge
	{
		private static readonly Type SceneHierarchyWindowType =
			typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");

		private static EditorWindow s_hierarchyWindow;
		private static PropertyInfo s_sceneHierarchyProperty;

		// Cached delegate for IsExpanded -- avoids MethodInfo.Invoke boxing per call
		private static Func<int, bool> s_isExpandedDelegate;
		private static object s_isExpandedTarget;

		private static bool s_isExpandedBindingFailed;
		private static bool s_sceneHierarchyBindingFailed;
		private static bool s_expandedWarningEmitted;
		private static bool s_hierarchyWarningEmitted;

		/// <summary>
		/// Checks whether a hierarchy item is expanded, using a cached delegate into SceneHierarchy.IsExpanded.
		/// </summary>
		public static bool IsHierarchyItemExpanded(int instanceId)
		{
			if (s_isExpandedBindingFailed)
				return false;

			object sceneHierarchy = GetSceneHierarchy();
			if (sceneHierarchy == null)
				return false;

			// Rebuild delegate if the target object changed (e.g. hierarchy window recreated)
			if (s_isExpandedDelegate == null || s_isExpandedTarget != sceneHierarchy)
			{
				MethodInfo method = sceneHierarchy.GetType().GetMethod(
					"IsExpanded",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
					null,
					new[] { typeof(int) },
					null);

				if (method == null)
				{
					s_isExpandedBindingFailed = true;
					EmitWarningOnce(ref s_expandedWarningEmitted, "Could not bind SceneHierarchy.IsExpanded");
					return false;
				}

				try
				{
					s_isExpandedDelegate = (Func<int, bool>)Delegate.CreateDelegate(typeof(Func<int, bool>), sceneHierarchy, method);
					s_isExpandedTarget = sceneHierarchy;
				}
				catch (Exception)
				{
					s_isExpandedBindingFailed = true;
					EmitWarningOnce(ref s_expandedWarningEmitted, "Could not create delegate for SceneHierarchy.IsExpanded");
					return false;
				}
			}

			try
			{
				return s_isExpandedDelegate(instanceId);
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Resolves the SceneHierarchyWindow editor window.
		/// </summary>
		public static EditorWindow ResolveHierarchyWindow()
		{
			if (s_hierarchyWindow != null)
				return s_hierarchyWindow;

			if (SceneHierarchyWindowType == null)
				return null;

			EditorWindow focusedWindow = EditorWindow.focusedWindow;
			if (focusedWindow != null && SceneHierarchyWindowType.IsInstanceOfType(focusedWindow))
			{
				s_hierarchyWindow = focusedWindow;
				return s_hierarchyWindow;
			}

			UnityEngine.Object[] hierarchyWindows = Resources.FindObjectsOfTypeAll(SceneHierarchyWindowType);
			if (hierarchyWindows != null && hierarchyWindows.Length > 0)
				s_hierarchyWindow = hierarchyWindows[0] as EditorWindow;

			return s_hierarchyWindow;
		}

		/// <summary>
		/// Clears cached window reference and delegate so they get re-resolved.
		/// </summary>
		public static void InvalidateWindow()
		{
			s_hierarchyWindow = null;
			s_isExpandedDelegate = null;
			s_isExpandedTarget = null;
		}

		private static object GetSceneHierarchy()
		{
			if (s_sceneHierarchyBindingFailed)
				return null;

			EditorWindow hierarchyWindow = ResolveHierarchyWindow();
			if (hierarchyWindow == null)
				return null;

			if (s_sceneHierarchyProperty == null)
			{
				s_sceneHierarchyProperty = hierarchyWindow.GetType().GetProperty(
					"sceneHierarchy",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				if (s_sceneHierarchyProperty == null)
				{
					s_sceneHierarchyBindingFailed = true;
					EmitWarningOnce(ref s_hierarchyWarningEmitted, "Could not bind SceneHierarchyWindow.sceneHierarchy");
					return null;
				}
			}

			try
			{
				return s_sceneHierarchyProperty.GetValue(hierarchyWindow, null);
			}
			catch (TargetInvocationException)
			{
				return null;
			}
		}

		private static void EmitWarningOnce(ref bool flag, string message)
		{
			if (flag)
				return;

			flag = true;
			NoodleHammer.Core.Editor.NoodleHammerDiagnostics.Warn("Hierarchy", message);
		}
	}
}
#endif
