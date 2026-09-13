using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EasyProjectViewer.Editor.ProjectWindowThumbnail
{
    /// <summary>
    /// Displays custom thumbnails in Unity's Project Browser window
    /// Generic thumbnail display system that works with any prefab type
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectWindowThumbnailDisplay
    {
        private const int MaxCacheSize = 500;

        private static Dictionary<string, (Texture2D texture, long accessOrder)> thumbnailCache
            = new Dictionary<string, (Texture2D, long)>();
        private static long s_accessCounter;
        private static System.Action reloadCallback;

        static ProjectWindowThumbnailDisplay()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            // Check if thumbnails are enabled
            if (!ProjectWindowThumbnailSettings.Instance.showThumbnails)
            {
                return;
            }

            // Simply draw from cache if available
            if (!thumbnailCache.TryGetValue(guid, out var cached))
            {
                return;
            }

            var thumbnail = cached.texture;

            // Guard against null texture (should not happen but safety check)
            if (thumbnail == null)
            {
                thumbnailCache.Remove(guid);
                return;
            }

            // Update access order for LRU eviction
            thumbnailCache[guid] = (thumbnail, ++s_accessCounter);

            // Calculate icon rect based on project window layout
            Rect iconRect = CalculateIconRect(selectionRect);

            // Draw background to completely hide default prefab icon
            // In list view, limit Y margin to stay within row bounds
            float bgMarginX = 3f;
            float bgMarginY = Mathf.Min(1f, (selectionRect.height - iconRect.height) / 2f);
            Rect bgRect = new Rect(
                iconRect.x - bgMarginX,
                iconRect.y - bgMarginY,
                iconRect.width + bgMarginX * 2f,
                iconRect.height + bgMarginY * 2f
            );

            Color bgColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.76f, 0.76f, 0.76f);
            EditorGUI.DrawRect(bgRect, bgColor);

            // Draw thumbnail
            GUI.DrawTexture(iconRect, thumbnail, ScaleMode.ScaleToFit);

            // Draw Prefab badge (bottom-right) - only in small list view
            // Large/Medium views already show Prefab icon below the thumbnail
            if (selectionRect.height <= 16)
            {
                DrawPrefabBadge(iconRect);
            }
        }

        private static Texture2D cachedPrefabIcon;

        private static void DrawPrefabBadge(Rect iconRect)
        {
            // Get Prefab icon (cached)
            if (cachedPrefabIcon == null)
            {
                string[] iconNames = { "d_Prefab Icon", "Prefab Icon", "d_PrefabNormal Icon", "PrefabNormal Icon" };
                foreach (string iconName in iconNames)
                {
                    var content = EditorGUIUtility.IconContent(iconName);
                    if (content != null && content.image != null)
                    {
                        cachedPrefabIcon = content.image as Texture2D;
                        break;
                    }
                }
                if (cachedPrefabIcon == null) return;
            }

            int badgeSize = 8;
            Rect badgeRect = new Rect(
                iconRect.x + iconRect.width - badgeSize,
                iconRect.y + iconRect.height - badgeSize,
                badgeSize,
                badgeSize
            );

            // Draw Prefab icon only (no background)
            GUI.DrawTexture(badgeRect, cachedPrefabIcon, ScaleMode.ScaleToFit);
        }

        private static Rect CalculateIconRect(Rect selectionRect)
        {
            // Project window has three main size modes:
            // 1. Small (list view): height <= 16
            // 2. Medium (small icon view): 16 < height <= 20
            // 3. Large (grid view): height > 20

            if (selectionRect.height > 20)
            {
                // Large icon view - icon fills most of the rect
                float padding = 4f;
                float size = selectionRect.width - padding * 2;
                size = Mathf.Min(size, selectionRect.height - padding * 2);

                float x = selectionRect.x + (selectionRect.width - size) / 2f;
                float y = selectionRect.y + padding;

                return new Rect(x, y, size, size);
            }
            else if (selectionRect.height > 16)
            {
                // Medium icon view - scale dynamically to fill available space
                float padding = 1f;
                float size = selectionRect.height - padding * 2;
                float y = selectionRect.y + padding;
                return new Rect(selectionRect.x, y, size, size);
            }
            else
            {
                // Small list view - slightly smaller than row height to prevent overlap
                float size = selectionRect.height - 2f;
                float y = selectionRect.y + 1f;
                return new Rect(selectionRect.x, y, size, size);
            }
        }

        /// <summary>
        /// Register a thumbnail for display in project window
        /// Call this from your viewer after generating/loading thumbnails
        /// </summary>
        /// <param name="guid">Asset GUID</param>
        /// <param name="thumbnail">Thumbnail texture</param>
        public static void RegisterThumbnail(string guid, Texture2D thumbnail)
        {
            if (thumbnail != null)
            {
                thumbnailCache[guid] = (thumbnail, ++s_accessCounter);
                EvictIfOverLimit();
                EditorApplication.RepaintProjectWindow();
            }
        }

        /// <summary>
        /// Unregister a thumbnail from display
        /// </summary>
        /// <param name="guid">Asset GUID</param>
        public static void UnregisterThumbnail(string guid)
        {
            if (thumbnailCache.Remove(guid))
            {
                EditorApplication.RepaintProjectWindow();
            }
        }

        /// <summary>
        /// Clear all cached thumbnails from memory
        /// </summary>
        public static void ClearCache()
        {
            thumbnailCache.Clear();
            s_accessCounter = 0;
            EditorApplication.RepaintProjectWindow();
        }

        private static void EvictIfOverLimit()
        {
            // Purge entries with null textures (deleted assets)
            var staleKeys = thumbnailCache
                .Where(kvp => kvp.Value.texture == null)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in staleKeys)
                thumbnailCache.Remove(key);

            // Evict least recently accessed entries
            while (thumbnailCache.Count > MaxCacheSize)
            {
                string oldestKey = null;
                long oldestOrder = long.MaxValue;

                foreach (var kvp in thumbnailCache)
                {
                    if (kvp.Value.accessOrder < oldestOrder)
                    {
                        oldestOrder = kvp.Value.accessOrder;
                        oldestKey = kvp.Key;
                    }
                }

                if (oldestKey == null) break;
                thumbnailCache.Remove(oldestKey);
            }
        }

        /// <summary>
        /// Set callback for reloading thumbnails
        /// Call this from your thumbnail loader to enable reload functionality
        /// </summary>
        /// <param name="callback">Callback to reload all thumbnails</param>
        public static void SetReloadCallback(System.Action callback)
        {
            reloadCallback = callback;
        }

        /// <summary>
        /// Reload all thumbnails using registered callback
        /// </summary>
        public static void ReloadAllThumbnails()
        {
            ClearCache();
            reloadCallback?.Invoke();
        }
    }
}
