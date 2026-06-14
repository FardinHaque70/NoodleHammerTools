#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
        private const double CacheLifetimeSeconds = 0.1;

        private struct CachedClassification
        {
            public ImprovedHierarchyIconInfo Result;
            public double Timestamp;
        }

        /// <summary>
        /// Intermediate analysis results from a single-pass scan of a component array.
        /// Avoids allocating separate arrays or doing multiple passes.
        /// </summary>
        private struct ComponentScanResult
        {
            public bool HasMissingScript;
            public int NonTransformCount;
            public int UserScriptCount;
            public Component FirstNonTransform;
            public Component FirstUserComponent;
        }

        private static readonly Dictionary<int, CachedClassification> ClassificationCache = new();

        static ImprovedHierarchyClassifier()
        {
            EditorApplication.hierarchyChanged += InvalidateCache;
            Undo.undoRedoPerformed += InvalidateCache;
        }

        /// <summary>
        /// Clears the entire classification cache.
        /// </summary>
        public static void InvalidateCache()
        {
            ClassificationCache.Clear();
        }

        /// <summary>
        /// Removes a single entry from the classification cache.
        /// </summary>
        public static void InvalidateObject(int instanceId)
        {
            ClassificationCache.Remove(instanceId);
        }

        public static ImprovedHierarchyIconInfo Analyze(GameObject gameObject)
        {
            int instanceId = gameObject.GetInstanceID();

            // Check cache -- use EditorApplication.timeSinceStartup which ticks reliably in Edit mode
            if (ClassificationCache.TryGetValue(instanceId, out CachedClassification cached))
            {
                double age = EditorApplication.timeSinceStartup - cached.Timestamp;
                if (age >= 0.0 && age < CacheLifetimeSeconds)
                    return cached.Result;
            }

            Component[] components = gameObject.GetComponents<Component>();
            if (components == null || components.Length == 0)
                return default;

            // Single-pass scan of the component array
            ComponentScanResult scan = ScanComponents(components);

            ImprovedHierarchyRuleCategory category = Classify(scan.HasMissingScript, scan.NonTransformCount, scan.UserScriptCount);
            Component displayComponent = ResolveDisplayComponent(
                gameObject,
                components,
                scan.FirstNonTransform,
                scan.FirstUserComponent,
                category);
            GUIContent content = GetContent(gameObject, displayComponent, category);

            ImprovedHierarchySettings.IconDisplayMode iconMode = ImprovedHierarchySettings.GetIconMode(category);
            if (ImprovedHierarchySettings.OverridePrefabIcons && IsPrefab(gameObject))
                iconMode = ImprovedHierarchySettings.PrefabIconMode;

            ImprovedHierarchyIconInfo result = new ImprovedHierarchyIconInfo(content, iconMode, category, scan.HasMissingScript, scan.UserScriptCount);

            ClassificationCache[instanceId] = new CachedClassification
            {
                Result = result,
                Timestamp = EditorApplication.timeSinceStartup
            };

            return result;
        }

        /// <summary>
        /// Single-pass scan: computes HasMissingScript, NonTransformCount, UserScriptCount,
        /// FirstNonTransform, and FirstUserComponent all in one iteration.
        /// </summary>
        private static ComponentScanResult ScanComponents(Component[] components)
        {
            ComponentScanResult scan = default;

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                if (component == null)
                {
                    scan.HasMissingScript = true;
                    continue;
                }

                if (component is UnityEngine.Transform)
                    continue;

                scan.NonTransformCount++;

                if (scan.FirstNonTransform == null)
                    scan.FirstNonTransform = component;

                if (!IsUnityRelated(component))
                {
                    scan.UserScriptCount++;
                    if (scan.FirstUserComponent == null)
                        scan.FirstUserComponent = component;
                }
            }

            return scan;
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

        /// <summary>
        /// Creates an owned copy of GUIContent so cached entries aren't invalidated by
        /// Unity's internal ObjectContent/IconContent pooling.
        /// </summary>
        private static GUIContent GetContent(GameObject gameObject, Component component, ImprovedHierarchyRuleCategory category)
        {
            if (category == ImprovedHierarchyRuleCategory.MissingScripts)
            {
                GUIContent missing = EditorGUIUtility.IconContent("console.warnicon.sml");
                string tooltip = ImprovedHierarchySettings.EnableTooltips ? "Missing Script" : string.Empty;
                return new GUIContent(missing.image, tooltip);
            }

            if (category == ImprovedHierarchyRuleCategory.NoScripts)
            {
                GUIContent gameObjectContent = EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject));
                string tooltip = ImprovedHierarchySettings.EnableTooltips ? nameof(GameObject) : string.Empty;
                return new GUIContent(gameObjectContent.image, tooltip);
            }

            if (component == null)
                return new GUIContent();

            Type componentType = component.GetType();
            GUIContent content = EditorGUIUtility.ObjectContent(component, componentType);
            string componentTooltip = ImprovedHierarchySettings.EnableTooltips ? componentType.Name : string.Empty;
            return new GUIContent(content.image, componentTooltip);
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

            // Type checks via 'is' are fast (single vtable lookup), do these first
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

            // String-based checks only when is-checks didn't match
            Type componentType = component.GetType();
            string namespaceValue = componentType.Namespace ?? string.Empty;

            if (namespaceValue.StartsWith("UnityEngine.UI", StringComparison.Ordinal))
                return 104;
            if (componentType.Name == "NavMeshAgent")
                return 102;

            bool isUserComponent = !IsUnityRelatedByNamespace(namespaceValue);

            if (isUserComponent)
            {
                bool hasDefaultScriptIcon = HasDefaultScriptIcon(component, componentType);
                if (!hasDefaultScriptIcon)
                    return 90;

                return category == ImprovedHierarchyRuleCategory.SingleUserScript ? 88 : 84;
            }

            return 60;
        }

        private static bool HasDefaultScriptIcon(Component component, Type componentType)
        {
            if (component == null)
                return false;

            GUIContent content = EditorGUIUtility.ObjectContent(component, componentType);
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
            return !string.IsNullOrEmpty(namespaceValue) && IsUnityRelatedByNamespace(namespaceValue);
        }

        private static bool IsUnityRelatedByNamespace(string namespaceValue)
        {
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
