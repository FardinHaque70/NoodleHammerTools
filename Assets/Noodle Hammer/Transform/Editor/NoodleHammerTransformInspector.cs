#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Transform.Editor
{
    [CustomEditor(typeof(UnityEngine.Transform))]
    [CanEditMultipleObjects]
    public sealed class NoodleHammerTransformInspector : UnityEditor.Editor
    {
        private static Type s_transformInspectorType;

        private static Vector3 s_fullPositionBuffer;
        private static Vector3 s_fullRotationBuffer;
        private static Vector3 s_fullScaleBuffer;
        private static bool s_hasFullBuffer;

        private UnityEditor.Editor _defaultEditor;
        private string _hoverTooltip;
        private Vector2 _hoverMousePosition;
        private GUIStyle _tooltipStyle;

        private void OnEnable()
        {
            s_transformInspectorType ??= Type.GetType("UnityEditor.TransformInspector, UnityEditor");
            if (s_transformInspectorType != null)
                _defaultEditor = CreateEditor(targets, s_transformInspectorType);
        }

        private void OnDisable()
        {
            if (_defaultEditor != null)
            {
                DestroyImmediate(_defaultEditor);
                _defaultEditor = null;
            }
        }

        public override void OnInspectorGUI()
        {
            if (!TransformEditorSettings.Enabled)
            {
                if (_defaultEditor != null)
                    _defaultEditor.OnInspectorGUI();
                else
                    DrawDefaultInspector();
                return;
            }

            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
                Repaint();

            _hoverTooltip = null;

            DrawToolbar();
            EditorGUILayout.Space(6f);

            if (_defaultEditor != null)
                _defaultEditor.OnInspectorGUI();
            else
                DrawDefaultInspector();

            DrawHoverTooltip();
        }

        private void DrawToolbar()
        {
            Rect toolbarRect = EditorGUILayout.GetControlRect(false, 36f);

            const float outerPadding = 0f;
            const float gap = 6f;
            float buttonWidth = (toolbarRect.width - outerPadding * 2f - gap * 2f) / 3f;
            float buttonHeight = 26f;
            float y = toolbarRect.y + (toolbarRect.height - buttonHeight) * 0.5f;

            Rect resetRect = new Rect(toolbarRect.x + outerPadding, y, buttonWidth, buttonHeight);
            Rect copyRect = new Rect(resetRect.xMax + gap, y, buttonWidth, buttonHeight);
            Rect pasteRect = new Rect(copyRect.xMax + gap, y, buttonWidth, buttonHeight);

            if (DrawToolbarButton(resetRect, "Reset", "Reset local position, rotation, and scale on all selected Transforms.", true))
                ResetFullTransform();

            if (DrawToolbarButton(copyRect, "Copy", "Copy local position, rotation, and scale from the first selected Transform.", true))
                CopyFullTransform();

            if (DrawToolbarButton(pasteRect, "Paste", GetPasteTooltip(), s_hasFullBuffer))
                PasteFullTransform();
        }

        private bool DrawToolbarButton(Rect rect, string label, string tooltip, bool enabled)
        {
            Event evt = Event.current;
            bool hovered = rect.Contains(evt.mousePosition);

            if (!string.IsNullOrEmpty(tooltip) && hovered)
            {
                _hoverTooltip = tooltip;
                _hoverMousePosition = evt.mousePosition;
                Repaint();
            }

            using (new EditorGUI.DisabledScope(!enabled))
                return GUI.Button(rect, new GUIContent(label, tooltip));
        }

        private void CopyFullTransform()
        {
            UnityEngine.Transform source = (UnityEngine.Transform)targets[0];
            s_fullPositionBuffer = source.localPosition;
            s_fullRotationBuffer = source.localEulerAngles;
            s_fullScaleBuffer = source.localScale;
            s_hasFullBuffer = true;
        }

        private void PasteFullTransform()
        {
            ApplyToTargets("Paste Transform", transform =>
            {
                transform.localPosition = s_fullPositionBuffer;
                transform.localEulerAngles = s_fullRotationBuffer;
                transform.localScale = s_fullScaleBuffer;
            });
        }

        private void ResetFullTransform()
        {
            ApplyToTargets("Reset Transform", transform =>
            {
                transform.localPosition = Vector3.zero;
                transform.localEulerAngles = Vector3.zero;
                transform.localScale = Vector3.one;
            });
        }

        private void ApplyToTargets(string undoLabel, Action<UnityEngine.Transform> apply)
        {
            Undo.RecordObjects(targets, undoLabel);
            for (int i = 0; i < targets.Length; i++)
            {
                UnityEngine.Transform transform = (UnityEngine.Transform)targets[i];
                apply(transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
                EditorUtility.SetDirty(transform);
            }

            EditorCompatibilityUtility.RepaintAllViews();
        }

        private void DrawHoverTooltip()
        {
            if (string.IsNullOrEmpty(_hoverTooltip) || Event.current.type != EventType.Repaint)
                return;

            GUIContent content = new GUIContent(_hoverTooltip);
            float maxWidth = Mathf.Min(360f, EditorGUIUtility.currentViewWidth * 0.78f);
            Vector2 size = TooltipStyle.CalcSize(content);
            if (size.x > maxWidth)
            {
                size.x = maxWidth;
                size.y = TooltipStyle.CalcHeight(content, maxWidth);
            }

            Vector2 position = _hoverMousePosition + new Vector2(16f, 18f);
            float viewWidth = EditorGUIUtility.currentViewWidth;
            if (position.x + size.x > viewWidth - 8f)
                position.x = Mathf.Max(8f, _hoverMousePosition.x - size.x - 16f);

            Rect rect = new Rect(position, size);
            EditorGUI.DrawRect(rect, ImprovedEditorTheme.TooltipBackground);
            ImprovedEditorTheme.DrawOutline(rect, ImprovedEditorTheme.BorderStrong);

            Color previousColor = GUI.color;
            GUI.color = ImprovedEditorTheme.Text;
            GUI.Label(rect, content, TooltipStyle);
            GUI.color = previousColor;
        }

        private static string GetPasteTooltip()
        {
            if (!s_hasFullBuffer)
                return "Copy a Transform first.";

            return "Paste full local transform:\nPos   " + FormatVec3(s_fullPositionBuffer)
                + "\nRot   " + FormatVec3(s_fullRotationBuffer)
                + "\nScale " + FormatVec3(s_fullScaleBuffer);
        }

        private static string FormatVec3(Vector3 value)
        {
            return "(" + value.x.ToString("0.###") + ", " + value.y.ToString("0.###") + ", " + value.z.ToString("0.###") + ")";
        }

        private GUIStyle TooltipStyle =>
            _tooltipStyle ?? (_tooltipStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                padding = new RectOffset(8, 8, 6, 6),
                normal = { textColor = ImprovedEditorTheme.Text }
            });
    }
}
#endif
