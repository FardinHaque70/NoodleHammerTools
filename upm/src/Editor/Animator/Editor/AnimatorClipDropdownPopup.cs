#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Animator.Editor
{
	/// <summary>
	/// Popup window for selecting an animation clip from a list.
	/// </summary>
	internal sealed class AnimatorClipDropdownPopup : PopupWindowContent
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

		public AnimatorClipDropdownPopup(float popupWidth, GUIContent[] options, int selectedIndex, Action<int> onSelected)
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
					GUI.Label(checkRect, "\u2713", CheckStyle);

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
}
#endif
