#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AnimatorComponent = UnityEngine.Animator;
using TransformComponent = UnityEngine.Transform;

namespace NoodleHammer.Animator.Editor
{
	/// <summary>
	/// Encapsulates AnimationMode enter/exit, clip sampling, T-Pose logic, and playback state.
	/// Extracted from NoodleHammerAnimatorInspector for single-responsibility.
	/// </summary>
	internal sealed class AnimatorPlaybackSession : IDisposable
	{
		private static int s_previewAnimatorId;

		private readonly AnimatorComponent _animator;
		private readonly int _animatorInstanceId;
		private AnimationClip _activeClip;
		private float _normalizedTime;
		private float _playbackSpeed;
		private bool _isPlaying;
		private double _lastEditorTime;

		/// <summary>
		/// Creates a new playback session for the given Animator.
		/// </summary>
		public AnimatorPlaybackSession(AnimatorComponent animator)
		{
			_animator = animator;
			_animatorInstanceId = animator != null ? animator.GetInstanceID() : 0;
			_playbackSpeed = AnimatorEditorSettings.DefaultPlaybackSpeed;
		}

		public AnimationClip ActiveClip => _activeClip;
		public float NormalizedTime
		{
			get => _normalizedTime;
			set => _normalizedTime = Mathf.Clamp01(value);
		}
		public float PlaybackSpeed
		{
			get => _playbackSpeed;
			set => _playbackSpeed = Mathf.Max(0.01f, value);
		}
		public bool IsPlaying => _isPlaying;

		/// <summary>
		/// Sets the active clip. Stops and restarts preview mode as needed.
		/// </summary>
		public void SetClip(AnimationClip clip, bool resetTime)
		{
			if (_activeClip == clip)
				return;

			_activeClip = clip;
			_isPlaying = clip != null;
			if (resetTime)
				_normalizedTime = 0f;

			StopPreviewMode(true);
			NoodleHammerDiagnostics.Log("Animator", $"SetClip: {(clip != null ? clip.name : "null")}");

			if (_activeClip != null)
			{
				_lastEditorTime = EditorApplication.timeSinceStartup;
				SampleImmediate();
			}
			else
			{
				Reset();
			}
		}

		/// <summary>
		/// Samples the active clip at the current normalized time.
		/// </summary>
		public void SampleImmediate()
		{
			if (_animator == null || _activeClip == null)
				return;

			EnsurePreviewMode();
			float sampleTime = Mathf.Clamp01(_normalizedTime) * _activeClip.length;
			AnimationMode.BeginSampling();
			AnimationMode.SampleAnimationClip(_animator.gameObject, _activeClip, sampleTime);
			AnimationMode.EndSampling();
			EditorCompatibilityUtility.RepaintAllViews();
		}

		/// <summary>
		/// Toggles playback on/off.
		/// </summary>
		public void TogglePlayback()
		{
			_isPlaying = !_isPlaying;
			_lastEditorTime = EditorApplication.timeSinceStartup;
			if (_isPlaying)
				SampleImmediate();

			NoodleHammerDiagnostics.Log("Animator", $"TogglePlayback: {(_isPlaying ? "Playing" : "Paused")}");
		}

		/// <summary>
		/// Resets the animator state and stops preview mode.
		/// </summary>
		public void Reset()
		{
			_isPlaying = false;
			_normalizedTime = 0f;
			StopPreviewMode(true);
		}

		/// <summary>
		/// Applies the Avatar T-Pose by copying local rotations from the source prefab/model.
		/// </summary>
		public bool ApplyTPose()
		{
			if (_animator == null)
				return false;

			StopPreviewMode(true);

			TransformComponent root = _animator.transform;
			TransformComponent sourceRoot = PrefabUtility.GetCorrespondingObjectFromSource(root);
			if (sourceRoot == null)
			{
				ImprovedEditorNotifications.Warning("Avatar T-Pose Unavailable",
					"No source prefab or model root was found for this Animator.", 3f);
				return false;
			}

			Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Force Avatar T-Pose");
			Dictionary<string, TransformComponent> sourceMap = new Dictionary<string, TransformComponent>();
			BuildPathMap(sourceRoot, string.Empty, sourceMap);
			CopyLocalRotationsFromSource(root, string.Empty, sourceMap);
			EditorUtility.SetDirty(root.gameObject);
			EditorCompatibilityUtility.RepaintAllViews();

			NoodleHammerDiagnostics.Log("Animator", "Applied Avatar T-Pose");
			return true;
		}

		/// <summary>
		/// Advances playback time by the given delta. Called from the editor update loop.
		/// </summary>
		public void AdvanceTime(double deltaTime)
		{
			if (!_isPlaying || _activeClip == null || _animator == null)
				return;

			float clipLength = Mathf.Max(_activeClip.length, 0.0001f);
			_normalizedTime += (float)(deltaTime * _playbackSpeed / clipLength);
			if (_normalizedTime > 1f)
				_normalizedTime -= Mathf.Floor(_normalizedTime);

			SampleImmediate();
		}

		/// <summary>
		/// Wraps AnimationMode.StartAnimationMode() with ownership tracking.
		/// </summary>
		public void EnsurePreviewMode()
		{
			if (_animator == null)
				return;

			if (s_previewAnimatorId != 0 && s_previewAnimatorId != _animatorInstanceId && AnimationMode.InAnimationMode())
			{
				NoodleHammerDiagnostics.Log("Animator", "Stopping preview mode for previous animator");
				ForceStopPreviewMode();
			}

			if (!AnimationMode.InAnimationMode())
			{
				AnimationMode.StartAnimationMode();
				NoodleHammerDiagnostics.Log("Animator", "Started AnimationMode");
			}

			s_previewAnimatorId = _animatorInstanceId;
		}

		/// <summary>
		/// Wraps AnimationMode.StopAnimationMode() with cleanup.
		/// </summary>
		public void StopPreviewMode(bool rebindAfterStop)
		{
			bool ownsPreview = _animator != null && s_previewAnimatorId == _animatorInstanceId;
			bool stopGlobalPreview = AnimationMode.InAnimationMode() && (ownsPreview || _animator == null);

			if (stopGlobalPreview)
			{
				AnimationMode.StopAnimationMode();
				s_previewAnimatorId = 0;
				NoodleHammerDiagnostics.Log("Animator", "Stopped AnimationMode");
			}

			if (rebindAfterStop && _animator != null)
			{
				_animator.Rebind();
				_animator.Update(0f);
			}

			EditorCompatibilityUtility.RepaintAllViews();
		}

		/// <summary>
		/// Cleanup: stops preview mode.
		/// </summary>
		public void Dispose()
		{
			StopPreviewMode(true);
		}

		/// <summary>
		/// Serializes session state for SessionState persistence.
		/// </summary>
		public SessionData CaptureSessionData()
		{
			return new SessionData
			{
				clipName = _activeClip != null ? _activeClip.name : null,
				clipAssetPath = _activeClip != null ? AssetDatabase.GetAssetPath(_activeClip) : null,
				normalizedTime = _normalizedTime,
				playbackSpeed = _playbackSpeed
			};
		}

		/// <summary>
		/// Restores session state from persisted data.
		/// </summary>
		public void RestoreSessionData(SessionData data)
		{
			if (data == null)
				return;

			_normalizedTime = Mathf.Clamp01(data.normalizedTime);
			_playbackSpeed = data.playbackSpeed > 0f ? data.playbackSpeed : AnimatorEditorSettings.DefaultPlaybackSpeed;
			_isPlaying = false;

			if (!string.IsNullOrEmpty(data.clipAssetPath))
			{
				AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(data.clipAssetPath);
				if (clip != null)
				{
					_activeClip = clip;
					return;
				}
			}

			_activeClip = null;
			_normalizedTime = 0f;
		}

		/// <summary>
		/// Gets the current editor time for delta calculation.
		/// </summary>
		public double LastEditorTime
		{
			get => _lastEditorTime;
			set => _lastEditorTime = value;
		}

		private static void ForceStopPreviewMode()
		{
			if (AnimationMode.InAnimationMode())
				AnimationMode.StopAnimationMode();
			s_previewAnimatorId = 0;
		}

		private static void BuildPathMap(TransformComponent source, string parentPath, Dictionary<string, TransformComponent> map)
		{
			string path = string.IsNullOrEmpty(parentPath) ? source.name : parentPath + "/" + source.name;
			map[path] = source;

			for (int i = 0; i < source.childCount; i++)
				BuildPathMap(source.GetChild(i), path, map);
		}

		private static void CopyLocalRotationsFromSource(TransformComponent destination, string parentPath, Dictionary<string, TransformComponent> sourceMap)
		{
			string path = string.IsNullOrEmpty(parentPath) ? destination.name : parentPath + "/" + destination.name;
			if (sourceMap.TryGetValue(path, out TransformComponent source))
				destination.localRotation = source.localRotation;

			for (int i = 0; i < destination.childCount; i++)
				CopyLocalRotationsFromSource(destination.GetChild(i), path, sourceMap);
		}

		/// <summary>
		/// Serializable session data for SessionState persistence.
		/// </summary>
		[Serializable]
		internal sealed class SessionData
		{
			public string clipName;
			public string clipAssetPath;
			public float normalizedTime;
			public float playbackSpeed;
		}
	}
}
#endif
