using UnityEditor;
using UnityEngine;
using EasyProjectViewer.Editor.ProjectWindowThumbnail;
#if VFX_GRAPH_AVAILABLE
using UnityEngine.VFX;
#endif

namespace EffectViewer.Editor
{
    /// <summary>
    /// Automatically generates thumbnails for effect prefabs on import/modification
    /// Uses AssetPostprocessor to detect prefab changes
    /// </summary>
    public class EffectPrefabAutoThumbnail : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Check if auto-generation is enabled
            if (!EffectViewerSettings.Instance.autoGenerateThumbnails)
            {
                return;
            }

            // Process imported/modified prefabs
            foreach (string assetPath in importedAssets)
            {
                if (ProcessPrefab(assetPath, out string guid))
                {
                    // Notify windows about new effect prefab
                    string capturedGuid = guid;
                    EditorApplication.delayCall += () =>
                        EffectViewerWindowRefresher.NotifyPrefabAdded(capturedGuid);
                }
            }

            // Process moved prefabs (regenerate with new GUID mapping)
            for (int i = 0; i < movedAssets.Length; i++)
            {
                ProcessPrefab(movedAssets[i], out _);
            }

        }

        /// <summary>
        /// Process a prefab for thumbnail generation
        /// </summary>
        /// <param name="assetPath">Asset path</param>
        /// <param name="guid">Output GUID if processed as new effect prefab</param>
        /// <returns>True if this is a new effect prefab that was processed</returns>
        private static bool ProcessPrefab(string assetPath, out string guid)
        {
            guid = null;

            // Only process prefab assets
            if (!assetPath.EndsWith(".prefab"))
            {
                return false;
            }

            // Load prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return false;
            }

            // Check if prefab has effect components
            bool hasEffects = HasEffectComponents(prefab);
            if (!hasEffects)
            {
                return false;
            }

            // Get prefab GUID
            guid = AssetDatabase.AssetPathToGUID(assetPath);

            // Check if thumbnail already exists
            bool isNewPrefab = !EffectThumbnailManager.HasThumbnail(guid);

            // Generate thumbnail for new prefabs
            if (isNewPrefab)
            {
                GenerateThumbnailForPrefab(prefab, guid, assetPath);
            }

            return isNewPrefab;
        }

        internal static bool HasEffectComponents(GameObject prefab)
        {
            // Check for ParticleSystem (root object only)
            if (prefab.GetComponent<ParticleSystem>() != null)
            {
                return true;
            }

#if VFX_GRAPH_AVAILABLE
            // Check for VFX Graph (root object only)
            if (prefab.GetComponent<VisualEffect>() != null)
            {
                return true;
            }
#endif

            return false;
        }

        internal static void GenerateThumbnailForPrefab(GameObject prefab, string guid, string assetPath)
        {
            try
            {
                int size = EffectViewerSettings.Instance.thumbnailSize;

#if VFX_GRAPH_AVAILABLE
                // VFX-only prefab → deferred capture (GPU particles need multi-frame warmup)
                if (IsVFXOnlyPrefab(prefab))
                {
                    VFXDeferredThumbnailCapture.Enqueue(guid, assetPath, prefab, size);
                    return;
                }
#endif

                // ParticleSystem (or mixed) → synchronous path
                EffectThumbnailGenerator generator = new EffectThumbnailGenerator();
                generator.Initialize();
                Texture2D thumbnail = generator.GenerateStaticThumbnail(prefab, size, size);

                // Save thumbnail
                if (thumbnail != null)
                {
                    EffectThumbnailManager.SaveThumbnail(guid, thumbnail);
                }
                else
                {
                    Debug.LogWarning($"[Effect Viewer] Failed to generate thumbnail for: {prefab.name}");
                }

                // Cleanup
                generator.Cleanup();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Effect Viewer] Error generating thumbnail for {assetPath}: {e.Message}");
            }
        }

#if VFX_GRAPH_AVAILABLE
        private static bool IsVFXOnlyPrefab(GameObject prefab)
        {
            bool hasParticle = prefab.GetComponentInChildren<ParticleSystem>(true) != null;
            bool hasVFX = prefab.GetComponentInChildren<VisualEffect>(true) != null;
            return hasVFX && !hasParticle;
        }
#endif

        /// <summary>
        /// Batch regenerate all effect prefab thumbnails in project
        /// </summary>
        [MenuItem("Tools/EffectViewer/Regenerate All Thumbnails")]
        public static void RegenerateAllThumbnails()
        {
            // Find all prefabs in project
            string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int processedCount = 0;
            int generatedCount = 0;

            EditorUtility.DisplayProgressBar("Regenerating Thumbnails", "Scanning prefabs...", 0f);

            try
            {
                for (int i = 0; i < allPrefabGuids.Length; i++)
                {
                    string guid = allPrefabGuids[i];
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Update progress bar
                    float progress = (float)i / allPrefabGuids.Length;
                    EditorUtility.DisplayProgressBar("Regenerating Thumbnails",
                        $"Processing: {assetPath} ({i + 1}/{allPrefabGuids.Length})",
                        progress);

                    // Load prefab
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab == null)
                    {
                        continue;
                    }

                    // Check if effect prefab
                    if (!HasEffectComponents(prefab))
                    {
                        continue;
                    }

                    processedCount++;

                    // Generate thumbnail using deferred or synchronous method as appropriate
#if VFX_GRAPH_AVAILABLE
                    if (IsVFXOnlyPrefab(prefab))
                    {
                        int size = EffectViewerSettings.Instance.thumbnailSize;
                        VFXDeferredThumbnailCapture.Enqueue(guid, assetPath, prefab, size);
                        generatedCount++;
                    }
                    else
                    {
                        GenerateThumbnailForPrefab(prefab, guid, assetPath);
                        generatedCount++;
                    }
#else
                    GenerateThumbnailForPrefab(prefab, guid, assetPath);
                    generatedCount++;
#endif
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Repaint to show newly registered thumbnails
            // (SaveThumbnail already calls RegisterThumbnail for each one)
            EditorApplication.RepaintProjectWindow();

            // Show results
            Debug.Log($"[Effect Viewer] Thumbnail regeneration complete:\n" +
                      $"  Processed: {processedCount} effect prefabs\n" +
                      $"  Generated: {generatedCount} thumbnails");

            EditorUtility.DisplayDialog("Thumbnail Regeneration Complete",
                $"Processed {processedCount} effect prefabs\nGenerated {generatedCount} thumbnails",
                "OK");
        }
    }
}
