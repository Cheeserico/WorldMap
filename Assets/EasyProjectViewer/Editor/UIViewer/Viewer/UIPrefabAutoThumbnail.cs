using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIViewer.Editor
{
    /// <summary>
    /// Automatically generates thumbnails for UI prefabs on import/modification
    /// Uses AssetPostprocessor to detect prefab changes
    /// </summary>
    public class UIPrefabAutoThumbnail : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Check if auto-generation is enabled
            if (!UIViewerSettings.Instance.autoGenerateThumbnails)
            {
                return;
            }

            // Process imported/modified prefabs
            foreach (string assetPath in importedAssets)
            {
                ProcessPrefab(assetPath);
            }

            // Process moved prefabs (regenerate with new GUID mapping)
            foreach (string assetPath in movedAssets)
            {
                ProcessPrefab(assetPath);
            }

            // Process deleted prefabs — clean up orphaned thumbnails
            bool hasDeletedPrefabs = false;
            foreach (string assetPath in deletedAssets)
            {
                if (assetPath.EndsWith(".prefab"))
                {
                    hasDeletedPrefabs = true;
                    break;
                }
            }

            if (hasDeletedPrefabs)
            {
                // AssetPathToGUID is unreliable for deleted assets,
                // so scan thumbnail folder and remove any that no longer have a matching prefab
                EditorApplication.delayCall += UIThumbnailManager.CleanupOrphanedThumbnails;
            }
        }

        /// <summary>
        /// Process a prefab for thumbnail generation
        /// </summary>
        private static void ProcessPrefab(string assetPath)
        {
            // Only process prefabs in Assets folder, not in Packages
            if (assetPath.StartsWith("Packages/"))
            {
                return;
            }

            // Only process prefab assets
            if (!assetPath.EndsWith(".prefab"))
            {
                return;
            }

            // Load prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return;
            }

            // Check if prefab is a UI prefab
            if (!IsUIPrefab(prefab))
            {
                return;
            }

            // Get prefab GUID
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            // Always regenerate on import/modification (covers both new and updated prefabs)
            GenerateThumbnailForPrefab(prefab, guid, assetPath);
        }

        /// <summary>
        /// Check if GameObject is a UI prefab
        /// </summary>
        private static bool IsUIPrefab(GameObject prefab)
        {
            // Must have RectTransform on root
            var rectTransform = prefab.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return false;
            }

            // Must have Canvas or UI components
            var canvas = prefab.GetComponent<Canvas>();
            if (canvas != null)
            {
                return true;
            }

            // Check for UI components
            if (prefab.GetComponentInChildren<Image>() != null)
            {
                return true;
            }

            if (prefab.GetComponentInChildren<RawImage>() != null)
            {
                return true;
            }

            if (prefab.GetComponentInChildren<Button>() != null)
            {
                return true;
            }

            if (prefab.GetComponentInChildren<Text>() != null)
            {
                return true;
            }

            if (prefab.GetComponentInChildren<TMP_Text>() != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Generate and save thumbnail for a UI prefab
        /// </summary>
        private static void GenerateThumbnailForPrefab(GameObject prefab, string guid, string assetPath)
        {
            try
            {
                // Initialize generator
                UIThumbnailGenerator generator = new UIThumbnailGenerator();
                generator.Initialize();

                // Generate thumbnail
                Texture2D thumbnail = generator.GenerateStaticThumbnail(prefab);

                // Save thumbnail
                if (thumbnail != null)
                {
                    UIThumbnailManager.SaveThumbnail(guid, thumbnail);
                }
                else
                {
                    Debug.LogWarning($"[UI Viewer] Failed to generate thumbnail for: {prefab.name}");
                }

                // Cleanup
                generator.Cleanup();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UI Viewer] Error generating thumbnail for {assetPath}: {e.Message}");
            }
        }

        /// <summary>
        /// Batch regenerate all UI prefab thumbnails in project
        /// </summary>
        [MenuItem("Tools/UIViewer/Regenerate All UI Thumbnails")]
        public static void RegenerateAllThumbnails()
        {
            // Find all prefabs in project
            string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int processedCount = 0;
            int generatedCount = 0;

            EditorUtility.DisplayProgressBar("Regenerating UI Thumbnails", "Scanning prefabs...", 0f);

            try
            {
                for (int i = 0; i < allPrefabGuids.Length; i++)
                {
                    string guid = allPrefabGuids[i];
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Skip Packages folder
                    if (assetPath.StartsWith("Packages/"))
                    {
                        continue;
                    }

                    // Update progress bar
                    float progress = (float)i / allPrefabGuids.Length;
                    EditorUtility.DisplayProgressBar("Regenerating UI Thumbnails",
                        $"Processing: {assetPath} ({i + 1}/{allPrefabGuids.Length})",
                        progress);

                    // Load prefab
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab == null)
                    {
                        continue;
                    }

                    // Check if UI prefab
                    if (!IsUIPrefab(prefab))
                    {
                        continue;
                    }

                    processedCount++;

                    // Generate thumbnail
                    GenerateThumbnailForPrefab(prefab, guid, assetPath);
                    generatedCount++;
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
            Debug.Log($"[UI Viewer] Thumbnail regeneration complete:\n" +
                      $"  Processed: {processedCount} UI prefabs\n" +
                      $"  Generated: {generatedCount} thumbnails");

            EditorUtility.DisplayDialog("UI Thumbnail Regeneration Complete",
                $"Processed {processedCount} UI prefabs\nGenerated {generatedCount} thumbnails",
                "OK");
        }
    }
}
