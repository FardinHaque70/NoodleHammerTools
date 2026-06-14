using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace NoodleHammer.Core.Editor
{
	/// <summary>
	/// Loads project-owned settings assets from AssetDatabase paths or serialized files
	/// under ProjectSettings/. Handles deferred persistence when the editor is compiling
	/// or updating, and can migrate legacy asset-backed settings into ProjectSettings.
	/// </summary>
	public static class ProjectSettingsAssetUtility
	{
		private const int MaxPersistAttempts = 8;

		private sealed class PendingSettingsAsset
		{
			internal string Key;
			internal string AssetPath;
			internal ScriptableObject Asset;
			internal int Attempts;
		}

		private static readonly Dictionary<string, PendingSettingsAsset> PendingAssetsByKey = new Dictionary<string, PendingSettingsAsset>();
		private static readonly Dictionary<int, PendingSettingsAsset> PendingAssetsByInstanceId = new Dictionary<int, PendingSettingsAsset>();
		private static readonly Dictionary<int, string> ManagedPathsByInstanceId = new Dictionary<int, string>();
		private static bool s_persistScheduled;

		/// <summary>
		/// Loads a settings asset from disk, or creates a new in-memory instance and schedules it for persistence.
		/// </summary>
		public static T LoadOrCreate<T>(string assetPath, Action<T> initialize, string legacyAssetPath = null) where T : ScriptableObject
		{
			T asset = LoadPersistedAsset<T>(assetPath);
			if (asset != null)
			{
				RegisterManagedAsset(asset, assetPath);
				return asset;
			}

			string key = MakeKey<T>(assetPath);
			if (PendingAssetsByKey.TryGetValue(key, out PendingSettingsAsset pendingAsset) && pendingAsset.Asset != null)
				return (T)pendingAsset.Asset;

			asset = ScriptableObject.CreateInstance<T>();
			asset.hideFlags = HideFlags.HideAndDontSave;
			initialize?.Invoke(asset);
			TryMigrateLegacyAsset(asset, legacyAssetPath);

			pendingAsset = new PendingSettingsAsset
			{
				Key = key,
				AssetPath = assetPath,
				Asset = asset,
			};
			PendingAssetsByKey[key] = pendingAsset;
			PendingAssetsByInstanceId[asset.GetInstanceID()] = pendingAsset;
			RegisterManagedAsset(asset, assetPath);
			SchedulePersistPendingAssets();

			return asset;
		}

		/// <summary>
		/// Saves the given asset to disk. If the asset is still pending persistence, attempts to persist it first.
		/// </summary>
		public static void Save(UnityObject asset)
		{
			if (asset == null)
				return;

			if (!EditorUtility.IsPersistent(asset) &&
			    PendingAssetsByInstanceId.TryGetValue(asset.GetInstanceID(), out PendingSettingsAsset pendingAsset))
			{
				if (!TryPersistPendingAsset(pendingAsset))
					SchedulePersistPendingAssets();

				return;
			}

			if (ManagedPathsByInstanceId.TryGetValue(asset.GetInstanceID(), out string managedPath) &&
			    IsProjectSettingsPath(managedPath))
			{
				SaveProjectSettingsAsset(asset, managedPath);
				return;
			}

			EditorUtility.SetDirty(asset);
			AssetDatabase.SaveAssets();
		}

		/// <summary>
		/// Reads a boolean value from a legacy key-value settings file.
		/// </summary>
		public static bool TryReadBool(string legacyPath, string key, out bool value)
		{
			value = false;
			if (!TryReadValue(legacyPath, key, out string raw))
				return false;

			if (raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
			{
				value = true;
				return true;
			}

			if (raw == "0" || string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
			{
				value = false;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Reads an integer value from a legacy key-value settings file.
		/// </summary>
		public static bool TryReadInt(string legacyPath, string key, out int value)
		{
			value = 0;
			return TryReadValue(legacyPath, key, out string raw)
			       && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
		}

		/// <summary>
		/// Reads a float value from a legacy key-value settings file.
		/// </summary>
		public static bool TryReadFloat(string legacyPath, string key, out float value)
		{
			value = 0f;
			return TryReadValue(legacyPath, key, out string raw)
			       && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		/// <summary>
		/// Reads a Color value from a legacy key-value settings file.
		/// </summary>
		public static bool TryReadColor(string legacyPath, string key, out Color value)
		{
			value = default;
			if (!TryReadValue(legacyPath, key, out string raw))
				return false;

			return TryReadNamedFloat(raw, "r", out value.r)
			       && TryReadNamedFloat(raw, "g", out value.g)
			       && TryReadNamedFloat(raw, "b", out value.b)
			       && TryReadNamedFloat(raw, "a", out value.a);
		}

		/// <summary>
		/// Reads a Vector2 value from a legacy key-value settings file.
		/// </summary>
		public static bool TryReadVector2(string legacyPath, string key, out Vector2 value)
		{
			value = default;
			if (!TryReadValue(legacyPath, key, out string raw))
				return false;

			return TryReadNamedFloat(raw, "x", out value.x)
			       && TryReadNamedFloat(raw, "y", out value.y);
		}

		/// <summary>
		/// Reads a UnityEngine.Object reference from a legacy key-value settings file via GUID.
		/// </summary>
		public static bool TryReadObject<T>(string legacyPath, string key, out T value) where T : UnityObject
		{
			value = null;
			if (!TryReadValue(legacyPath, key, out string raw))
				return false;

			if (!TryReadNamedString(raw, "guid", out string guid) || string.IsNullOrEmpty(guid))
				return false;

			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(assetPath))
				return false;

			value = AssetDatabase.LoadAssetAtPath<T>(assetPath);
			return value != null;
		}

		private static void EnsureAssetFolder(string folderPath)
		{
			if (string.IsNullOrEmpty(folderPath))
				return;

			folderPath = folderPath.Replace('\\', '/');
			if (AssetDatabase.IsValidFolder(folderPath))
				return;

			if (Directory.Exists(folderPath))
			{
				AssetDatabase.ImportAsset(folderPath);
				if (AssetDatabase.IsValidFolder(folderPath))
					return;
			}

			string[] parts = folderPath.Split('/');
			string current = parts[0];
			for (int i = 1; i < parts.Length; i++)
			{
				string next = current + "/" + parts[i];
				if (AssetDatabase.IsValidFolder(next))
				{
					current = next;
					continue;
				}

				if (Directory.Exists(next))
				{
					AssetDatabase.ImportAsset(next);
					current = next;
					continue;
				}

				string createdGuid = AssetDatabase.CreateFolder(current, parts[i]);
				string createdPath = AssetDatabase.GUIDToAssetPath(createdGuid);
				if (!string.IsNullOrEmpty(createdPath))
					next = createdPath;

				current = next;
			}
		}

		private static string MakeKey<T>(string assetPath) where T : ScriptableObject
		{
			return typeof(T).FullName + "|" + assetPath;
		}

		private static bool IsProjectSettingsPath(string assetPath)
		{
			return !string.IsNullOrEmpty(assetPath) &&
			       assetPath.Replace('\\', '/').StartsWith("ProjectSettings/", StringComparison.Ordinal);
		}

		private static T LoadPersistedAsset<T>(string assetPath) where T : ScriptableObject
		{
			if (IsProjectSettingsPath(assetPath))
			{
				UnityObject[] objects = InternalEditorUtility.LoadSerializedFileAndForget(assetPath);
				for (int i = 0; i < objects.Length; i++)
				{
					if (objects[i] is T loadedAsset)
						return loadedAsset;
				}

				return null;
			}

			return AssetDatabase.LoadAssetAtPath<T>(assetPath);
		}

		private static void TryMigrateLegacyAsset<T>(T target, string legacyAssetPath) where T : ScriptableObject
		{
			if (target == null || string.IsNullOrEmpty(legacyAssetPath))
				return;

			T legacyAsset = AssetDatabase.LoadAssetAtPath<T>(legacyAssetPath);
			if (legacyAsset == null)
				return;

			EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(legacyAsset), target);
		}

		private static void RegisterManagedAsset(UnityObject asset, string assetPath)
		{
			if (asset == null || string.IsNullOrEmpty(assetPath))
				return;

			ManagedPathsByInstanceId[asset.GetInstanceID()] = assetPath;
		}

		private static void SaveProjectSettingsAsset(UnityObject asset, string assetPath)
		{
			string directoryPath = Path.GetDirectoryName(assetPath);
			if (!string.IsNullOrEmpty(directoryPath))
				Directory.CreateDirectory(directoryPath);

			InternalEditorUtility.SaveToSerializedFileAndForget(new[] { asset }, assetPath, true);
		}

		private static void SchedulePersistPendingAssets()
		{
			if (s_persistScheduled)
				return;

			s_persistScheduled = true;
			EditorApplication.delayCall += PersistPendingAssets;
		}

		private static void PersistPendingAssets()
		{
			s_persistScheduled = false;
			if (PendingAssetsByKey.Count == 0)
				return;

			PendingSettingsAsset[] pendingAssets = new PendingSettingsAsset[PendingAssetsByKey.Count];
			PendingAssetsByKey.Values.CopyTo(pendingAssets, 0);
			foreach (PendingSettingsAsset pendingAsset in pendingAssets)
				TryPersistPendingAsset(pendingAsset);
		}

		private static bool TryPersistPendingAsset(PendingSettingsAsset pendingAsset)
		{
			if (pendingAsset == null)
				return true;

			if (pendingAsset.Asset == null)
			{
				RemovePendingAsset(pendingAsset);
				return true;
			}

			if (EditorApplication.isCompiling || EditorApplication.isUpdating)
			{
				SchedulePersistPendingAssets();
				return false;
			}

			try
			{
				if (IsProjectSettingsPath(pendingAsset.AssetPath))
				{
					pendingAsset.Asset.hideFlags = HideFlags.None;
					SaveProjectSettingsAsset(pendingAsset.Asset, pendingAsset.AssetPath);
				}
				else
				{
					EnsureAssetFolder(Path.GetDirectoryName(pendingAsset.AssetPath)?.Replace('\\', '/'));
					if (File.Exists(pendingAsset.AssetPath))
						AssetDatabase.DeleteAsset(pendingAsset.AssetPath);

					pendingAsset.Asset.hideFlags = HideFlags.None;
					AssetDatabase.CreateAsset(pendingAsset.Asset, pendingAsset.AssetPath);
					EditorUtility.SetDirty(pendingAsset.Asset);
					AssetDatabase.SaveAssets();
				}

				RemovePendingAsset(pendingAsset);
				return true;
			}
			catch (Exception exception)
			{
				pendingAsset.Asset.hideFlags = HideFlags.HideAndDontSave;
				pendingAsset.Attempts++;
				if (pendingAsset.Attempts < MaxPersistAttempts)
				{
					SchedulePersistPendingAssets();
				}
				else
				{
					Debug.LogWarning(
						$"Could not create settings asset at '{pendingAsset.AssetPath}'. " +
						$"Settings will use in-memory defaults until Unity can create it. Last error: {exception.Message}");
				}

				return false;
			}
		}

		private static void RemovePendingAsset(PendingSettingsAsset pendingAsset)
		{
			PendingAssetsByKey.Remove(pendingAsset.Key);
			if (pendingAsset.Asset != null)
				PendingAssetsByInstanceId.Remove(pendingAsset.Asset.GetInstanceID());
		}

		private static bool TryReadValue(string legacyPath, string key, out string value)
		{
			value = null;
			if (!File.Exists(legacyPath))
				return false;

			string prefix = key + ":";
			string[] lines = File.ReadAllLines(legacyPath);
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i].Trim();
				if (!line.StartsWith(prefix, StringComparison.Ordinal))
					continue;

				value = line.Substring(prefix.Length).Trim();
				return true;
			}

			return false;
		}

		private static bool TryReadNamedFloat(string raw, string name, out float value)
		{
			value = 0f;
			return TryReadNamedString(raw, name, out string rawValue)
			       && float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}

		private static bool TryReadNamedString(string raw, string name, out string value)
		{
			value = null;
			string token = name + ":";
			int tokenIndex = raw.IndexOf(token, StringComparison.Ordinal);
			if (tokenIndex < 0)
				return false;

			int valueStart = tokenIndex + token.Length;
			while (valueStart < raw.Length && char.IsWhiteSpace(raw[valueStart]))
				valueStart++;

			int valueEnd = raw.IndexOf(',', valueStart);
			if (valueEnd < 0)
			{
				valueEnd = raw.IndexOf('}', valueStart);
				if (valueEnd < 0)
					valueEnd = raw.Length;
			}

			value = raw.Substring(valueStart, valueEnd - valueStart).Trim();
			return true;
		}
	}
}
