#if UNITY_EDITOR
using System.Collections.Generic;
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

        // Pre-allocated colors to avoid per-row new Color() allocations
        private static readonly Color AlternatingRowEven = new Color(1f, 1f, 1f, 0.012f);
        private static readonly Color AlternatingRowOdd = new Color(0f, 0f, 0f, 0.06f);
        private static readonly Color DividerSelected = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color DividerNormal = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color InactiveAlpha = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color MissingBadgeBackground = new Color(0.72f, 0.32f, 0.16f, 0.95f);
        private static readonly Color MissingBadgeBorder = new Color(0f, 0f, 0f, 0.42f);

        // Cached per-repaint-pass state to avoid repeated property lookups and allocations
        private static readonly HashSet<int> AdditionalSelectedInstanceIds = new HashSet<int>();
        private static readonly HashSet<int> SelectionIdSet = new HashSet<int>();

        private static bool s_hierarchyHasFocus;
        private static GUIStyle s_missingBadgeStyle;
        private static GUIContent s_missingBadgeContent;
        private static bool s_updateRegistered;
        private static int s_cachedSelectionVersion;

        // Settings cached once per repaint pass to avoid per-row property chain traversal
        private static bool s_cachedActive;
        private static bool s_cachedEnableAlternatingRows;
        private static bool s_cachedEnableRowDividers;
        private static bool s_cachedEnableTreeGuides;
        private static Color s_cachedGuideColor;
        private static bool s_settingsCachedThisEvent;
        private static EventType s_lastCachedEventType;
        private static int s_lastCachedEventHash;

        static ImprovedHierarchyIconDisplayer()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
            NoodleHammer.Core.Editor.EditorUpdateLoop.EnsureRegistered(ref s_updateRegistered, OnEditorUpdate);
        }

        private static void OnEditorUpdate()
        {
            if (NoodleHammer.Core.Editor.EditorTransitionGuard.IsUnsafeTransition())
                return;

            EditorWindow hierarchyWindow = HierarchyReflectionBridge.ResolveHierarchyWindow();
            if (hierarchyWindow == null)
                HierarchyReflectionBridge.InvalidateWindow();

            s_hierarchyHasFocus = EditorWindow.focusedWindow != null && EditorWindow.focusedWindow == hierarchyWindow;
            AdditionalSelectedInstanceIds.Clear();

            // Invalidate settings cache so next repaint picks up changes
            s_settingsCachedThisEvent = false;
        }

        private static void OnHierarchyWindowItemGUI(int instanceId, Rect selectionRect)
        {
            if (NoodleHammer.Core.Editor.EditorTransitionGuard.IsUnsafeTransition())
                return;

            // Cache settings once per repaint event batch
            CacheSettingsIfNeeded();

            if (!s_cachedActive)
                return;

            // Cross-version compatible: EntityIdToObject on Unity 6+, InstanceIDToObject on 2022
            GameObject gameObject = NoodleHammer.Core.Editor.EditorCompatibilityUtility.InstanceIDToObject(instanceId) as GameObject;
            if (gameObject == null)
                return;

            // Cache selection set once per event to avoid per-row array allocation from Selection.gameObjects
            CacheSelectionIfNeeded();

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

        private static void CacheSettingsIfNeeded()
        {
            // Use event identity to avoid re-caching within the same event batch
            Event current = Event.current;
            int eventHash = current != null ? current.GetHashCode() : 0;
            EventType eventType = current != null ? current.type : EventType.Ignore;

            if (s_settingsCachedThisEvent && eventType == s_lastCachedEventType && eventHash == s_lastCachedEventHash)
                return;

            s_cachedActive = ImprovedHierarchySettings.Active;
            s_cachedEnableAlternatingRows = ImprovedHierarchySettings.EnableAlternatingRows;
            s_cachedEnableRowDividers = ImprovedHierarchySettings.EnableRowDividers;
            s_cachedEnableTreeGuides = ImprovedHierarchySettings.EnableTreeGuides;

            Color baseGuide = ImprovedEditorTheme.HierarchyGuide;
            s_cachedGuideColor = new Color(baseGuide.r, baseGuide.g, baseGuide.b, 0.28f);

            s_settingsCachedThisEvent = true;
            s_lastCachedEventType = eventType;
            s_lastCachedEventHash = eventHash;
        }

        private static void CacheSelectionIfNeeded()
        {
            // Selection.gameObjects allocates a new array each access.
            // Cache the instance IDs into a HashSet once, then use O(1) lookups per row.
            int currentVersion = SelectionVersionTracker.Version;
            if (currentVersion == s_cachedSelectionVersion && SelectionIdSet.Count > 0)
                return;

            s_cachedSelectionVersion = currentVersion;
            SelectionIdSet.Clear();
            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] != null)
                    SelectionIdSet.Add(selected[i].GetInstanceID());
            }
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
                IsSelected = SelectionIdSet.Contains(instanceId),
                IsHovered = fullRowRect.Contains(Event.current.mousePosition),
                IsExpandArrowHovered = expandArrowRect.Contains(Event.current.mousePosition)
            };
        }

        private static void UpdateSelectionTracking(int instanceId, ImprovedHierarchyRowState rowState)
        {
            if (rowState.IsSelected || (rowState.IsExpandArrowHovered && ImprovedHierarchyMouseState.IsMouseDown))
            {
                if (SelectionIdSet.Count > 1)
                    AdditionalSelectedInstanceIds.Clear();

                AdditionalSelectedInstanceIds.Add(instanceId);
                return;
            }

            AdditionalSelectedInstanceIds.Remove(instanceId);
        }

        private static void ClearOriginalIconSlot(ImprovedHierarchyRowState rowState, Rect selectionRect)
        {
            int selectedCount = SelectionIdSet.Count > 1
                ? SelectionIdSet.Count
                : AdditionalSelectedInstanceIds.Count;
            Color background = ImprovedHierarchyBackgrounds.GetIconSlotBackground(rowState, s_hierarchyHasFocus, selectedCount);

            Rect backgroundRect = selectionRect;
            backgroundRect.width = IconSlotWidth;
            EditorGUI.DrawRect(backgroundRect, background);
        }

        private static void DrawRowVisuals(Rect selectionRect, ImprovedHierarchyRowState rowState)
        {
            Rect fullRowRect = selectionRect;
            fullRowRect.x = 0f;
            fullRowRect.width = short.MaxValue;

            if (s_cachedEnableAlternatingRows && !rowState.IsSelected && !rowState.IsHovered)
            {
                int rowIndex = Mathf.Max(0, Mathf.RoundToInt(selectionRect.y / Mathf.Max(1f, selectionRect.height)));
                EditorGUI.DrawRect(fullRowRect, (rowIndex & 1) == 0 ? AlternatingRowEven : AlternatingRowOdd);
            }

            if (s_cachedEnableRowDividers)
            {
                EditorGUI.DrawRect(
                    new Rect(fullRowRect.x, selectionRect.yMax - 1f, fullRowRect.width, 1f),
                    rowState.IsSelected ? DividerSelected : DividerNormal);
            }
        }

        private static void DrawTreeGuides(Transform transform, Rect selectionRect)
        {
            if (!s_cachedEnableTreeGuides || transform == null)
                return;

            Transform parent = transform.parent;
            if (parent == null)
                return;

            Color guideColor = s_cachedGuideColor;
            float centerY = Mathf.Round(selectionRect.y + selectionRect.height * 0.5f);
            float expandArrowX = selectionRect.x - ExpandArrowOffset;
            float currentColumnX = Mathf.Round(expandArrowX - TreeGuideColumnOffset);

            Transform ancestor = parent;
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

        private static bool HasNextSibling(Transform transform)
        {
            if (transform == null || transform.parent == null)
                return false;

            return transform.GetSiblingIndex() < transform.parent.childCount - 1;
        }

        private static bool HasVisibleExpandedChildren(Transform transform)
        {
            if (transform == null || transform.childCount == 0)
                return false;

            return HierarchyReflectionBridge.IsHierarchyItemExpanded(transform.gameObject.GetInstanceID());
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
                iconRect.x += 7f;
                iconRect.y += 7f;
            }

            Color previousColor = GUI.color;
            if (!gameObject.activeInHierarchy)
                GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, 0.5f);

            EditorGUI.LabelField(iconRect, content);
            GUI.color = previousColor;
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

            EditorGUI.DrawRect(badgeRect, MissingBadgeBackground);
            ImprovedEditorTheme.DrawOutline(badgeRect, MissingBadgeBorder);
            GUI.Label(badgeRect, MissingBadgeContent, MissingBadgeStyle);
        }

        private static GUIContent MissingBadgeContent =>
            s_missingBadgeContent ?? (s_missingBadgeContent = new GUIContent("!", "Missing Script"));

        private static GUIStyle MissingBadgeStyle =>
            s_missingBadgeStyle ?? (s_missingBadgeStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
    }

    /// <summary>
    /// Tracks Selection changes via selectionChanged callback to avoid re-building
    /// the selection HashSet when nothing changed.
    /// </summary>
    [InitializeOnLoad]
    internal static class SelectionVersionTracker
    {
        private static int s_version;

        public static int Version => s_version;

        static SelectionVersionTracker()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            s_version++;
        }
    }
}
#endif
