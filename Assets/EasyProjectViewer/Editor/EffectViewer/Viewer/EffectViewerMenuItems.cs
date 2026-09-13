using UnityEditor;
using UnityEngine;
using EasyProjectViewer.Editor.ProjectWindowThumbnail;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Project Window context menu items for Effect Viewer
    /// </summary>
    public static class EffectViewerMenuItems
    {
        /// <summary>
        /// Add selected prefab(s) to Effect Viewer
        /// </summary>
        [MenuItem("Assets/Effect Viewer/Add to Viewer", false, 2000)]
        private static void AddToEffectViewer()
        {
            var settings = EffectViewerSettings.Instance;
            int addedCount = 0;

            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject prefab)
                {
                    string assetPath = AssetDatabase.GetAssetPath(prefab);

                    // Only process prefabs
                    if (!assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    string guid = AssetDatabase.AssetPathToGUID(assetPath);

                    // Remove from exclusion list if present
                    if (settings.IsExcluded(guid))
                    {
                        settings.UnexcludePrefab(guid);
                    }

                    // Add to manual list
                    settings.AddManualPrefab(guid);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                // Refresh window if it's open
                var window = EditorWindow.GetWindow<EffectViewerWindow>(false, "Effect Viewer", false);
                if (window != null)
                {
                    window.RefreshAfterAssetChange();
                }
            }
        }

        /// <summary>
        /// Validate menu item (only show for prefabs)
        /// </summary>
        [MenuItem("Assets/Effect Viewer/Add to Viewer", true)]
        private static bool ValidateAddToEffectViewer()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject)
                {
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    if (assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Remove selected prefab(s) from Effect Viewer (unified for both manual and auto-detected)
        /// </summary>
        [MenuItem("Assets/Effect Viewer/Remove from Viewer", false, 2001)]
        private static void RemoveFromEffectViewer()
        {
            var settings = EffectViewerSettings.Instance;
            int removedCount = 0;

            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject prefab)
                {
                    string assetPath = AssetDatabase.GetAssetPath(prefab);

                    if (!assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    string guid = AssetDatabase.AssetPathToGUID(assetPath);

                    // Auto-detect: manually added or auto-detected
                    if (settings.IsManuallyAdded(guid))
                    {
                        // Remove from manual list
                        settings.RemoveManualPrefab(guid);
                    }
                    else
                    {
                        // Add to exclusion list (for auto-detected)
                        settings.ExcludePrefab(guid);
                    }

                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                // Refresh window if it's open
                var window = EditorWindow.GetWindow<EffectViewerWindow>(false, "Effect Viewer", false);
                if (window != null)
                {
                    window.RefreshAfterAssetChange();
                }
            }
        }

        /// <summary>
        /// Validate remove menu item (show for all prefabs)
        /// </summary>
        [MenuItem("Assets/Effect Viewer/Remove from Viewer", true)]
        private static bool ValidateRemoveFromEffectViewer()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject)
                {
                    string assetPath = AssetDatabase.GetAssetPath(obj);
                    if (assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Generate thumbnail for selected effect prefab(s)
        /// </summary>
        [MenuItem("Assets/Effect Viewer/Generate Thumbnail", false, 2010)]
        private static void GenerateThumbnail()
        {
            var selectedAssets = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            if (selectedAssets.Length == 0) return;

            int generatedCount = 0;
            foreach (var prefab in selectedAssets)
            {
                if (!EffectPrefabAutoThumbnail.HasEffectComponents(prefab))
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(prefab);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                EffectPrefabAutoThumbnail.GenerateThumbnailForPrefab(prefab, guid, assetPath);
                generatedCount++;
            }

            EditorApplication.RepaintProjectWindow();

            EditorUtility.DisplayDialog("Thumbnail Generated",
                $"Generated {generatedCount} thumbnail(s)", "OK");
        }

        /// <summary>
        /// Validate GenerateThumbnail menu item (only for effect prefabs)
        /// </summary>
        [MenuItem("Assets/Effect Viewer/Generate Thumbnail", true)]
        private static bool ValidateGenerateThumbnail()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject prefab)
                {
                    string assetPath = AssetDatabase.GetAssetPath(prefab);
                    if (!assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (EffectPrefabAutoThumbnail.HasEffectComponents(prefab))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clear thumbnail for selected effect prefab(s)
        /// </summary>
        [MenuItem("Assets/Effect Viewer/Clear Thumbnail", false, 2011)]
        private static void ClearThumbnail()
        {
            var selectedAssets = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            if (selectedAssets.Length == 0) return;

            int clearedCount = 0;
            foreach (var prefab in selectedAssets)
            {
                string assetPath = AssetDatabase.GetAssetPath(prefab);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                if (EffectThumbnailManager.HasThumbnail(guid))
                {
                    EffectThumbnailManager.ClearThumbnail(guid);
                    clearedCount++;
                }
            }

            EditorUtility.DisplayDialog("Thumbnail Cleared",
                $"Cleared {clearedCount} thumbnail(s)", "OK");
        }

        /// <summary>
        /// Validate ClearThumbnail menu item (only for effect prefabs)
        /// </summary>
        [MenuItem("Assets/Effect Viewer/Clear Thumbnail", true)]
        private static bool ValidateClearThumbnail()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject prefab)
                {
                    string assetPath = AssetDatabase.GetAssetPath(prefab);
                    if (!assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (EffectPrefabAutoThumbnail.HasEffectComponents(prefab))
                        return true;
                }
            }
            return false;
        }
    }
}
