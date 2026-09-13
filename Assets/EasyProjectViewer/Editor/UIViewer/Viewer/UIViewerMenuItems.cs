using UnityEditor;
using UnityEngine;

namespace UIViewer.Editor
{
    /// <summary>
    /// Menu items for UI Viewer
    /// Provides options to generate, clear, and manage UI thumbnails
    /// </summary>
    public static class UIViewerMenuItems
    {
        /// <summary>
        /// Generate thumbnail for selected UI prefab
        /// </summary>
        [MenuItem("Assets/UI Viewer/Generate Thumbnail", false, 2020)]
        public static void GenerateThumbnail()
        {
            // Get selected prefab from assets
            var selectedAssets = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            if (selectedAssets.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a UI prefab", "OK");
                return;
            }

            foreach (var prefab in selectedAssets)
            {
                GenerateThumbnailForPrefab(prefab);
            }

            EditorUtility.DisplayDialog("Thumbnail Generated",
                $"Generated {selectedAssets.Length} thumbnail(s)", "OK");
        }

        /// <summary>
        /// Validate GenerateThumbnail menu item
        /// </summary>
        [MenuItem("Assets/UI Viewer/Generate Thumbnail", true)]
        public static bool ValidateGenerateThumbnail()
        {
            var selectedAssets = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            return selectedAssets.Length > 0;
        }

        /// <summary>
        /// Clear thumbnail for selected UI prefab
        /// </summary>
        [MenuItem("Assets/UI Viewer/Clear Thumbnail", false, 2021)]
        public static void ClearThumbnail()
        {
            var selectedAssets = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            if (selectedAssets.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a UI prefab", "OK");
                return;
            }

            int clearedCount = 0;
            foreach (var prefab in selectedAssets)
            {
                string assetPath = AssetDatabase.GetAssetPath(prefab);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                if (UIThumbnailManager.HasThumbnail(guid))
                {
                    UIThumbnailManager.ClearThumbnail(guid);
                    clearedCount++;
                }
            }

            EditorUtility.DisplayDialog("Thumbnail Cleared",
                $"Cleared {clearedCount} thumbnail(s)", "OK");
        }

        /// <summary>
        /// Validate ClearThumbnail menu item
        /// </summary>
        [MenuItem("Assets/UI Viewer/Clear Thumbnail", true)]
        public static bool ValidateClearThumbnail()
        {
            var selectedAssets = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            return selectedAssets.Length > 0;
        }

        /// <summary>
        /// Generate thumbnail for a single prefab
        /// </summary>
        private static void GenerateThumbnailForPrefab(GameObject prefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

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
                    Debug.Log($"[UI Viewer] Generated thumbnail for: {prefab.name}");
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
                Debug.LogError($"[UI Viewer] Error generating thumbnail for {prefab.name}: {e.Message}");
            }
        }
    }
}
