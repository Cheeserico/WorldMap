#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

namespace UIViewer.Editor
{
    /// <summary>
    /// Detects when UI Prefabs are modified and clears their preview cache
    /// </summary>
    public class UIPrefabAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Check imported and moved assets for Prefab changes
            List<Object> prefabsToRegenerate = new List<Object>();

            // Check imported assets
            foreach (var assetPath in importedAssets)
            {
                // Skip Packages folder
                if (assetPath.StartsWith("Packages/"))
                    continue;

                if (assetPath.EndsWith(".prefab"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab != null && shouldInvalidatePreview(prefab))
                    {
                        prefabsToRegenerate.Add(prefab);
                    }
                }
            }

            // Check moved assets
            foreach (var assetPath in movedAssets)
            {
                // Skip Packages folder
                if (assetPath.StartsWith("Packages/"))
                    continue;

                if (assetPath.EndsWith(".prefab"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab != null && shouldInvalidatePreview(prefab))
                    {
                        prefabsToRegenerate.Add(prefab);
                    }
                }
            }

            // Regenerate previews for modified UI Prefabs
            if (prefabsToRegenerate.Count > 0)
            {
                UIInspectorPreview.RegeneratePreviewCache(prefabsToRegenerate);
            }
        }

        /// <summary>
        /// Check if this Prefab is a UI element that should have its preview invalidated
        /// </summary>
        private static bool shouldInvalidatePreview(GameObject prefab)
        {
            // Check if root has RectTransform (UI element)
            if (prefab.GetComponent<RectTransform>() != null)
                return true;

            return false;
        }
    }
}
#endif
