#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using AnimatorComponent = UnityEngine.Animator;

namespace NoodleHammer.Animator.Editor
{
	[CustomEditor(typeof(UnityEngine.Animator))]
	[CanEditMultipleObjects]
	public sealed class NoodleHammerAnimatorInspector : UnityEditor.Editor
	{
		private static readonly int ActionButtonHash = "NoodleHammerAnimatorInspector.ActionButton".GetHashCode();
		private static readonly int DropdownButtonHash = "NoodleHammerAnimatorInspector.DropdownButton".GetHashCode();
		private const string PlaybackExpandedSessionKey = "NoodleHammerAnimatorInspector.EditorPlaybackExpanded";
		private const string SessionStateKeyPrefix = "NoodleHammerAnimator_";
		private const float RowHorizontalPadding = 8f;
		private const float RowVerticalPadding = 6f;
		private const float RowBackgroundHorizontalExpand = 0f;
		private const float RowHierarchyIndent = 24f;
		private const float SubsectionGap = 0f;
		private const float HeaderBottomSpacing = 3f;
		private const float ButtonRowTopSpacing = 2f;
		private const float ButtonRowHeight = 34f;
		private const float ButtonGap = 6f;
		private const float FoldoutBottomSpacing = 8f;
		private const float ActionIconButtonScale = 1.2f;

		private static Type s_animatorInspectorType;
		private static Texture2D s_tPoseIcon;

		private UnityEditor.Editor _defaultEditor;
		private AnimatorPlaybackSession _session;
		private int _inspectorAnimatorId;
		private AnimBool _editorPlaybackExpanded;
		private bool _updateRegistered;
		private GUIStyle _buttonLabelStyle;
		private GUIStyle _foldoutBodyStyle;
		private GUIStyle _dropdownLabelStyle;
		private GUIStyle _dropdownArrowStyle;
		private bool _rowGuideActive;
		private float _rowGuideBottomY;
		private float _rowGuideX;

		private void OnEnable()
		{
			s_animatorInspectorType ??= Type.GetType("UnityEditor.AnimatorInspector, UnityEditor");
			if (s_animatorInspectorType != null)
				_defaultEditor = CreateEditor(targets, s_animatorInspectorType);

			AnimatorComponent animator = target as AnimatorComponent;
			_inspectorAnimatorId = animator != null ? animator.GetInstanceID() : 0;

			_session = new AnimatorPlaybackSession(animator);

			// Restore session state
			if (_inspectorAnimatorId != 0)
			{
				string json = SessionState.GetString(SessionStateKeyPrefix + _inspectorAnimatorId, string.Empty);
				if (!string.IsNullOrEmpty(json))
				{
					var data = new AnimatorPlaybackSession.SessionData();
					EditorJsonUtility.FromJsonOverwrite(json, data);
					_session.RestoreSessionData(data);
				}
			}

			bool defaultExpanded = AnimatorEditorSettings.ExpandPlaybackByDefault;
			_editorPlaybackExpanded = CreateAnimBool(SessionState.GetBool(PlaybackExpandedSessionKey, defaultExpanded));

			EditorUpdateLoop.EnsureRegistered(ref _updateRegistered, OnEditorUpdate);
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			EditorUpdateLoop.EnsureUnregistered(ref _updateRegistered, OnEditorUpdate);
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

			// Save session state
			if (_session != null)
			{
				if (_inspectorAnimatorId != 0)
				{
					var data = _session.CaptureSessionData();
					string json = EditorJsonUtility.ToJson(data);
					SessionState.SetString(SessionStateKeyPrefix + _inspectorAnimatorId, json);
				}

				_session.Dispose();
				_session = null;
			}

			DisposeAnimBool(_editorPlaybackExpanded);

			if (_defaultEditor != null)
			{
				DestroyImmediate(_defaultEditor);
				_defaultEditor = null;
			}
		}

		public override void OnInspectorGUI()
		{
			// If custom inspector is disabled, just draw default
			if (!AnimatorEditorSettings.Enabled)
			{
				if (_defaultEditor != null)
					_defaultEditor.OnInspectorGUI();
				else
					DrawDefaultInspector();
				return;
			}

			if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
				Repaint();

			if (_defaultEditor != null)
				_defaultEditor.OnInspectorGUI();
			else
				DrawDefaultInspector();

			EditorGUILayout.Space(8f);

			if (targets.Length > 1)
			{
				EditorGUILayout.HelpBox("Multi-object editing stays in Unity's default inspector. Select one Animator to scrub clips and preview poses.", MessageType.Info);
				return;
			}

			AnimatorComponent animator = (AnimatorComponent)target;
			if (animator == null)
				return;

			bool expanded = ImprovedEditorTheme.DrawSectionHeader(
				_editorPlaybackExpanded.target,
				"Editor Playback",
				"Scrub clips, preview animation timing, and force an Avatar T-pose.",
				true);
			if (!Mathf.Approximately(_editorPlaybackExpanded.target ? 1f : 0f, expanded ? 1f : 0f))
				SessionState.SetBool(PlaybackExpandedSessionKey, expanded);

			_editorPlaybackExpanded.target = expanded;
			if (EditorGUILayout.BeginFadeGroup(_editorPlaybackExpanded.faded))
			{
				EditorGUILayout.BeginVertical(FoldoutBodyStyle);
				int nextRowIndex = DrawClipSection(animator, 0);
				EditorGUILayout.Space(SubsectionGap);
				DrawPlaybackSection(animator, nextRowIndex);
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(FoldoutBottomSpacing);
			}
			EditorGUILayout.EndFadeGroup();
		}

		private int DrawClipSection(AnimatorComponent animator, int startRowIndex)
		{
			AnimationClip[] clips = GetAnimatorClips(animator);
			using (new EditorGUILayout.VerticalScope())
			{
				BeginRowGuideSequence();
				int rowIndex = startRowIndex;

				if (clips.Length > 0)
				{
					DrawClipSelectionRow(animator, rowIndex++, clips);
				}
				else
				{
					EditorGUILayout.HelpBox("No AnimationClips were found on this Animator's GameObject hierarchy.", MessageType.Info);
				}

				EndRowGuideSequence();
				return rowIndex;
			}
		}

		private void DrawPlaybackSection(AnimatorComponent animator, int startRowIndex)
		{
			using (new EditorGUILayout.VerticalScope())
			{
				BeginRowGuideSequence();
				int rowIndex = startRowIndex;

				if (_session.ActiveClip == null)
				{
					EndRowGuideSequence();
					return;
				}

				GUIContent timeLabel = new GUIContent("Time");
				EditorGUI.BeginChangeCheck();
				float newTime = DrawStyledSliderRow(rowIndex++, timeLabel, _session.NormalizedTime, 0f, 1f);
				if (EditorGUI.EndChangeCheck())
				{
					_session.NormalizedTime = newTime;
					_session.SampleImmediate();
				}
				EndRowGuideSequence();
			}
		}

		private void OnEditorUpdate()
		{
			if (EditorTransitionGuard.IsUnsafeTransition())
				return;

			if (!_session.IsPlaying || _session.ActiveClip == null || target == null)
				return;

			double now = EditorApplication.timeSinceStartup;
			double deltaTime = now - _session.LastEditorTime;
			_session.LastEditorTime = now;

			_session.AdvanceTime(deltaTime);
			Repaint();
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.EnteredPlayMode)
				return;

			if (_session != null)
				_session.StopPreviewMode(false);
		}

		private void DrawClipSelectionRow(AnimatorComponent animator, int rowIndex, AnimationClip[] clips)
		{
			float buttonSize = Mathf.Round((EditorGUIUtility.singleLineHeight + 6f) * ActionIconButtonScale);
			float rowHeight = Mathf.Max(EditorGUIUtility.singleLineHeight, buttonSize) + RowVerticalPadding * 2f;
			Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight);
			DrawRowChrome(rowRect, rowIndex);

			const float iconGap = 4f;
			float buttonWidth = buttonSize;
			float buttonHeight = buttonSize;
			bool canTPose = animator.avatar != null && animator.avatar.isHuman;
			float buttonY = rowRect.y + (rowRect.height - buttonHeight) * 0.5f;
			float contentY = rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f;

			Rect tPoseRect = new Rect(
				rowRect.xMax - RowHorizontalPadding - buttonWidth,
				buttonY,
				buttonWidth,
				buttonHeight);
			Rect resetRect = new Rect(
				tPoseRect.x - iconGap - buttonWidth,
				buttonY,
				buttonWidth,
				buttonHeight);
			Rect buttonRect = new Rect(
				resetRect.x - iconGap - buttonWidth,
				buttonY,
				buttonWidth,
				buttonHeight);
			float contentLeft = rowRect.x + RowHorizontalPadding + RowHierarchyIndent;
			float contentWidth = buttonRect.x - contentLeft - iconGap;
			float labelWidth = Mathf.Clamp(contentWidth * 0.36f, 120f, 220f);
			Rect labelRect = new Rect(
				contentLeft,
				contentY,
				Mathf.Min(labelWidth, contentWidth),
				EditorGUIUtility.singleLineHeight);
			Rect popupRect = new Rect(
				labelRect.xMax + 10f,
				buttonY,
				Mathf.Max(40f, buttonRect.x - iconGap - (labelRect.xMax + 10f)),
				buttonHeight);

			GUIContent playContent = GetPlaybackToggleContent();
			if (DrawActionButton(buttonRect, playContent, ImprovedEditorTheme.Success, _session.ActiveClip != null))
				_session.TogglePlayback();

			if (DrawActionButton(resetRect, GetResetContent(), ImprovedEditorTheme.Warning, true))
			{
				_session.Reset();
				ImprovedEditorNotifications.Success("Animator Reset", "Rebound the Animator and cleared the preview state.", 2.5f);
			}

			if (DrawActionButton(tPoseRect, GetTPoseContent(), ImprovedEditorTheme.Error, canTPose))
			{
				if (_session.ApplyTPose())
					ImprovedEditorNotifications.Success("Avatar T-Pose Applied", "Restored local bone rotations from the source asset.", 2.7f);
			}

			EditorGUI.LabelField(labelRect, "Animator Clips");
			if (DrawClipDropdownButton(popupRect, _session.ActiveClip != null ? _session.ActiveClip.name : "<None>", "Choose an animation clip to preview."))
				ShowClipMenu(animator, clips, popupRect);
		}

		private static AnimationClip[] GetAnimatorClips(AnimatorComponent animator)
		{
			if (animator == null)
				return Array.Empty<AnimationClip>();

			AnimationClip[] clips = AnimationUtility.GetAnimationClips(animator.gameObject);
			if (clips == null || clips.Length == 0)
				return Array.Empty<AnimationClip>();

			Dictionary<int, AnimationClip> unique = new Dictionary<int, AnimationClip>();
			for (int i = 0; i < clips.Length; i++)
			{
				AnimationClip clip = clips[i];
				if (clip == null)
					continue;

				unique[clip.GetInstanceID()] = clip;
			}

			AnimationClip[] values = new AnimationClip[unique.Count];
			unique.Values.CopyTo(values, 0);
			Array.Sort(values, (left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
			return values;
		}

		private float DrawStyledSliderRow(int rowIndex, GUIContent label, float value, float min, float max)
		{
			float fieldHeight = ImprovedEditorTheme.GetStyledSliderHeight(EditorGUIUtility.currentViewWidth - 60f, label);
			Rect rowRect = EditorGUILayout.GetControlRect(false, fieldHeight + RowVerticalPadding * 2f);
			DrawRowChrome(rowRect, rowIndex);
			float fieldY = rowRect.y + (rowRect.height - fieldHeight) * 0.5f;
			Rect fieldRect = new Rect(
				rowRect.x + RowHorizontalPadding + RowHierarchyIndent,
				fieldY,
				rowRect.width - RowHorizontalPadding * 2f - RowHierarchyIndent,
				fieldHeight);
			return ImprovedEditorTheme.DrawStyledSlider(fieldRect, label, value, min, max, 2);
		}

		private void BeginRowGuideSequence()
		{
			_rowGuideActive = false;
			_rowGuideBottomY = 0f;
			_rowGuideX = 0f;
		}

		private void EndRowGuideSequence()
		{
			_rowGuideActive = false;
		}

		private void DrawRowChrome(Rect rowRect, int rowIndex)
		{
			ImprovedEditorTheme.DrawAlternatingRowBackground(rowRect, rowIndex, RowBackgroundHorizontalExpand, 0f);

			float guideX = rowRect.x + RowHorizontalPadding + 4f;
			if (_rowGuideActive && Mathf.Abs(guideX - _rowGuideX) < 0.01f && rowRect.y > _rowGuideBottomY)
			{
				EditorGUI.DrawRect(
					new Rect(guideX, _rowGuideBottomY, 1f, rowRect.y - _rowGuideBottomY),
					ImprovedEditorTheme.HierarchyGuide);
			}

			ImprovedEditorTheme.DrawHierarchyGuide(rowRect, guideX);
			_rowGuideActive = true;
			_rowGuideBottomY = rowRect.yMax;
			_rowGuideX = guideX;
		}

		private GUIContent GetPlaybackToggleContent()
		{
			GUIContent content = EditorGUIUtility.IconContent(_session.IsPlaying ? "PauseButton" : "PlayButton");
			content.text = string.Empty;
			content.tooltip = _session.IsPlaying ? "Pause preview playback." : "Play the active clip preview.";
			return content;
		}

		private GUIContent GetResetContent()
		{
			GUIContent content = EditorGUIUtility.IconContent("TreeEditor.Refresh");
			content.text = string.Empty;
			content.tooltip = "Reset Animator.";
			return content;
		}

		private GUIContent GetTPoseContent()
		{
			GUIContent content = new GUIContent(TPoseIcon);
			content.tooltip = "Force Avatar T-Pose.";
			return content;
		}

		private Texture2D TPoseIcon => s_tPoseIcon ?? (s_tPoseIcon = CreateTPoseIcon());

		private static Texture2D CreateTPoseIcon()
		{
			Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Point
			};

			Color clear = new Color(0f, 0f, 0f, 0f);
			Color white = Color.white;
			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
					texture.SetPixel(x, y, clear);
			}

			DrawFilledRect(texture, 6, 1, 4, 3, white);
			DrawFilledRect(texture, 2, 5, 12, 2, white);
			DrawFilledRect(texture, 7, 5, 2, 6, white);
			DrawFilledRect(texture, 5, 11, 2, 3, white);
			DrawFilledRect(texture, 9, 11, 2, 3, white);

			texture.Apply();
			return texture;
		}

		private static void DrawFilledRect(Texture2D texture, int x, int y, int width, int height, Color color)
		{
			for (int yy = y; yy < y + height; yy++)
			{
				for (int xx = x; xx < x + width; xx++)
					texture.SetPixel(xx, yy, color);
			}
		}

		private bool DrawActionButton(Rect rect, GUIContent content, Color accent, bool enabled)
		{
			Event evt = Event.current;
			int controlId = GUIUtility.GetControlID(ActionButtonHash, FocusType.Passive, rect);
			bool hovered = rect.Contains(evt.mousePosition);
			bool pressed = GUIUtility.hotControl == controlId && hovered;

			switch (evt.GetTypeForControl(controlId))
			{
				case EventType.MouseDown:
					if (enabled && evt.button == 0 && hovered)
					{
						GUIUtility.hotControl = controlId;
						evt.Use();
						Repaint();
					}
					break;

				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlId)
					{
						evt.Use();
						Repaint();
					}
					break;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlId)
					{
						GUIUtility.hotControl = 0;
						evt.Use();
						Repaint();
						if (enabled && hovered)
							return true;
					}
					break;
			}

			EditorGUI.DrawRect(rect, ImprovedEditorTheme.GetActionFill(accent, hovered, pressed, enabled));
			ImprovedEditorTheme.DrawOutline(rect, ImprovedEditorTheme.GetActionBorder(accent, enabled));
			if (rect.height >= 6f && rect.width >= 6f)
			{
				EditorGUI.DrawRect(
					new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f),
					ImprovedEditorTheme.GetActionTopHighlight(hovered, pressed, enabled));
				EditorGUI.DrawRect(
					new Rect(rect.x + 1f, rect.yMax - 2f, rect.width - 2f, 1f),
					ImprovedEditorTheme.GetActionBottomShadow(pressed, enabled));
			}

			Color previousColor = GUI.color;
			if (content.image != null && string.IsNullOrEmpty(content.text))
			{
				GUI.color = ImprovedEditorTheme.GetActionIconColor(enabled);
				const float iconSize = 17f;
				Rect iconRect = new Rect(
					rect.x + (rect.width - iconSize) * 0.5f,
					rect.y + (rect.height - iconSize) * 0.5f,
					iconSize,
					iconSize);
				GUI.DrawTexture(iconRect, content.image, ScaleMode.ScaleToFit, true);
			}
			else
			{
				GUI.color = ImprovedEditorTheme.GetActionTextColor(enabled);
				GUI.Label(rect, content.text, ButtonLabelStyle);
			}
			GUI.color = previousColor;
			return false;
		}

		private bool DrawClipDropdownButton(Rect rect, string text, string tooltip)
		{
			Event evt = Event.current;
			int controlId = GUIUtility.GetControlID(DropdownButtonHash, FocusType.Passive, rect);
			bool hovered = rect.Contains(evt.mousePosition);
			bool pressed = GUIUtility.hotControl == controlId && hovered;

			switch (evt.GetTypeForControl(controlId))
			{
				case EventType.MouseDown:
					if (evt.button == 0 && hovered)
					{
						GUIUtility.hotControl = controlId;
						evt.Use();
						Repaint();
					}
					break;

				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlId)
					{
						evt.Use();
						Repaint();
					}
					break;

				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlId)
					{
						GUIUtility.hotControl = 0;
						evt.Use();
						Repaint();
						if (hovered)
							return true;
					}
					break;
			}

			Color fill = !GUI.enabled
				? new Color(0.24f, 0.24f, 0.24f, 1f)
				: pressed
					? new Color(0.30f, 0.30f, 0.30f, 1f)
					: hovered
						? new Color(0.36f, 0.36f, 0.36f, 1f)
						: new Color(0.33f, 0.33f, 0.33f, 1f);
			Color border = new Color(0f, 0f, 0f, 0.42f);
			EditorGUI.DrawRect(rect, fill);
			ImprovedEditorTheme.DrawOutline(rect, border);
			EditorGUI.DrawRect(
				new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f),
				hovered ? new Color(1f, 1f, 1f, 0.07f) : new Color(1f, 1f, 1f, 0.05f));
			EditorGUI.DrawRect(
				new Rect(rect.x + 1f, rect.yMax - 2f, rect.width - 2f, 1f),
				pressed ? new Color(0f, 0f, 0f, 0.14f) : new Color(0f, 0f, 0f, 0.24f));

			Rect arrowRect = new Rect(rect.xMax - 22f, rect.y + (rect.height - 16f) * 0.5f, 14f, 16f);
			Rect textRect = new Rect(rect.x + 10f, rect.y, Mathf.Max(0f, arrowRect.x - rect.x - 14f), rect.height);

			GUI.Label(textRect, text, DropdownLabelStyle);
			GUI.Label(arrowRect, "\u25BC", DropdownArrowStyle);
			return false;
		}

		private void ShowClipMenu(AnimatorComponent animator, AnimationClip[] clips, Rect popupRect)
		{
			GUIContent[] options = new GUIContent[clips.Length + 1];
			options[0] = new GUIContent("<None>");
			int currentIndex = 0;
			for (int i = 0; i < clips.Length; i++)
			{
				options[i + 1] = new GUIContent(clips[i].name);
				if (_session.ActiveClip == clips[i])
					currentIndex = i + 1;
			}

			PopupWindow.Show(
				popupRect,
				new AnimatorClipDropdownPopup(
					popupRect.width,
					options,
					currentIndex,
					selectedIndex =>
					{
						AnimationClip selectedClip = selectedIndex <= 0 ? null : clips[selectedIndex - 1];
						_session.SetClip(selectedClip, true);
						if (selectedClip != null)
							ImprovedEditorNotifications.Info("Animator Clip Selected", "Loaded \"" + selectedClip.name + "\" into the scrubber.", 2.4f);
						Repaint();
					}));
		}

		private GUIStyle ButtonLabelStyle =>
			_buttonLabelStyle ?? (_buttonLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 10
			});

		private GUIStyle FoldoutBodyStyle =>
			_foldoutBodyStyle ?? (_foldoutBodyStyle = new GUIStyle(EditorStyles.helpBox)
			{
				padding = new RectOffset(0, 0, 0, 0),
				margin = new RectOffset(0, 0, -1, 0),
				overflow = new RectOffset(0, 0, 0, 0),
				border = new RectOffset(1, 1, 1, 1),
				normal = { background = ImprovedEditorTheme.GetSectionBodyBackgroundTexture() },
				hover = { background = ImprovedEditorTheme.GetSectionBodyBackgroundTexture() },
				active = { background = ImprovedEditorTheme.GetSectionBodyBackgroundTexture() },
				focused = { background = ImprovedEditorTheme.GetSectionBodyBackgroundTexture() }
			});

		private GUIStyle DropdownLabelStyle =>
			_dropdownLabelStyle ?? (_dropdownLabelStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleLeft,
				clipping = TextClipping.Clip,
				padding = new RectOffset(0, 0, 0, 0),
				normal = { textColor = ImprovedEditorTheme.Text }
			});

		private GUIStyle DropdownArrowStyle =>
			_dropdownArrowStyle ?? (_dropdownArrowStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 10,
				normal = { textColor = new Color(1f, 1f, 1f, 0.78f) }
			});

		private AnimBool CreateAnimBool(bool value)
		{
			AnimBool anim = new AnimBool(value);
			anim.valueChanged.AddListener(Repaint);
			return anim;
		}

		private void DisposeAnimBool(AnimBool anim)
		{
			if (anim == null)
				return;

			anim.valueChanged.RemoveListener(Repaint);
		}
	}
}
#endif
