#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace NoodleHammer.Hierarchy.Editor
{
    internal enum ImprovedHierarchyRuleCategory
    {
        MissingScripts,
        NoScripts,
        SingleUserScript,
        UnityScriptsOnly,
        ContainsUserScripts
    }

    internal readonly struct ImprovedHierarchyIconInfo
    {
        public ImprovedHierarchyIconInfo(
            GUIContent content,
            ImprovedHierarchySettings.IconDisplayMode iconMode,
            ImprovedHierarchyRuleCategory category,
            bool hasMissingScript,
            int userScriptCount)
        {
            Content = content;
            IconMode = iconMode;
            Category = category;
            HasMissingScript = hasMissingScript;
            UserScriptCount = userScriptCount;
        }

        public GUIContent Content { get; }
        public ImprovedHierarchySettings.IconDisplayMode IconMode { get; }
        public ImprovedHierarchyRuleCategory Category { get; }
        public bool HasMissingScript { get; }
        public int UserScriptCount { get; }
        public bool HasIcon => Content != null && Content.image != null;
    }

    internal static class ImprovedHierarchyClassifier
    {
        public static ImprovedHierarchyIconInfo Analyze(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();
            if (components == null || components.Length == 0)
                return default;

            bool hasMissingScript = HasMissingScript(components);
            int nonTransformCount = GetNonTransformCount(components);
            int userScriptCount = GetUserScriptCount(components);
            Component firstNonTransform = GetFirstNonTransformComponent(components);
            Component firstUserComponent = GetFirstUserComponent(components);

            ImprovedHierarchyRuleCategory category = Classify(hasMissingScript, nonTransformCount, userScriptCount);
            Component displayComponent = ResolveDisplayComponent(
                gameObject,
                components,
                firstNonTransform,
                firstUserComponent,
                category);
            GUIContent content = GetContent(gameObject, displayComponent, category);

            ImprovedHierarchySettings.IconDisplayMode iconMode = ImprovedHierarchySettings.GetIconMode(category);
            if (ImprovedHierarchySettings.OverridePrefabIcons && IsPrefab(gameObject))
                iconMode = ImprovedHierarchySettings.PrefabIconMode;

            return new ImprovedHierarchyIconInfo(content, iconMode, category, hasMissingScript, userScriptCount);
        }

        private static ImprovedHierarchyRuleCategory Classify(bool hasMissingScript, int nonTransformCount, int userScriptCount)
        {
            if (hasMissingScript)
                return ImprovedHierarchyRuleCategory.MissingScripts;

            if (nonTransformCount <= 0)
                return ImprovedHierarchyRuleCategory.NoScripts;

            if (userScriptCount <= 0)
                return ImprovedHierarchyRuleCategory.UnityScriptsOnly;

            return nonTransformCount == 1 && userScriptCount == 1
                ? ImprovedHierarchyRuleCategory.SingleUserScript
                : ImprovedHierarchyRuleCategory.ContainsUserScripts;
        }

        private static Component ResolveDisplayComponent(
            GameObject gameObject,
            Component[] components,
            Component firstNonTransform,
            Component firstUserComponent,
            ImprovedHierarchyRuleCategory category)
        {
            if (category == ImprovedHierarchyRuleCategory.MissingScripts)
                return null;

            if (category == ImprovedHierarchyRuleCategory.NoScripts)
                return null;

            if (ImprovedHierarchySettings.AlwaysShowFirstComponentIcon)
                return firstNonTransform ?? components[0];

            Component priorityComponent = GetPriorityComponent(components, category);
            if (priorityComponent != null)
                return priorityComponent;

            if (category == ImprovedHierarchyRuleCategory.ContainsUserScripts && firstUserComponent != null)
                return firstUserComponent;

            return firstNonTransform ?? gameObject.transform;
        }

        private static Component GetFirstNonTransformComponent(Component[] components)
        {
            if (components == null || components.Length == 0)
                return null;

            for (int i = 1; i < components.Length; i++)
            {
                if (components[i] == null)
                    return null;

                if (!(components[i] is UnityEngine.Transform))
                    return components[i];
            }

            return components[0];
        }

        private static Component GetFirstUserComponent(Component[] components)
        {
            if (components == null)
                return null;

            for (int i = 1; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    return null;

                if (!IsUnityRelated(component))
                    return component;
            }

            return null;
        }

        private static int GetUserScriptCount(Component[] components)
        {
            if (components == null)
                return 0;

            int count = 0;
            for (int i = 1; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    continue;

                if (!IsUnityRelated(component))
                    count++;
            }

            return count;
        }

        private static int GetNonTransformCount(Component[] components)
        {
            if (components == null)
                return 0;

            int count = 0;
            for (int i = 0; i < components.Length; i++)
            {
                if (!(components[i] is UnityEngine.Transform))
                    count++;
            }

            return count;
        }

        private static bool HasMissingScript(Component[] components)
        {
            if (components == null)
                return false;

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                    return true;
            }

            return false;
        }

        private static GUIContent GetContent(GameObject gameObject, Component component, ImprovedHierarchyRuleCategory category)
        {
            if (category == ImprovedHierarchyRuleCategory.MissingScripts)
            {
                GUIContent missing = EditorGUIUtility.IconContent("console.warnicon.sml");
                missing.text = null;
                missing.tooltip = ImprovedHierarchySettings.EnableTooltips ? "Missing Script" : string.Empty;
                return missing;
            }

            if (category == ImprovedHierarchyRuleCategory.NoScripts)
            {
                GUIContent gameObjectContent = EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject));
                gameObjectContent.text = null;
                gameObjectContent.tooltip = ImprovedHierarchySettings.EnableTooltips ? nameof(GameObject) : string.Empty;
                return gameObjectContent;
            }

            if (component == null)
                return new GUIContent();

            System.Type componentType = component.GetType();
            GUIContent content = EditorGUIUtility.ObjectContent(component, componentType);
            content.text = null;
            content.tooltip = ImprovedHierarchySettings.EnableTooltips ? componentType.Name : string.Empty;
            return content;
        }

        private static Component GetPriorityComponent(Component[] components, ImprovedHierarchyRuleCategory category)
        {
            if (components == null)
                return null;

            Component bestComponent = null;
            int bestScore = int.MinValue;

            for (int i = 1; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    continue;

                int score = GetPriorityScore(component, category);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestComponent = component;
            }

            return bestComponent;
        }

        private static int GetPriorityScore(Component component, ImprovedHierarchyRuleCategory category)
        {
            if (component == null)
                return int.MinValue;

            Type componentType = component.GetType();
            string typeName = componentType.Name;
            string namespaceValue = componentType.Namespace ?? string.Empty;
            bool isUserComponent = !IsUnityRelated(component);
            bool hasDefaultScriptIcon = HasDefaultScriptIcon(component);

            if (component is UnityEngine.Animator)
                return 120;
            if (component is ParticleSystem)
                return 116;
            if (component is Camera)
                return 112;
            if (component is Light)
                return 110;
            if (component is AudioSource)
                return 108;
            if (component is Canvas || component is RectTransform)
                return 106;
            if (namespaceValue.StartsWith("UnityEngine.UI", StringComparison.Ordinal))
                return 104;
            if (typeName == "NavMeshAgent")
                return 102;
            if (component is Rigidbody || component is Rigidbody2D)
                return 100;
            if (component is Collider || component is Collider2D || component is CharacterController)
                return 98;
            if (component is SpriteRenderer)
                return 96;
            if (component is SkinnedMeshRenderer || component is MeshRenderer)
                return 94;
            if (component is TrailRenderer || component is LineRenderer)
                return 92;

            if (isUserComponent)
            {
                if (!hasDefaultScriptIcon)
                    return 90;

                return category == ImprovedHierarchyRuleCategory.SingleUserScript ? 88 : 84;
            }

            return 60;
        }

        private static bool HasDefaultScriptIcon(Component component)
        {
            if (component == null)
                return false;

            GUIContent content = EditorGUIUtility.ObjectContent(component, component.GetType());
            Texture image = content.image;
            if (image == null || string.IsNullOrEmpty(image.name))
                return false;

            return image.name.EndsWith("Script Icon", StringComparison.Ordinal);
        }

        private static bool IsPrefab(GameObject gameObject)
        {
            return PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject) != null;
        }

        private static bool IsUnityRelated(Component component)
        {
            if (component == null)
                return false;

            string namespaceValue = component.GetType().Namespace;
            if (string.IsNullOrEmpty(namespaceValue))
                return false;

            switch (ImprovedHierarchySettings.NativeDetection)
            {
                case ImprovedHierarchySettings.UnityNativeDetectionMode.Unity:
                    return namespaceValue.Contains("Unity") || namespaceValue.StartsWith("TMPro");
                case ImprovedHierarchySettings.UnityNativeDetectionMode.UnityEngine:
                    return namespaceValue == nameof(UnityEngine)
                        || namespaceValue == nameof(UnityEditor)
                        || namespaceValue.StartsWith(nameof(UnityEngine) + ".")
                        || namespaceValue.StartsWith(nameof(UnityEditor) + ".")
                        || namespaceValue.StartsWith("TMPro");
                case ImprovedHierarchySettings.UnityNativeDetectionMode.None:
                default:
                    return false;
            }
        }
    }
}
#endif
