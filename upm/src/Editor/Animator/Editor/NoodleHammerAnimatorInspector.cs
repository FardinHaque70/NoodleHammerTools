#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using AnimatorComponent = UnityEngine.Animator;
using TransformComponent = UnityEngine.Transform;

namespace NoodleHammer.Animator.Editor
{
    [CustomEditor(typeof(UnityEngine.Animator))]
    [CanEditMultipleObjects]
    public sealed class NoodleHammerAnimatorInspector : UnityEditor.Editor
    {
        private static readonly int ActionButtonHash = "NoodleHammerAnimatorInspector.ActionButton".GetHashCode();
        private static readonly int DropdownButtonHash = "NoodleHammerAnimatorInspector.DropdownButton".GetHashCode();
        private const string PlaybackExpandedSessionKey = "NoodleHammerAnimatorInspector.EditorPlaybackExpanded";
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

        [Serializable]
        private sealed class ScrubState
        {
            public AnimationClip Clip;
            public float NormalizedTime;
            public float PreviewSpeed = 1f;
        }

        private sealed class ClipDropdownPopup : PopupWindowContent
        {
        private const float RowHeight = 28f;
        private const float PopupMaxHeight = 240f;

        private readonly GUIContent[] _options;
        private readonly int _selectedIndex;
        private readonly Action<int> _onSelected;
        private readonly float _popupWidth;
        private Vector2 _scrollPosition;
        private GUIStyle _rowLabelStyle;
        private GUIStyle _checkStyle;

        public ClipDropdownPopup(float popupWidth, GUIContent[] options, int selectedIndex, Action<int> onSelected)
        {
            _popupWidth = Mathf.Max(180f, popupWidth);
            _options = options ?? Array.Empty<GUIContent>();
            _selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, _options.Length - 1));
            _onSelected = onSelected;
        }

        public override Vector2 GetWindowSize()
        {
            float height = Mathf.Min(PopupMaxHeight, _options.Length * RowHeight + 8f);
            return new Vector2(_popupWidth, height);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, ImprovedEditorTheme.Surface);
            ImprovedEditorTheme.DrawOutline(rect, ImprovedEditorTheme.BorderStrong);

            Rect listRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            float contentHeight = _options.Length * RowHeight;
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, listRect.width), contentHeight);
            _scrollPosition = GUI.BeginScrollView(listRect, _scrollPosition, viewRect, false, contentHeight > listRect.height);

            for (int i = 0; i < _options.Length; i++)
            {
                Rect rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                bool hovered = rowRect.Contains(Event.current.mousePosition);
                bool selected = i == _selectedIndex;

                Color baseRowColor = (i & 1) == 0 ? ImprovedEditorTheme.RowSurfaceA : ImprovedEditorTheme.RowSurfaceB;
                EditorGUI.DrawRect(rowRect, baseRowColor);

                if (selected)
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.08f));
                else if (hovered)
                    EditorGUI.DrawRect(rowRect, new Color(1f, 1f, 1f, 0.035f));

                if (selected)
                    EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 3f, rowRect.height), ImprovedEditorTheme.Accent);

                Rect checkRect = new Rect(rowRect.x + 8f, rowRect.y, 18f, rowRect.height);
                Rect labelRect = new Rect(rowRect.x + 28f, rowRect.y, rowRect.width - 36f, rowRect.height);

                if (selected)
                    GUI.Label(checkRect, "✓", CheckStyle);

                Color previousColor = GUI.color;
                GUI.color = hovered ? ImprovedEditorTheme.AccentBright : ImprovedEditorTheme.Text;
                GUI.Label(labelRect, _options[i], RowLabelStyle);
                GUI.color = previousColor;

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rowRect.Contains(Event.current.mousePosition))
                {
                    _onSelected?.Invoke(i);
                    editorWindow.Close();
                    Event.current.Use();
                }
            }

            GUI.EndScrollView();
        }

        private GUIStyle RowLabelStyle =>
            _rowLabelStyle ?? (_rowLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = ImprovedEditorTheme.Text }
            });

        private GUIStyle CheckStyle =>
            _checkStyle ?? (_checkStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = ImprovedEditorTheme.Text }
            });
        }

        private static readonly Dictionary<int, ScrubState> StatePerAnimator = new Dictionary<int, ScrubState>();
        private static Type s_animatorInspectorType;
        private static Texture2D s_tPoseIcon;
        private static int s_previewAnimatorId;

        private UnityEditor.Editor _defaultEditor;
        private AnimationClip _clip;
        private float _normalizedTime;
        private float _previewSpeed = 1f;
        private bool _isPlaying;
        private bool _stateLoaded;
        private int _inspectorAnimatorId;
        private double _lastEditorTime;
        private AnimBool _editorPlaybackExpanded;
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

        _inspectorAnimatorId = target is AnimatorComponent animator ? animator.GetInstanceID() : 0;

        _editorPlaybackExpanded = CreateAnimBool(SessionState.GetBool(PlaybackExpandedSessionKey, true));

        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        if (target is AnimatorComponent animator)
        {
            StopPreviewMode(animator, true);
            SaveState(animator);
        }
        else if (_inspectorAnimatorId != 0 && s_previewAnimatorId == _inspectorAnimatorId)
        {
            StopPreviewMode(null, false);
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

        if (!_stateLoaded)
            LoadState(animator);

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

        SaveState(animator);
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

            if (_clip == null)
            {
                EndRowGuideSequence();
                return;
            }

            GUIContent timeLabel = new GUIContent("Time");
            EditorGUI.BeginChangeCheck();
            float newTime = DrawStyledSliderRow(rowIndex++, timeLabel, _normalizedTime, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                _isPlaying = false;
                _normalizedTime = newTime;
                SampleImmediate(animator);
            }
            EndRowGuideSequence();
        }
        }

        private void OnEditorUpdate()
        {
        if (!_isPlaying || _clip == null || target == null)
            return;

        AnimatorComponent animator = target as AnimatorComponent;
        if (animator == null)
            return;

        double now = EditorApplication.timeSinceStartup;
        double deltaTime = now - _lastEditorTime;
        _lastEditorTime = now;

        float clipLength = Mathf.Max(_clip.length, 0.0001f);
        _normalizedTime += (float)(deltaTime * _previewSpeed / clipLength);
        if (_normalizedTime > 1f)
            _normalizedTime -= Mathf.Floor(_normalizedTime);

        SampleImmediate(animator);
        Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
        if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.EnteredPlayMode)
            return;

        if (_inspectorAnimatorId != 0 && s_previewAnimatorId == _inspectorAnimatorId)
            StopPreviewMode(target as AnimatorComponent, false);
        }

        private void LoadState(AnimatorComponent animator)
        {
        if (animator == null)
            return;

        if (StatePerAnimator.TryGetValue(animator.GetInstanceID(), out ScrubState state))
        {
            _clip = state.Clip;
            _normalizedTime = Mathf.Clamp01(state.NormalizedTime);
            _previewSpeed = state.PreviewSpeed <= 0f ? 1f : state.PreviewSpeed;
        }
        else
        {
            _clip = null;
            _normalizedTime = 0f;
            _previewSpeed = 1f;
        }

        _isPlaying = false;
        _stateLoaded = true;
        }

        private void SaveState(AnimatorComponent animator)
        {
        if (animator == null)
            return;

        if (!StatePerAnimator.TryGetValue(animator.GetInstanceID(), out ScrubState state))
        {
            state = new ScrubState();
            StatePerAnimator[animator.GetInstanceID()] = state;
        }

        state.Clip = _clip;
        state.NormalizedTime = _normalizedTime;
        state.PreviewSpeed = _previewSpeed;
        }

        private void SetActiveClip(AnimatorComponent animator, AnimationClip clip, bool resetTime)
        {
        if (_clip == clip)
            return;

        _clip = clip;
        _isPlaying = clip != null;
        if (resetTime)
            _normalizedTime = 0f;

        StopPreviewMode(animator, true);

        if (_clip != null)
        {
            _lastEditorTime = EditorApplication.timeSinceStartup;
            SampleImmediate(animator);
        }
        else
            ResetAnimator(animator);
        }

        private void SampleImmediate(AnimatorComponent animator)
        {
        if (animator == null || _clip == null)
            return;

        EnsurePreviewMode(animator);
        float sampleTime = Mathf.Clamp01(_normalizedTime) * _clip.length;
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(animator.gameObject, _clip, sampleTime);
        AnimationMode.EndSampling();
        EditorCompatibilityUtility.RepaintAllViews();
        }

        private void ResetAnimator(AnimatorComponent animator)
        {
        _isPlaying = false;
        _normalizedTime = 0f;

        StopPreviewMode(animator, true);
        }

        private bool ApplyAvatarTPose(AnimatorComponent animator)
        {
        if (animator == null)
            return false;

        StopPreviewMode(animator, true);

        TransformComponent root = animator.transform;
        TransformComponent sourceRoot = PrefabUtility.GetCorrespondingObjectFromSource(root);
        if (sourceRoot == null)
        {
            ImprovedEditorNotifications.Warning("Avatar T-Pose Unavailable", "No source prefab or model root was found for this Animator.", 3f);
            return false;
        }

        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Force Avatar T-Pose");
        Dictionary<string, TransformComponent> sourceMap = new Dictionary<string, TransformComponent>();
        BuildPathMap(sourceRoot, string.Empty, sourceMap);
        CopyLocalRotationsFromSource(root, string.Empty, sourceMap);
        EditorUtility.SetDirty(root.gameObject);
        EditorCompatibilityUtility.RepaintAllViews();
        return true;
        }

        private void EnsurePreviewMode(AnimatorComponent animator)
        {
        if (animator == null)
            return;

        int animatorId = animator.GetInstanceID();
        if (s_previewAnimatorId != 0 && s_previewAnimatorId != animatorId && AnimationMode.InAnimationMode())
            StopPreviewMode(null, false);

        if (!AnimationMode.InAnimationMode())
            AnimationMode.StartAnimationMode();

        s_previewAnimatorId = animatorId;
        }

        private void StopPreviewMode(AnimatorComponent animator, bool rebindAfterStop)
        {
        bool ownsPreview = animator != null && s_previewAnimatorId == animator.GetInstanceID();
        bool stopGlobalPreview = AnimationMode.InAnimationMode() && (ownsPreview || animator == null);

        if (stopGlobalPreview)
        {
            AnimationMode.StopAnimationMode();
            s_previewAnimatorId = 0;
        }

        if (rebindAfterStop && animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        EditorCompatibilityUtility.RepaintAllViews();
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
        if (DrawActionButton(buttonRect, playContent, ImprovedEditorTheme.Success, _clip != null))
            TogglePlayback();

        if (DrawActionButton(resetRect, GetResetContent(), ImprovedEditorTheme.Warning, true))
        {
            ResetAnimator(animator);
            ImprovedEditorNotifications.Success("Animator Reset", "Rebound the Animator and cleared the preview state.", 2.5f);
        }

        if (DrawActionButton(tPoseRect, GetTPoseContent(), ImprovedEditorTheme.Error, canTPose))
        {
            _isPlaying = false;
            if (ApplyAvatarTPose(animator))
                ImprovedEditorNotifications.Success("Avatar T-Pose Applied", "Restored local bone rotations from the source asset.", 2.7f);
        }

        EditorGUI.LabelField(labelRect, "Animator Clips");
        if (DrawClipDropdownButton(popupRect, _clip != null ? _clip.name : "<None>", "Choose an animation clip to preview."))
            ShowClipMenu(animator, clips, popupRect);
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

        private void TogglePlayback()
        {
        _isPlaying = !_isPlaying;
        _lastEditorTime = EditorApplication.timeSinceStartup;
        if (_isPlaying && target is AnimatorComponent animator)
            SampleImmediate(animator);
        }

        private GUIContent GetPlaybackToggleContent()
        {
        GUIContent content = EditorGUIUtility.IconContent(_isPlaying ? "PauseButton" : "PlayButton");
        content.text = string.Empty;
        content.tooltip = _isPlaying ? "Pause preview playback." : "Play the active clip preview.";
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

        // Head
        DrawFilledRect(texture, 6, 1, 4, 3, white);

        // Arms in a T-pose
        DrawFilledRect(texture, 2, 5, 12, 2, white);

        // Torso
        DrawFilledRect(texture, 7, 5, 2, 6, white);

        // Legs
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
        GUI.Label(arrowRect, "▼", DropdownArrowStyle);
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
            if (_clip == clips[i])
                currentIndex = i + 1;
        }

        PopupWindow.Show(
            popupRect,
            new ClipDropdownPopup(
                popupRect.width,
                options,
                currentIndex,
                selectedIndex =>
                {
                    AnimationClip selectedClip = selectedIndex <= 0 ? null : clips[selectedIndex - 1];
                    SetActiveClip(animator, selectedClip, true);
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
