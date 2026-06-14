#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Hierarchy.Editor
{
    [InitializeOnLoad]
    internal static class ImprovedHierarchyIconDisplayer
    {
        private const float IconSlotWidth = 18.5f;
        private const float TreeIndentWidth = 14f;
        private const float TreeGuideColumnOffset = 8f;
        private const float TreeGuideBranchRightPadding = 2f;
        private const float ExpandArrowWidth = 11f;
        private const float ExpandArrowOffset = 14f;
        private const float MissingBadgeWidth = 14f;
        private const float MissingBadgeInset = 2f;

        private static readonly HashSet<int> AdditionalSelectedInstanceIds = new HashSet<int>();

        private static readonly System.Type SceneHierarchyWindowType =
            typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");

        private static EditorWindow s_hierarchyWindow;
        private static bool s_hierarchyHasFocus;
        private static GUIStyle s_missingBadgeStyle;
        private static PropertyInfo s_sceneHierarchyProperty;
        private static MethodInfo s_sceneHierarchyIsExpandedMethod;
        private static MethodInfo s_instanceIdToObjectMethod;

        static ImprovedHierarchyIconDisplayer()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (s_hierarchyWindow == null)
            {
                ResolveHierarchyWindow();
            }

            s_hierarchyHasFocus = EditorWindow.focusedWindow != null && EditorWindow.focusedWindow == s_hierarchyWindow;
            AdditionalSelectedInstanceIds.Clear();
        }

        private static void OnHierarchyWindowItemGUI(int instanceId, Rect selectionRect)
        {
            if (!ImprovedHierarchySettings.Active)
                return;

            GameObject gameObject = ResolveObjectFromInstanceId(instanceId) as GameObject;
            if (gameObject == null)
                return;

            ImprovedHierarchyRowState rowState = BuildRowState(instanceId, selectionRect);
            DrawRowVisuals(selectionRect, rowState);
            DrawTreeGuides(gameObject.transform, selectionRect);

            ImprovedHierarchyIconInfo iconInfo = ImprovedHierarchyClassifier.Analyze(gameObject);
            if (!iconInfo.HasIcon)
                return;

            if (iconInfo.IconMode == ImprovedHierarchySettings.IconDisplayMode.UnityDefault)
            {
                if (iconInfo.HasMissingScript)
                    DrawMissingScriptBadge(selectionRect);
                return;
            }

            UpdateSelectionTracking(instanceId, rowState);

            if (iconInfo.IconMode == ImprovedHierarchySettings.IconDisplayMode.LargeIcon)
                ClearOriginalIconSlot(rowState, selectionRect);

            DrawIcon(selectionRect, iconInfo.Content, gameObject, iconInfo.IconMode);

            if (iconInfo.HasMissingScript)
                DrawMissingScriptBadge(selectionRect);
        }

        private static ImprovedHierarchyRowState BuildRowState(int instanceId, Rect selectionRect)
        {
            Rect fullRowRect = selectionRect;
            fullRowRect.x = 0f;
            fullRowRect.width = short.MaxValue;

            Rect expandArrowRect = selectionRect;
            expandArrowRect.x -= ExpandArrowOffset;
            expandArrowRect.width = ExpandArrowWidth;

            return new ImprovedHierarchyRowState
            {
                IsSelected = IsGameObjectSelected(instanceId),
                IsHovered = fullRowRect.Contains(Event.current.mousePosition),
                IsExpandArrowHovered = expandArrowRect.Contains(Event.current.mousePosition)
            };
        }

        private static void UpdateSelectionTracking(int instanceId, ImprovedHierarchyRowState rowState)
        {
            if (rowState.IsSelected || (rowState.IsExpandArrowHovered && ImprovedHierarchyMouseState.IsMouseDown))
            {
                if (Selection.gameObjects.Length > 1)
                    AdditionalSelectedInstanceIds.Clear();

                AdditionalSelectedInstanceIds.Add(instanceId);
                return;
            }

            AdditionalSelectedInstanceIds.Remove(instanceId);
        }

        private static void ClearOriginalIconSlot(ImprovedHierarchyRowState rowState, Rect selectionRect)
        {
            int selectedCount = Selection.gameObjects.Length > 1
                ? Selection.gameObjects.Length
                : AdditionalSelectedInstanceIds.Count;
            Color background = ImprovedHierarchyBackgrounds.GetIconSlotBackground(rowState, s_hierarchyHasFocus, selectedCount);

            Rect backgroundRect = selectionRect;
            backgroundRect.width = IconSlotWidth;
            EditorGUI.DrawRect(backgroundRect, background);
        }

        private static bool IsGameObjectSelected(int instanceId)
        {
            GameObject[] selectedGameObjects = Selection.gameObjects;
            for (int i = 0; i < selectedGameObjects.Length; i++)
            {
                GameObject selectedGameObject = selectedGameObjects[i];
                if (selectedGameObject != null && selectedGameObject.GetInstanceID() == instanceId)
                    return true;
            }

            return false;
        }

        private static Object ResolveObjectFromInstanceId(int instanceId)
        {
            if (s_instanceIdToObjectMethod == null)
            {
                s_instanceIdToObjectMethod = typeof(EditorUtility).GetMethod(
                    "InstanceIDToObject",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(int) },
                    null);
            }

            return s_instanceIdToObjectMethod?.Invoke(null, new object[] { instanceId }) as Object;
        }

        private static void DrawRowVisuals(Rect selectionRect, ImprovedHierarchyRowState rowState)
        {
            Rect fullRowRect = selectionRect;
            fullRowRect.x = 0f;
            fullRowRect.width = short.MaxValue;

            if (ImprovedHierarchySettings.EnableAlternatingRows && !rowState.IsSelected && !rowState.IsHovered)
            {
                int rowIndex = Mathf.Max(0, Mathf.RoundToInt(selectionRect.y / Mathf.Max(1f, selectionRect.height)));
                Color rowColor = (rowIndex & 1) == 0
                    ? new Color(1f, 1f, 1f, 0.012f)
                    : new Color(0f, 0f, 0f, 0.06f);
                EditorGUI.DrawRect(fullRowRect, rowColor);
            }

            if (ImprovedHierarchySettings.EnableRowDividers)
            {
                Color dividerColor = rowState.IsSelected
                    ? new Color(1f, 1f, 1f, 0.10f)
                    : new Color(1f, 1f, 1f, 0.06f);
                EditorGUI.DrawRect(
                    new Rect(fullRowRect.x, selectionRect.yMax - 1f, fullRowRect.width, 1f),
                    dividerColor);
            }
        }

        private static void DrawTreeGuides(UnityEngine.Transform transform, Rect selectionRect)
        {
            if (!ImprovedHierarchySettings.EnableTreeGuides || transform == null)
                return;

            UnityEngine.Transform parent = transform.parent;
            if (parent == null)
                return;

            Color guideColor = new Color(
                ImprovedEditorTheme.HierarchyGuide.r,
                ImprovedEditorTheme.HierarchyGuide.g,
                ImprovedEditorTheme.HierarchyGuide.b,
                0.28f);
            float centerY = Mathf.Round(selectionRect.y + selectionRect.height * 0.5f);
            float expandArrowX = selectionRect.x - ExpandArrowOffset;
            float currentColumnX = Mathf.Round(expandArrowX - TreeGuideColumnOffset);

            UnityEngine.Transform ancestor = parent;
            float ancestorColumnX = currentColumnX - TreeIndentWidth;
            while (ancestor != null)
            {
                if (HasNextSibling(ancestor))
                {
                    EditorGUI.DrawRect(
                        new Rect(ancestorColumnX, selectionRect.y, 1f, selectionRect.height),
                        guideColor);
                }

                ancestor = ancestor.parent;
                ancestorColumnX -= TreeIndentWidth;
            }

            float topHeight = Mathf.Max(0f, centerY - selectionRect.y);
            if (topHeight > 0f)
            {
                EditorGUI.DrawRect(
                    new Rect(currentColumnX, selectionRect.y, 1f, topHeight),
                    guideColor);
            }

            bool continuesBelow = HasNextSibling(transform) || HasVisibleExpandedChildren(transform);
            if (continuesBelow)
            {
                float lowerY = centerY;
                float lowerHeight = Mathf.Max(0f, selectionRect.yMax - lowerY);
                if (lowerHeight > 0f)
                {
                    EditorGUI.DrawRect(
                        new Rect(currentColumnX, lowerY, 1f, lowerHeight),
                        guideColor);
                }
            }

            float horizontalStart = currentColumnX;
            float horizontalEnd = Mathf.Max(horizontalStart, expandArrowX - TreeGuideBranchRightPadding);
            float horizontalWidth = Mathf.Max(0f, horizontalEnd - horizontalStart);
            if (horizontalWidth > 0f)
            {
                EditorGUI.DrawRect(
                    new Rect(horizontalStart, centerY, horizontalWidth, 1f),
                    guideColor);
            }
        }

        private static bool HasNextSibling(UnityEngine.Transform transform)
        {
            if (transform == null || transform.parent == null)
                return false;

            return transform.GetSiblingIndex() < transform.parent.childCount - 1;
        }

        private static bool HasVisibleExpandedChildren(UnityEngine.Transform transform)
        {
            if (transform == null || transform.childCount == 0)
                return false;

            return IsHierarchyItemExpanded(transform.gameObject.GetInstanceID());
        }

        private static bool IsHierarchyItemExpanded(int instanceId)
        {
            object sceneHierarchy = GetSceneHierarchy();
            if (sceneHierarchy == null)
                return false;

            if (s_sceneHierarchyIsExpandedMethod == null)
            {
                s_sceneHierarchyIsExpandedMethod = sceneHierarchy.GetType().GetMethod(
                    "IsExpanded",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(int) },
                    null);
            }

            if (s_sceneHierarchyIsExpandedMethod == null)
                return false;

            object result = s_sceneHierarchyIsExpandedMethod.Invoke(sceneHierarchy, new object[] { instanceId });
            return result is bool isExpanded && isExpanded;
        }

        private static object GetSceneHierarchy()
        {
            EditorWindow hierarchyWindow = ResolveHierarchyWindow();
            if (hierarchyWindow == null)
                return null;

            if (s_sceneHierarchyProperty == null)
            {
                s_sceneHierarchyProperty = hierarchyWindow.GetType().GetProperty(
                    "sceneHierarchy",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            return s_sceneHierarchyProperty?.GetValue(hierarchyWindow, null);
        }

        private static EditorWindow ResolveHierarchyWindow()
        {
            if (s_hierarchyWindow != null)
                return s_hierarchyWindow;

            if (SceneHierarchyWindowType == null)
                return null;

            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow != null && SceneHierarchyWindowType.IsInstanceOfType(focusedWindow))
            {
                s_hierarchyWindow = focusedWindow;
                return s_hierarchyWindow;
            }

            Object[] hierarchyWindows = Resources.FindObjectsOfTypeAll(SceneHierarchyWindowType);
            if (hierarchyWindows != null && hierarchyWindows.Length > 0)
                s_hierarchyWindow = hierarchyWindows[0] as EditorWindow;

            return s_hierarchyWindow;
        }

        private static void DrawIcon(
            Rect selectionRect,
            GUIContent content,
            GameObject gameObject,
            ImprovedHierarchySettings.IconDisplayMode iconMode)
        {
            Rect iconRect = selectionRect;
            if (iconMode == ImprovedHierarchySettings.IconDisplayMode.SmallIcon)
            {
                iconRect.width = 10f;
                iconRect.height = 10f;
                iconRect.position += new Vector2(7f, 7f);
            }

            Color previousColor = GUI.color;
            if (!gameObject.activeInHierarchy)
                GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, 0.5f);

            EditorGUI.LabelField(iconRect, content);
            GUI.color = previousColor;
        }

        private static bool IsHierarchyWindowFocused()
        {
            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            return focusedWindow != null && focusedWindow.GetType().Name == "SceneHierarchyWindow";
        }

        private static void DrawMissingScriptBadge(Rect selectionRect)
        {
            if (selectionRect.width < 40f)
                return;

            Rect badgeRect = new Rect(
                selectionRect.xMax - MissingBadgeWidth - MissingBadgeInset,
                selectionRect.y + 1f,
                MissingBadgeWidth,
                Mathf.Max(12f, selectionRect.height - 2f));

            EditorGUI.DrawRect(badgeRect, new Color(0.72f, 0.32f, 0.16f, 0.95f));
            ImprovedEditorTheme.DrawOutline(badgeRect, new Color(0f, 0f, 0f, 0.42f));
            GUI.Label(badgeRect, new GUIContent("!", "Missing Script"), MissingBadgeStyle);
        }

        private static GUIStyle MissingBadgeStyle =>
            s_missingBadgeStyle ?? (s_missingBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
    }
}
#endif
