#if UNITY_EDITOR
using UnityEngine;

namespace NoodleHammer.Hierarchy.Editor
{
    internal static class ImprovedHierarchyMouseState
    {
        private static bool s_isMouseDown;
        private static bool s_isMouseDragged;

        public static bool IsMouseDown
        {
            get
            {
                Update();
                return s_isMouseDown;
            }
        }

        public static bool IsMouseDragged
        {
            get
            {
                Update();
                return s_isMouseDragged;
            }
        }

        private static void Update()
        {
            Event current = Event.current;
            if (current == null)
                return;

            switch (current.type)
            {
                case EventType.MouseDown:
                    s_isMouseDown = true;
                    break;
                case EventType.MouseUp:
                    s_isMouseDown = false;
                    s_isMouseDragged = false;
                    break;
                case EventType.MouseDrag:
                    s_isMouseDown = true;
                    s_isMouseDragged = true;
                    break;
                case EventType.DragExited:
                    s_isMouseDown = false;
                    s_isMouseDragged = false;
                    break;
            }
        }
    }
}
#endif
