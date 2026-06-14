using System.IO;
using UnityEditor;
using UnityObject = UnityEngine.Object;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Resolves editor asset locations across Asset Store imports and Git UPM installs.
	/// Each tool creates its own static instance with its specific folder and package names.
	/// </summary>
	public sealed class ToolInstallLayout
	{
		private readonly string[] _installRoots;

		/// <summary>
		/// Creates a new install layout for a tool with the given folder and package names.
		/// </summary>
		/// <param name="toolFolderName">The folder name under "Noodle Hammer/", e.g. "Preview Forge".</param>
		/// <param name="packageName">The UPM package name, e.g. "com.noodlehammer.preview-forge".</param>
		public ToolInstallLayout(string toolFolderName, string packageName)
		{
			string assetsRoot = "Assets/Noodle Hammer/" + toolFolderName;
			string packageRoot = "Packages/" + packageName + "/Noodle Hammer/" + toolFolderName;

			_installRoots = new[]
			{
				assetsRoot,
				packageRoot,
			};
		}

		/// <summary>
		/// Builds candidate asset paths from all known install roots.
		/// </summary>
		public string[] BuildAssetPaths(string relativePathFromToolRoot)
		{
			string normalizedRelativePath = NormalizeRelativePath(relativePathFromToolRoot);
			string[] paths = new string[_installRoots.Length];
			for (int i = 0; i < _installRoots.Length; i++)
				paths[i] = _installRoots[i] + "/" + normalizedRelativePath;
			return paths;
		}

		/// <summary>
		/// Loads the first asset found at the relative path from any install root.
		/// </summary>
		public T LoadFirstAssetAtRelativePath<T>(string relativePathFromToolRoot) where T : UnityObject
		{
			string[] candidatePaths = BuildAssetPaths(relativePathFromToolRoot);
			for (int i = 0; i < candidatePaths.Length; i++)
			{
				T asset = AssetDatabase.LoadAssetAtPath<T>(candidatePaths[i]);
				if (asset != null)
					return asset;
			}

			return null;
		}

		/// <summary>
		/// Resolves the first existing absolute filesystem path for the given relative path.
		/// </summary>
		public string TryResolveExistingAbsolutePath(string relativePathFromToolRoot)
		{
			string[] candidatePaths = BuildAssetPaths(relativePathFromToolRoot);
			for (int i = 0; i < candidatePaths.Length; i++)
			{
				string fullPath = Path.GetFullPath(candidatePaths[i]);
				if (File.Exists(fullPath))
					return fullPath;
			}

			return null;
		}

		private static string NormalizeRelativePath(string relativePathFromToolRoot)
		{
			if (string.IsNullOrEmpty(relativePathFromToolRoot))
				return string.Empty;

			return relativePathFromToolRoot.TrimStart('/');
		}
	}
}
