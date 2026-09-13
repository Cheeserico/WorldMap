using System.IO;
using UnityEditor;
using UnityEngine;
using EasyProjectViewer.Editor.ProjectWindowThumbnail;

namespace UIViewer.Editor
{
    /// <summary>
    /// Static class for UI thumbnail management
    /// Manages saving and loading as individual JPG files
    /// </summary>
    public static class UIThumbnailManager
    {
        private const int JPG_QUALITY = 85;

        private static string s_CachedThumbnailPath;

        /// <summary>
        /// Get thumbnail cache folder path (dynamic resolution, cached after first call)
        /// </summary>
        internal static string GetThumbnailCachePath()
        {
            if (!string.IsNullOrEmpty(s_CachedThumbnailPath))
                return s_CachedThumbnailPath;

            // Try to find existing thumbnail folder by searching for any thumbnail
            string[] existingThumbnails = AssetDatabase.FindAssets("t:Texture2D UIThumbnailImage");
            if (existingThumbnails.Length > 0)
            {
                string thumbnailPath = AssetDatabase.GUIDToAssetPath(existingThumbnails[0]);
                s_CachedThumbnailPath = Path.GetDirectoryName(thumbnailPath).Replace("\\", "/");
                return s_CachedThumbnailPath;
            }

            // Find UIThumbnailManager script to determine root folder
            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript UIThumbnailManager");
            string rootFolder = "Assets/EasyProjectViewer"; // Default fallback
            if (scriptGuids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                string scriptDir = Path.GetDirectoryName(scriptPath);
                // Navigate: Viewer(current) -> UIViewer -> Editor -> EasyProjectViewer
                rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(scriptDir))).Replace("\\", "/");
            }

            s_CachedThumbnailPath = Path.Combine(rootFolder, "UIThumbnailImage").Replace("\\", "/");
            return s_CachedThumbnailPath;
        }

        /// <summary>
        /// Initialize thumbnail cache folder (create if doesn't exist)
        /// </summary>
        public static void Initialize()
        {
            string thumbnailCachePath = GetThumbnailCachePath();
            if (!AssetDatabase.IsValidFolder(thumbnailCachePath))
            {
                // Create directory structure
                string[] folderParts = thumbnailCachePath.Split('/');
                string currentPath = folderParts[0];
                for (int i = 1; i < folderParts.Length; i++)
                {
                    string nextPath = currentPath + "/" + folderParts[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folderParts[i]);
                    }
                    currentPath = nextPath;
                }
            }
        }

        /// <summary>
        /// Save thumbnail
        /// </summary>
        /// <param name="guid">Prefab GUID</param>
        /// <param name="texture">Texture2D to save</param>
        public static void SaveThumbnail(string guid, Texture2D texture)
        {
            Initialize();

            if (texture == null)
            {
                Debug.LogWarning($"[UI Viewer] Cannot save null texture for {guid}");
                return;
            }

            try
            {
                // Convert Texture2D to JPG byte array (quality 85)
                byte[] jpgData = texture.EncodeToJPG(JPG_QUALITY);

                if (jpgData == null || jpgData.Length == 0)
                {
                    Debug.LogWarning($"[UI Viewer] Failed to encode texture to JPG for {guid}");
                    return;
                }

                // Generate file path
                string filePath = GetThumbnailPath(guid);

                // Save to file system
                File.WriteAllBytes(filePath, jpgData);

                // Import to Unity Asset Database and apply settings
                AssetDatabase.ImportAsset(filePath);
                ApplyTextureImportSettings(filePath);

                // Register with ProjectWindowThumbnailDisplay for display
                Texture2D loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                if (loadedTexture != null)
                {
                    ProjectWindowThumbnailDisplay.RegisterThumbnail(guid, loadedTexture);
                }
                else
                {
                    Debug.LogWarning($"[UI Viewer] Failed to load thumbnail after saving for {guid}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UI Viewer] Failed to save thumbnail for {guid}: {e.Message}");
            }
        }

        /// <summary>
        /// Load thumbnail
        /// </summary>
        /// <param name="guid">Prefab GUID</param>
        /// <returns>Loaded Texture2D (null if doesn't exist)</returns>
        public static Texture2D LoadThumbnail(string guid)
        {
            string filePath = GetThumbnailPath(guid);

            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                // Load from Asset Database
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
                return texture;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UI Viewer] Failed to load thumbnail for {guid}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if thumbnail exists
        /// </summary>
        /// <param name="guid">Prefab GUID</param>
        /// <returns>True if thumbnail exists</returns>
        public static bool HasThumbnail(string guid)
        {
            string filePath = GetThumbnailPath(guid);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Delete specific thumbnail
        /// </summary>
        /// <param name="guid">Prefab GUID</param>
        public static void ClearThumbnail(string guid)
        {
            string filePath = GetThumbnailPath(guid);

            if (File.Exists(filePath))
            {
                AssetDatabase.DeleteAsset(filePath);

                // Unregister from ProjectWindowThumbnailDisplay
                ProjectWindowThumbnailDisplay.UnregisterThumbnail(guid);
            }
        }

        /// <summary>
        /// Delete all thumbnails
        /// </summary>
        public static void ClearAllThumbnails()
        {
            string thumbnailCachePath = GetThumbnailCachePath();
            if (!Directory.Exists(thumbnailCachePath))
            {
                Debug.LogWarning("[UI Viewer] Thumbnail cache folder does not exist.");
                return;
            }

            try
            {
                string[] jpgFiles = Directory.GetFiles(thumbnailCachePath, "*.jpg");
                int count = 0;

                foreach (string file in jpgFiles)
                {
                    string assetPath = file.Replace("\\", "/");
                    AssetDatabase.DeleteAsset(assetPath);
                    count++;
                }

                AssetDatabase.Refresh();

                // Clear ProjectWindowThumbnailDisplay cache
                ProjectWindowThumbnailDisplay.ClearCache();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UI Viewer] Failed to clear thumbnails: {e.Message}");
            }
        }


        /// <summary>
        /// Remove thumbnail files whose GUID no longer maps to an existing prefab asset.
        /// Called after prefab deletion to prevent orphaned thumbnails from accumulating.
        /// </summary>
        public static void CleanupOrphanedThumbnails()
        {
            string thumbnailCachePath = GetThumbnailCachePath();
            if (!Directory.Exists(thumbnailCachePath))
                return;

            string[] jpgFiles = Directory.GetFiles(thumbnailCachePath, "*.jpg");
            int removedCount = 0;

            foreach (string file in jpgFiles)
            {
                string guid = Path.GetFileNameWithoutExtension(file);
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // If GUID doesn't map to any asset, or the asset no longer exists, remove the thumbnail
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
                {
                    string assetFilePath = file.Replace("\\", "/");
                    AssetDatabase.DeleteAsset(assetFilePath);
                    ProjectWindowThumbnailDisplay.UnregisterThumbnail(guid);
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                Debug.Log($"[UI Viewer] Cleaned up {removedCount} orphaned thumbnail(s).");
            }
        }

        /// <summary>
        /// Get cache size (in MB)
        /// </summary>
        /// <returns>Cache size (MB)</returns>
        public static float GetCacheSizeMB()
        {
            string thumbnailCachePath = GetThumbnailCachePath();
            if (!Directory.Exists(thumbnailCachePath))
            {
                return 0f;
            }

            try
            {
                string[] jpgFiles = Directory.GetFiles(thumbnailCachePath, "*.jpg");
                long totalBytes = 0;

                foreach (string file in jpgFiles)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    totalBytes += fileInfo.Length;
                }

                return totalBytes / (1024f * 1024f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UI Viewer] Failed to calculate cache size: {e.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// Get thumbnail count
        /// </summary>
        /// <returns>Number of saved thumbnails</returns>
        public static int GetThumbnailCount()
        {
            string thumbnailCachePath = GetThumbnailCachePath();
            if (!Directory.Exists(thumbnailCachePath))
            {
                return 0;
            }

            try
            {
                string[] jpgFiles = Directory.GetFiles(thumbnailCachePath, "*.jpg");
                return jpgFiles.Length;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UI Viewer] Failed to count thumbnails: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get thumbnail file path
        /// </summary>
        /// <param name="guid">Prefab GUID</param>
        /// <returns>File path</returns>
        private static string GetThumbnailPath(string guid)
        {
            return $"{GetThumbnailCachePath()}/{guid}.jpg";
        }

        /// <summary>
        /// Apply TextureImporter settings
        /// </summary>
        /// <param name="assetPath">Asset path</param>
        private static void ApplyTextureImportSettings(string assetPath)
        {
            try
            {
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    bool needsReimport = false;
                    if (importer.textureType != TextureImporterType.Default)
                    { importer.textureType = TextureImporterType.Default; needsReimport = true; }
                    if (importer.isReadable)
                    { importer.isReadable = false; needsReimport = true; }
                    if (importer.maxTextureSize != 128)
                    { importer.maxTextureSize = 128; needsReimport = true; }
                    if (importer.textureCompression != TextureImporterCompression.Compressed)
                    { importer.textureCompression = TextureImporterCompression.Compressed; needsReimport = true; }

                    if (needsReimport)
                        importer.SaveAndReimport();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UI Viewer] Failed to apply texture import settings: {e.Message}");
            }
        }
    }
}
