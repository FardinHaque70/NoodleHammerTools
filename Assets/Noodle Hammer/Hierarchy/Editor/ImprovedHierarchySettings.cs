#if UNITY_EDITOR
using System.Reflection;
using UnityEditor.AnimatedValues;
using UnityEditor;
using UnityEngine;
using UnityEditorEditor = UnityEditor.Editor;

namespace NoodleHammer.Hierarchy.Editor
{
    public sealed class ImprovedHierarchySettingsAsset : ScriptableObject
    {
        public bool isActive = ImprovedHierarchySettings.D_IsActive;
        public bool alwaysShowFirstComponentIcon = ImprovedHierarchySettings.D_AlwaysShowFirstComponentIcon;
        public bool overridePrefabIcons = ImprovedHierarchySettings.D_OverridePrefabIcons;
        public bool enableTooltips = ImprovedHierarchySettings.D_EnableTooltips;
        public bool enableAlternatingRows = ImprovedHierarchySettings.D_EnableAlternatingRows;
        public bool enableRowDividers = ImprovedHierarchySettings.D_EnableRowDividers;
        public bool enableTreeGuides = ImprovedHierarchySettings.D_EnableTreeGuides;
        public ImprovedHierarchySettings.UnityNativeDetectionMode nativeDetection = ImprovedHierarchySettings.D_NativeDetection;
        public ImprovedHierarchySettings.IconDisplayMode missingScriptsIconMode = ImprovedHierarchySettings.D_MissingScriptsIconMode;
        public ImprovedHierarchySettings.IconDisplayMode noScriptsIconMode = ImprovedHierarchySettings.D_NoScriptsIconMode;
        public ImprovedHierarchySettings.IconDisplayMode singleUserScriptIconMode = ImprovedHierarchySettings.D_SingleUserScriptIconMode;
        public ImprovedHierarchySettings.IconDisplayMode unityScriptsOnlyIconMode = ImprovedHierarchySettings.D_UnityScriptsOnlyIconMode;
        public ImprovedHierarchySettings.IconDisplayMode containsUserScriptsIconMode = ImprovedHierarchySettings.D_ContainsUserScriptsIconMode;
        public ImprovedHierarchySettings.IconDisplayMode prefabIconMode = ImprovedHierarchySettings.D_PrefabIconMode;

        public void PingAsset()
        {
            Selection.activeObject = this;
            EditorGUIUtility.PingObject(this);
        }

        public void ResetToDefaults()
        {
            NoodleHammer.Core.Editor.ProjectSettingsUndoUtility.ResetToDefaultsWithUndo(
                this,
                "Reset Improved Hierarchy Settings",
                () => ImprovedHierarchySettings.ApplyDefaults(this),
                () =>
                {
                    NoodleHammer.Core.Editor.ProjectSettingsAssetUtility.Save(this);
                    EditorApplication.RepaintHierarchyWindow();
                });
        }

        private void OnValidate()
        {
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    [CustomEditor(typeof(ImprovedHierarchySettingsAsset))]
    public sealed class ImprovedHierarchySettingsAssetEditor : UnityEditorEditor
    {
        private const float ResetButtonWidth = 24f;
        private const float ResetButtonGap = 6f;
        private const float ToggleWidth = 46f;
        private const float ToggleHeight = 18f;
        private const float RowHorizontalPadding = 8f;
        private const float RowVerticalPadding = 6f;
        private const float RowBackgroundHorizontalExpand = 12f;
        private const float RowBackgroundVerticalExpand = 0f;
        private const float RowHierarchyIndent = 24f;

        private AnimBool _generalExpanded;
        private AnimBool _classificationExpanded;
        private AnimBool _prefabExpanded;
        private ImprovedHierarchySettingsAsset _defaultValues;
        private GUIContent _resetButtonContent;
        private int _sectionRowIndex;
        private SerializedObject _trackedSerializedObject;

        private void OnEnable()
        {
            _generalExpanded = CreateAnimBool(true);
            _classificationExpanded = CreateAnimBool(true);
            _prefabExpanded = CreateAnimBool(true);
            _defaultValues = ScriptableObject.CreateInstance<ImprovedHierarchySettingsAsset>();
            ImprovedHierarchySettings.ApplyDefaults(_defaultValues);
            _resetButtonContent = EditorGUIUtility.IconContent("TreeEditor.Refresh");
            _resetButtonContent.text = string.Empty;
            _resetButtonContent.tooltip = "Reset this setting to its default value.";

            _trackedSerializedObject = NoodleHammer.Core.Editor.ProjectSettingsUndoUtility
                .CreateSerializedObject(
                    (ImprovedHierarchySettingsAsset)target,
                    PersistAndNotify);
        }

        private void OnDisable()
        {
            DisposeAnimBool(_generalExpanded);
            DisposeAnimBool(_classificationExpanded);
            DisposeAnimBool(_prefabExpanded);

            if (_defaultValues != null)
                DestroyImmediate(_defaultValues);
        }

        private void PersistAndNotify()
        {
            NoodleHammer.Core.Editor.ProjectSettingsAssetUtility.Save(target);
            EditorApplication.RepaintHierarchyWindow();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool isActive = serializedObject.FindProperty("isActive").boolValue;

            ImprovedEditorTheme.DrawInspectorHeader(
                "Noodle Hammer Hierarchy",
                "Replace hierarchy row icons with smarter component-aware icons.",
                isActive);

            ImprovedEditorTheme.DrawToggleHeader(serializedObject.FindProperty("isActive"));

            _generalExpanded.target = ImprovedEditorTheme.DrawSectionHeader(_generalExpanded.target, "General", "Core hierarchy behavior and hover feedback.", isActive);
            if (EditorGUILayout.BeginFadeGroup(_generalExpanded.faded))
            {
                BeginSectionBody();
                DrawProperty("alwaysShowFirstComponentIcon", "Always Show First Component Icon", "Use the first non-Transform component icon more aggressively.");
                DrawProperty("enableTooltips", "Enable Hierarchy Icon Tooltips", "Show the component type name when hovering the hierarchy icon.");
                DrawProperty("enableAlternatingRows", "Enable Alternating Rows", "Draw subtle alternating row backgrounds across the Hierarchy.");
                DrawProperty("enableRowDividers", "Enable Row Dividers", "Draw a divider line after each hierarchy row.");
                DrawProperty("enableTreeGuides", "Enable Tree Guides", "Draw vertical and horizontal guide lines to show parent-child relationships.");
                EndSectionBody();
            }
            EditorGUILayout.EndFadeGroup();

            _classificationExpanded.target = ImprovedEditorTheme.DrawSectionHeader(_classificationExpanded.target, "Classification", "How hierarchy objects are classified and mapped to icon styles.", isActive);
            if (EditorGUILayout.BeginFadeGroup(_classificationExpanded.faded))
            {
                BeginSectionBody();
                DrawProperty("nativeDetection", "Unity Native Script Keyword", "Controls how namespaces are treated as Unity/native code.");
                DrawProperty("missingScriptsIconMode", "Contains Missing Scripts", "Icon style for objects with one or more missing scripts.");
                DrawProperty("noScriptsIconMode", "Contains No Scripts", "Icon style for objects that only contain Transform.");
                DrawProperty("singleUserScriptIconMode", "Contains Single User Script", "Icon style for objects with a single user-authored component.");
                DrawProperty("unityScriptsOnlyIconMode", "Contains Unity Scripts Only", "Icon style for objects that only contain Unity/native components.");
                DrawProperty("containsUserScriptsIconMode", "Contains User Scripts", "Icon style for objects that contain user-authored components.");
                EndSectionBody();
            }
            EditorGUILayout.EndFadeGroup();

            _prefabExpanded.target = ImprovedEditorTheme.DrawSectionHeader(_prefabExpanded.target, "Prefab Override", "Optional override rule for prefab instances.", isActive);
            if (EditorGUILayout.BeginFadeGroup(_prefabExpanded.faded))
            {
                BeginSectionBody();
                DrawProperty("overridePrefabIcons", "Override Prefab Icons", "When enabled, prefab instances use the prefab icon rule instead of the general category rule.");
                using (new EditorGUI.DisabledScope(!serializedObject.FindProperty("overridePrefabIcons").boolValue))
                    DrawProperty("prefabIconMode", "Prefab Icon Style", "Icon style to use for prefab instances when override is enabled.");
                EndSectionBody();
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Asset", GUILayout.Height(28f)))
                ((ImprovedHierarchySettingsAsset)target).PingAsset();
            if (GUILayout.Button("Reset To Defaults", GUILayout.Height(28f)) &&
                EditorUtility.DisplayDialog(
                    "Reset Noodle Hammer Hierarchy Settings",
                    "Reset all Noodle Hammer Hierarchy settings back to their default values?",
                    "Reset",
                    "Cancel"))
            {
                ((ImprovedHierarchySettingsAsset)target).ResetToDefaults();
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void BeginSectionBody()
        {
            _sectionRowIndex = 0;
            ImprovedEditorTheme.BeginSectionBody(true);
        }

        private static void EndSectionBody()
        {
            ImprovedEditorTheme.EndSectionBody();
        }

        private void DrawProperty(string propertyName, string label, string tooltip)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;

            GUIContent content = new GUIContent(label, tooltip);
            float fieldHeight = property.propertyType == SerializedPropertyType.Boolean
                ? ImprovedEditorTheme.GetStyledToggleHeight(
                    EditorGUIUtility.currentViewWidth - (RowHorizontalPadding * 2f + RowHierarchyIndent + ResetButtonWidth + ResetButtonGap + 32f),
                    content,
                    ToggleWidth)
                : EditorGUI.GetPropertyHeight(property, content, true);

            Rect rowRect = EditorGUILayout.GetControlRect(true, fieldHeight + RowVerticalPadding * 2f);
            int rowIndex = _sectionRowIndex++;
            ImprovedEditorTheme.DrawAlternatingRowBackground(rowRect, rowIndex, RowBackgroundHorizontalExpand, RowBackgroundVerticalExpand);
            ImprovedEditorTheme.DrawHierarchyGuide(rowRect, rowRect.x + RowHorizontalPadding + 4f);

            Rect fieldRect = new Rect(
                rowRect.x + RowHorizontalPadding + RowHierarchyIndent,
                rowRect.y + RowVerticalPadding,
                rowRect.width - RowHorizontalPadding * 2f - RowHierarchyIndent - ResetButtonWidth - ResetButtonGap,
                fieldHeight);
            Rect buttonRect = GetResetButtonRect(rowRect);

            if (property.propertyType == SerializedPropertyType.Boolean)
                DrawBooleanProperty(fieldRect, buttonRect, property, content);
            else
                EditorGUI.PropertyField(fieldRect, property, content, true);

            DrawResetButton(buttonRect, propertyName);
        }

        private AnimBool CreateAnimBool(bool value)
        {
            AnimBool anim = new AnimBool(value);
            anim.speed = 6f;
            anim.valueChanged.AddListener(Repaint);
            return anim;
        }

        private void DisposeAnimBool(AnimBool anim)
        {
            if (anim == null)
                return;

            anim.valueChanged.RemoveListener(Repaint);
        }

        private static Rect GetResetButtonRect(Rect rowRect)
        {
            float buttonHeight = Mathf.Min(EditorGUIUtility.singleLineHeight, rowRect.height);
            return new Rect(
                rowRect.xMax - RowHorizontalPadding - ResetButtonWidth,
                rowRect.y + (rowRect.height - buttonHeight) * 0.5f,
                ResetButtonWidth,
                buttonHeight);
        }

        private void DrawResetButton(Rect rect, string propertyName)
        {
            bool canReset = CanResetProperty(propertyName);
            Color previousBackground = GUI.backgroundColor;
            if (canReset)
                GUI.backgroundColor = ImprovedEditorTheme.Accent;

            bool clicked = GUI.Button(rect, _resetButtonContent, EditorStyles.miniButton);
            GUI.backgroundColor = previousBackground;

            if (clicked)
                ResetPropertyToDefault(propertyName);
        }

        private void DrawBooleanProperty(Rect fieldRect, Rect resetButtonRect, SerializedProperty property, GUIContent content)
        {
            Event evt = Event.current;
            Rect rowClickRect = new Rect(fieldRect.x, fieldRect.y, fieldRect.width, fieldRect.height);
            if (evt.type == EventType.MouseDown && rowClickRect.Contains(evt.mousePosition) && !resetButtonRect.Contains(evt.mousePosition))
            {
                property.boolValue = !property.boolValue;
                evt.Use();
            }

            bool stacked = fieldRect.height > EditorGUIUtility.singleLineHeight + 1f;
            Rect labelRect;
            Rect toggleRect;

            if (stacked)
            {
                labelRect = new Rect(fieldRect.x, fieldRect.y, fieldRect.width, EditorGUIUtility.singleLineHeight);
                float toggleY = fieldRect.y + EditorGUIUtility.singleLineHeight + 4f;
                toggleRect = new Rect(fieldRect.xMax - ToggleWidth, toggleY, ToggleWidth, ToggleHeight);
            }
            else
            {
                labelRect = new Rect(fieldRect.x, fieldRect.y, fieldRect.width - ToggleWidth - 10f, fieldRect.height);
                toggleRect = new Rect(fieldRect.xMax - ToggleWidth, fieldRect.y + Mathf.Max(0f, (fieldRect.height - ToggleHeight) * 0.5f), ToggleWidth, ToggleHeight);
            }

            EditorGUI.LabelField(labelRect, content);
            ImprovedEditorTheme.DrawInlineToggle(toggleRect, property);
        }

        private bool CanResetProperty(string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || _defaultValues == null)
                return false;

            FieldInfo field = typeof(ImprovedHierarchySettingsAsset).GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return false;

            object defaultValue = field.GetValue(_defaultValues);
            return !IsPropertyAtDefault(property, defaultValue);
        }

        private static bool IsPropertyAtDefault(SerializedProperty property, object defaultValue)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return property.boolValue == (bool)defaultValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex == (int)defaultValue;
                default:
                    return true;
            }
        }

        private void ResetPropertyToDefault(string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || _defaultValues == null)
                return;

            FieldInfo field = typeof(ImprovedHierarchySettingsAsset).GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return;

            Undo.RecordObject(target, $"Reset {propertyName}");
            object defaultValue = field.GetValue(_defaultValues);

            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    property.boolValue = (bool)defaultValue;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = (int)defaultValue;
                    break;
            }
        }
    }

    public static class ImprovedHierarchySettings
    {
        public enum IconDisplayMode
        {
            SmallIcon,
            LargeIcon,
            UnityDefault
        }

        public enum UnityNativeDetectionMode
        {
            UnityEngine,
            Unity,
            None
        }

        public const string ScriptFileName = "ImprovedHierarchySettings.cs";
        public const string AssetFileName = "Noodle Hammer Hierarchy Settings.asset";

        public const bool D_IsActive = true;
        public const bool D_AlwaysShowFirstComponentIcon = false;
        public const bool D_OverridePrefabIcons = false;
        public const bool D_EnableTooltips = true;
        public const bool D_EnableAlternatingRows = true;
        public const bool D_EnableRowDividers = true;
        public const bool D_EnableTreeGuides = true;
        public const UnityNativeDetectionMode D_NativeDetection = UnityNativeDetectionMode.Unity;
        public const IconDisplayMode D_MissingScriptsIconMode = IconDisplayMode.LargeIcon;
        public const IconDisplayMode D_NoScriptsIconMode = IconDisplayMode.LargeIcon;
        public const IconDisplayMode D_SingleUserScriptIconMode = IconDisplayMode.SmallIcon;
        public const IconDisplayMode D_UnityScriptsOnlyIconMode = IconDisplayMode.LargeIcon;
        public const IconDisplayMode D_ContainsUserScriptsIconMode = IconDisplayMode.SmallIcon;
        public const IconDisplayMode D_PrefabIconMode = IconDisplayMode.SmallIcon;

        private static ImprovedHierarchySettingsAsset _cachedAsset;

        public static bool Active => Asset != null && Asset.isActive;
        public static bool AlwaysShowFirstComponentIcon => Asset.alwaysShowFirstComponentIcon;
        public static bool OverridePrefabIcons => Asset.overridePrefabIcons;
        public static bool EnableTooltips => Asset.enableTooltips;
        public static bool EnableAlternatingRows => Asset.enableAlternatingRows;
        public static bool EnableRowDividers => Asset.enableRowDividers;
        public static bool EnableTreeGuides => Asset.enableTreeGuides;
        public static UnityNativeDetectionMode NativeDetection => Asset.nativeDetection;
        public static IconDisplayMode MissingScriptsIconMode => Asset.missingScriptsIconMode;
        public static IconDisplayMode NoScriptsIconMode => Asset.noScriptsIconMode;
        public static IconDisplayMode SingleUserScriptIconMode => Asset.singleUserScriptIconMode;
        public static IconDisplayMode UnityScriptsOnlyIconMode => Asset.unityScriptsOnlyIconMode;
        public static IconDisplayMode ContainsUserScriptsIconMode => Asset.containsUserScriptsIconMode;
        public static IconDisplayMode PrefabIconMode => Asset.prefabIconMode;

        [MenuItem("Tools/Noodle Hammer/Hierarchy Settings")]
        public static void SelectSettingsAsset()
        {
            Selection.activeObject = Asset;
            EditorGUIUtility.PingObject(Asset);
        }

        public static ImprovedHierarchySettingsAsset Asset
        {
            get
            {
                if (_cachedAsset == null)
                    _cachedAsset = NoodleHammer.Core.Editor.ProjectSettingsAssetUtility
                        .LoadOrCreate<ImprovedHierarchySettingsAsset>(AssetPath, ApplyDefaults);

                return _cachedAsset;
            }
        }

        internal static IconDisplayMode GetIconMode(ImprovedHierarchyRuleCategory category)
        {
            switch (category)
            {
                case ImprovedHierarchyRuleCategory.MissingScripts:
                    return MissingScriptsIconMode;
                case ImprovedHierarchyRuleCategory.NoScripts:
                    return NoScriptsIconMode;
                case ImprovedHierarchyRuleCategory.SingleUserScript:
                    return SingleUserScriptIconMode;
                case ImprovedHierarchyRuleCategory.ContainsUserScripts:
                    return ContainsUserScriptsIconMode;
                case ImprovedHierarchyRuleCategory.UnityScriptsOnly:
                default:
                    return UnityScriptsOnlyIconMode;
            }
        }

        public static string AssetDirectory => "Assets/Noodle Hammer/Hierarchy/Settings";
        public static string AssetPath => AssetDirectory + "/" + AssetFileName;

        public static void ApplyDefaults(ImprovedHierarchySettingsAsset asset)
        {
            if (asset == null)
                return;

            asset.isActive = D_IsActive;
            asset.alwaysShowFirstComponentIcon = D_AlwaysShowFirstComponentIcon;
            asset.overridePrefabIcons = D_OverridePrefabIcons;
            asset.enableTooltips = D_EnableTooltips;
            asset.enableAlternatingRows = D_EnableAlternatingRows;
            asset.enableRowDividers = D_EnableRowDividers;
            asset.enableTreeGuides = D_EnableTreeGuides;
            asset.nativeDetection = D_NativeDetection;
            asset.missingScriptsIconMode = D_MissingScriptsIconMode;
            asset.noScriptsIconMode = D_NoScriptsIconMode;
            asset.singleUserScriptIconMode = D_SingleUserScriptIconMode;
            asset.unityScriptsOnlyIconMode = D_UnityScriptsOnlyIconMode;
            asset.containsUserScriptsIconMode = D_ContainsUserScriptsIconMode;
            asset.prefabIconMode = D_PrefabIconMode;
        }
    }
}
#endif
