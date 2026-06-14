using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Helpers for locating and managing assets relative to script directories.
	/// </summary>
	public static class ScriptRelativeAssetUtility
	{
		/// <summary>
		/// Ensures all folders in the given asset path exist, creating them if necessary.
		/// </summary>
		public static void EnsureFolder(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath))
				return;

			string[] parts = assetPath.Split('/');
			if (parts.Length == 0 || parts[0] != "Assets")
				return;

			string current = "Assets";
			for (int i = 1; i < parts.Length; i++)
			{
				string next = current + "/" + parts[i];
				if (!AssetDatabase.IsValidFolder(next))
					AssetDatabase.CreateFolder(current, parts[i]);
				current = next;
			}
		}

		/// <summary>
		/// Finds the first asset of the given type anywhere under Assets/.
		/// </summary>
		public static string FindFirstAssetPathOfType<T>() where T : Object
		{
			string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" });
			if (guids == null || guids.Length == 0)
				return null;

			return AssetDatabase.GUIDToAssetPath(guids[0]);
		}

		/// <summary>
		/// Returns the fallback path as the script directory. Override for custom resolution logic.
		/// </summary>
		public static string GetScriptDirectory(string scriptFileName, string fallbackPath)
		{
			return fallbackPath;
		}

		/// <summary>
		/// Combines two asset path segments with proper separator handling.
		/// </summary>
		public static string CombineAssetPath(string left, string right)
		{
			if (string.IsNullOrEmpty(left))
				return right ?? string.Empty;

			if (string.IsNullOrEmpty(right))
				return left;

			return left.TrimEnd('/') + "/" + right.TrimStart('/');
		}
	}
}
