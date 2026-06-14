using System;
using System.Collections.Generic;
using UnityEditor;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Generic bootstrap that triggers settings asset creation on editor startup.
	/// Tools register their settings loading via <see cref="Register"/>.
	/// </summary>
	[InitializeOnLoad]
	public static class SettingsBootstrap
	{
		private static readonly List<Action> BootstrapActions = new();
		private static bool s_scheduled;

		static SettingsBootstrap()
		{
			if (!s_scheduled && BootstrapActions.Count > 0)
				ScheduleBootstrap();
		}

		/// <summary>
		/// Registers a bootstrap action to be invoked on the next editor delay call.
		/// Tools call this from their own [InitializeOnLoad] to register settings loading.
		/// </summary>
		public static void Register(Action bootstrapAction)
		{
			if (bootstrapAction == null)
				return;

			BootstrapActions.Add(bootstrapAction);
			ScheduleBootstrap();
		}

		private static void ScheduleBootstrap()
		{
			if (s_scheduled)
				return;

			s_scheduled = true;
			EditorApplication.delayCall += ExecuteBootstrap;
		}

		private static void ExecuteBootstrap()
		{
			EditorApplication.delayCall -= ExecuteBootstrap;
			s_scheduled = false;

			for (int i = 0; i < BootstrapActions.Count; i++)
			{
				try
				{
					BootstrapActions[i]?.Invoke();
				}
				catch (Exception exception)
				{
					UnityEngine.Debug.LogWarning(
						$"[NoodleHammer] Settings bootstrap action failed: {exception.Message}");
				}
			}

			BootstrapActions.Clear();
		}
	}
}
