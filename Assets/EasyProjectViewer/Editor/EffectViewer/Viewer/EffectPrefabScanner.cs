using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if VFX_GRAPH_AVAILABLE
using UnityEngine.VFX;
#endif

namespace EffectViewer.Editor
{
    /// <summary>
    /// Scans Prefabs containing ParticleSystem or VFXGraph in the project
    /// Priority: ParticleSystem > VFXGraph
    /// </summary>
    public static class EffectPrefabScanner
    {
        /// <summary>
        /// Scans all Prefabs in the project and lists those containing ParticleSystem or VFXGraph
        /// Priority: ParticleSystem > VFXGraph (treated as ParticleSystem if both are present)
        /// Includes manually added prefabs and excludes excluded prefabs
        /// </summary>
        /// <returns>List of Prefabs containing ParticleSystem or VFXGraph</returns>
        public static List<EffectPrefabData> ScanAllEffectPrefabs()
        {
            var effectPrefabs = new List<EffectPrefabData>();
            var settings = EffectViewerSettings.Instance;
            var processedGuids = new HashSet<string>();

            // First, add manually added prefabs
            foreach (var guid in settings.manuallyAddedPrefabGuids)
            {
                if (settings.IsExcluded(guid)) continue;
                if (processedGuids.Contains(guid)) continue;

                var effectData = LoadManualPrefab(guid);
                if (effectData != null)
                {
                    effectData.IsManuallyAdded = true;
                    effectPrefabs.Add(effectData);
                    processedGuids.Add(guid);
                }
            }

            // Then scan all Prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            EditorUtility.DisplayProgressBar("Effect Viewer", "Scanning Prefabs...", 0f);

            try
            {
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    string guid = prefabGuids[i];

                    // Skip if already processed or excluded
                    if (processedGuids.Contains(guid)) continue;
                    if (settings.IsExcluded(guid)) continue;

                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Update progress bar
                    float progress = (float)i / prefabGuids.Length;
                    EditorUtility.DisplayProgressBar(
                        "Effect Viewer",
                        $"Scanning: {assetPath} ({i + 1}/{prefabGuids.Length})",
                        progress
                    );

                    // Load Prefab
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                    // Create EffectPrefabData (null if prefab doesn't contain effects)
                    var effectData = CreateEffectPrefabData(prefab, guid, assetPath, false);
                    if (effectData == null) continue;

                    // Attempt to restore thumbnail from cache
                    effectData.StaticThumbnail = LoadThumbnailFromCache(guid);

                    effectPrefabs.Add(effectData);
                    processedGuids.Add(guid);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return effectPrefabs;
        }

        /// <summary>
        /// Load manually added prefab (without auto-detection rules)
        /// </summary>
        private static EffectPrefabData LoadManualPrefab(string guid)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return null;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return null;

            // For manual prefabs, create data even without effects on root
            var effectData = new EffectPrefabData
            {
                AssetPath = assetPath,
                Guid = guid,
                Name = prefab.name,
                Prefab = prefab,
                IsManuallyAdded = true
            };

            // Try to detect type
            var particleSystem = prefab.GetComponent<ParticleSystem>();
#if VFX_GRAPH_AVAILABLE
            var visualEffect = prefab.GetComponent<VisualEffect>();
#endif

            if (particleSystem != null)
            {
                effectData.ParticleSystemCount = 1;
                effectData.MaxDuration = CalculateMaxDuration(particleSystem);
                effectData.Type = EffectType.ParticleSystem;
            }
#if VFX_GRAPH_AVAILABLE
            else if (visualEffect != null)
            {
                effectData.ParticleSystemCount = 1;
                effectData.MaxDuration = CalculateVFXDuration(visualEffect);
                effectData.Type = EffectType.VFXGraph;
            }
            else
#else
            else
#endif
            {
                // Even without effects on root, allow manual addition
                effectData.Type = EffectType.Unknown;
                effectData.MaxDuration = 2f; // Default duration
            }

            // Load thumbnail
            effectData.StaticThumbnail = LoadThumbnailFromCache(guid);

            return effectData;
        }

        /// <summary>
        /// Create EffectPrefabData from GameObject
        /// </summary>
        /// <param name="prefab">Prefab GameObject</param>
        /// <param name="guid">Asset GUID</param>
        /// <param name="assetPath">Asset path</param>
        /// <param name="isManuallyAdded">Whether this prefab is manually added</param>
        /// <returns>EffectPrefabData or null if prefab doesn't contain effects</returns>
        private static EffectPrefabData CreateEffectPrefabData(GameObject prefab, string guid, string assetPath, bool isManuallyAdded = false)
        {
            if (prefab == null) return null;

            // Priority: ParticleSystem > VFXGraph
            // Check only the root GameObject, not children
            var particleSystem = prefab.GetComponent<ParticleSystem>();
#if VFX_GRAPH_AVAILABLE
            var visualEffect = prefab.GetComponent<VisualEffect>();
#else
            Component visualEffect = null;
#endif

            // Skip if neither ParticleSystem nor VFX is present on the root
            if (particleSystem == null && visualEffect == null)
            {
                return null;
            }

            // Create EffectPrefabData
            var effectData = new EffectPrefabData
            {
                AssetPath = assetPath,
                Guid = guid,
                Name = prefab.name,
                Prefab = prefab,
                IsManuallyAdded = isManuallyAdded
            };

            // ParticleSystem has priority
            if (particleSystem != null)
            {
                effectData.ParticleSystemCount = 1;
                effectData.MaxDuration = CalculateMaxDuration(particleSystem);
                effectData.Type = EffectType.ParticleSystem;
            }
#if VFX_GRAPH_AVAILABLE
            else if (visualEffect != null)
            {
                effectData.ParticleSystemCount = 1; // VFX count
                effectData.MaxDuration = CalculateVFXDuration(visualEffect);
                effectData.Type = EffectType.VFXGraph;
            }
#endif
            else
            {
                effectData.Type = EffectType.Unknown;
            }

            return effectData;
        }

        /// <summary>
        /// Load thumbnail from cache
        /// </summary>
        /// <param name="guid">Prefab GUID</param>
        /// <returns>Cached thumbnail (null if not exists)</returns>
        private static Texture2D LoadThumbnailFromCache(string guid)
        {
            return EffectThumbnailManager.LoadThumbnail(guid);
        }

        /// <summary>
        /// Calculate Duration of ParticleSystem
        /// </summary>
        private static float CalculateMaxDuration(ParticleSystem particleSystem)
        {
            if (particleSystem == null) return 0f;

            float duration;
            if (particleSystem.main.loop)
            {
                // For looping: one cycle length
                duration = particleSystem.main.duration;
            }
            else
            {
                // For non-looping: duration + lifetime
                duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
            }

            return duration;
        }

#if VFX_GRAPH_AVAILABLE
        /// <summary>
        /// Calculate Duration of VFXGraph (estimated value)
        /// Uses fixed value since VFXGraph has no direct duration property
        /// </summary>
        private static float CalculateVFXDuration(VisualEffect visualEffect)
        {
            // VFXGraph duration is defined within the asset,
            // so we return a common value of 5 seconds here
            // VFX asset parsing can be implemented if needed
            return 5f;
        }
#endif

        /// <summary>
        /// Scan only Prefabs within specific folder
        /// </summary>
        /// <param name="folderPath">Folder path (e.g., "Assets/Effects")</param>
        /// <returns>List of Prefabs containing ParticleSystem</returns>
        public static List<EffectPrefabData> ScanEffectPrefabsInFolder(string folderPath)
        {
            var effectPrefabs = new List<EffectPrefabData>();

            // Search Prefabs in specified folder
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (string guid in prefabGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                // Create EffectPrefabData (null if prefab doesn't contain effects)
                var effectData = CreateEffectPrefabData(prefab, guid, assetPath);
                if (effectData == null) continue;

                effectData.StaticThumbnail = AssetPreview.GetAssetPreview(prefab);

                effectPrefabs.Add(effectData);
            }

            return effectPrefabs;
        }

        /// <summary>
        /// Search by Prefab name (partial match)
        /// </summary>
        /// <param name="allEffects">All effects list</param>
        /// <param name="searchText">Search text</param>
        /// <returns>Filtered list</returns>
        public static List<EffectPrefabData> FilterByName(List<EffectPrefabData> allEffects, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return allEffects;

            return allEffects
                .Where(e => e.Name.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        /// <summary>
        /// Filter by folder path
        /// </summary>
        /// <param name="allEffects">All effects list</param>
        /// <param name="folderPath">Folder path</param>
        /// <returns>Filtered list</returns>
        public static List<EffectPrefabData> FilterByFolder(List<EffectPrefabData> allEffects, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return allEffects;

            return allEffects
                .Where(e => e.AssetPath.StartsWith(folderPath, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
