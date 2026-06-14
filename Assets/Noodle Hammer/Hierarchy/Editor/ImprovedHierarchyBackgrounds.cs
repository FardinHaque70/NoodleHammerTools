#if UNITY_EDITOR
using UnityEngine;

namespace NoodleHammer.Hierarchy.Editor
{
    internal static class ImprovedHierarchyBackgrounds
    {
        private struct SkinColor
        {
            public Color Dark;
            public Color Light;

            public Color Resolve()
            {
                return UnityEditor.EditorGUIUtility.isProSkin ? Dark : Light;
            }
        }

        private static readonly SkinColor Default = new SkinColor
        {
            Dark = new Color(0.2196f, 0.2196f, 0.2196f),
            Light = new Color(0.7843f, 0.7843f, 0.7843f)
        };

        private static readonly SkinColor Selected = new SkinColor
        {
            Dark = new Color(0.1725f, 0.3647f, 0.5294f),
            Light = new Color(0.22745f, 0.4470f, 0.6902f)
        };

        private static readonly SkinColor SelectedUnfocused = new SkinColor
        {
            Dark = new Color(0.30f, 0.30f, 0.30f),
            Light = new Color(0.68f, 0.68f, 0.68f)
        };

        private static readonly SkinColor Hovered = new SkinColor
        {
            Dark = new Color(0.2706f, 0.2706f, 0.2706f),
            Light = new Color(0.698f, 0.698f, 0.698f)
        };

        public static Color GetIconSlotBackground(ImprovedHierarchyRowState rowState, bool hierarchyHasFocus, int selectedCount)
        {
            bool isMouseDown = ImprovedHierarchyMouseState.IsMouseDown;

            if (rowState.IsSelected)
            {
                if (isMouseDown && !rowState.IsExpandArrowHovered && !rowState.IsHovered && selectedCount == 1)
                    return Default.Resolve();

                return hierarchyHasFocus ? Selected.Resolve() : SelectedUnfocused.Resolve();
            }

            if (rowState.IsHovered)
                return isMouseDown && !rowState.IsExpandArrowHovered ? Selected.Resolve() : Hovered.Resolve();

            return Default.Resolve();
        }
    }
}
#endif
