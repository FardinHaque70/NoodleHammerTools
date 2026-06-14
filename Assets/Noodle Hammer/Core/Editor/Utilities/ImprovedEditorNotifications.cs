using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Displays transient notification messages in the currently focused editor window.
	/// </summary>
	public static class ImprovedEditorNotifications
	{
		/// <summary>
		/// Shows an informational notification.
		/// </summary>
		public static void Info(string title, string message, float duration)
		{
			Show(title, message, duration);
		}

		/// <summary>
		/// Shows a success notification.
		/// </summary>
		public static void Success(string title, string message, float duration)
		{
			Show(title, message, duration);
		}

		/// <summary>
		/// Shows a warning notification.
		/// </summary>
		public static void Warning(string title, string message, float duration)
		{
			Show(title, message, duration);
		}

		private static void Show(string title, string message, float duration)
		{
			EditorWindow window = EditorWindow.focusedWindow ?? SceneView.lastActiveSceneView;
			if (window == null)
				return;

			window.ShowNotification(new GUIContent(title + ": " + message), duration);
		}
	}
}
