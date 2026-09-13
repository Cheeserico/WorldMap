using UnityEditor;
using UnityEngine;
using System.IO;
using EasyProjectViewer.Editor.ProjectWindowThumbnail;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Loads all Effect Viewer thumbnails on Unity startup
    /// Registers them with ProjectWindowThumbnailDisplay for display
    /// </summary>
    [InitializeOnLoad]
    public static class EffectViewerThumbnailLoader
    {
        static EffectViewerThumbnailLoader()
        {
            // Register reload callback
            ProjectWindowThumbnailDisplay.SetReloadCallback(LoadAllThumbnails);

            // Load all thumbnails on startup
            LoadAllThumbnails();
        }

        public static void LoadAllThumbnails()
        {
            string thumbnailCachePath = EffectThumbnailManager.GetThumbnailCachePath();

            // Check if thumbnail directory exists
            if (!Directory.Exists(thumbnailCachePath))
            {
                return;
            }

            // Get all JPG thumbnail files
            string[] thumbnailFiles = Directory.GetFiles(thumbnailCachePath, "*.jpg");

            int loadedCount = 0;
            foreach (string filePath in thumbnailFiles)
            {
                try
                {
                    // Extract GUID from filename (without .jpg extension)
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string guid = fileName;

                    // Load thumbnail texture
                    string assetPath = filePath.Replace("\\", "/");
                    Texture2D thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                    if (thumbnail != null)
                    {
                        // Register with ProjectWindowThumbnailDisplay
                        ProjectWindowThumbnailDisplay.RegisterThumbnail(guid, thumbnail);
                        loadedCount++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Effect Viewer] Failed to load thumbnail {filePath}: {e.Message}");
                }
            }
        }
    }
}
