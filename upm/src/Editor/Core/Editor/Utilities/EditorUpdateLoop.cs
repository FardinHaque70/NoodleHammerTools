using System.Collections.Generic;
using UnityEditor;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Safe EditorApplication.update subscription manager with assembly reload cleanup.
	/// </summary>
	public static class EditorUpdateLoop
	{
		private static readonly HashSet<EditorApplication.CallbackFunction> RegisteredCallbacks = new();

		static EditorUpdateLoop()
		{
			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
		}

		/// <summary>
		/// Idempotently subscribes the callback to EditorApplication.update.
		/// </summary>
		public static void EnsureRegistered(ref bool isRegistered, EditorApplication.CallbackFunction callback)
		{
			if (isRegistered || callback == null)
				return;

			EditorApplication.update -= callback;
			EditorApplication.update += callback;
			RegisteredCallbacks.Add(callback);
			isRegistered = true;
		}

		/// <summary>
		/// Idempotently unsubscribes the callback from EditorApplication.update.
		/// </summary>
		public static void EnsureUnregistered(ref bool isRegistered, EditorApplication.CallbackFunction callback)
		{
			if (!isRegistered || callback == null)
				return;

			EditorApplication.update -= callback;
			RegisteredCallbacks.Remove(callback);
			isRegistered = false;
		}

		private static void OnBeforeAssemblyReload()
		{
			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			if (RegisteredCallbacks.Count == 0)
				return;

			foreach (EditorApplication.CallbackFunction callback in RegisteredCallbacks)
				EditorApplication.update -= callback;

			RegisteredCallbacks.Clear();
		}
	}
}
