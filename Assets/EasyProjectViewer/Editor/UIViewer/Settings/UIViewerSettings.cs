using System.IO;
using UnityEditor;
using UnityEngine;

namespace UIViewer.Editor
{
    /// <summary>
    /// UI Viewer settings (Inspector Preview, Auto-generation, etc.)
    /// </summary>
    public class UIViewerSettings : ScriptableObject
    {
        private static UIViewerSettings instance;

        [Header("Preview Display Settings")]
        [Tooltip("Enable/disable inspector preview for UI prefabs")]
        public bool enableInspectorPreview = true;

        [Tooltip("Enable/disable automatic thumbnail generation when importing/modifying UI prefabs")]
        public bool autoGenerateThumbnails = false;

        public static UIViewerSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find existing asset by type
                    string[] guids = AssetDatabase.FindAssets($"t:{typeof(UIViewerSettings).Name}");
                    if (guids.Length > 0)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        instance = AssetDatabase.LoadAssetAtPath<UIViewerSettings>(assetPath);
                    }

                    if (instance == null)
                    {
                        // Determine settings folder path relative to script location
                        var script = MonoScript.FromScriptableObject(CreateInstance<UIViewerSettings>());
                        string scriptPath = AssetDatabase.GetAssetPath(script);
                        string scriptDir = Path.GetDirectoryName(scriptPath);
                        // Navigate: Settings(current) -> UIViewer -> Editor -> EasyProjectViewer -> Settings
                        string rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(scriptDir)));
                        string settingsFolder = Path.Combine(rootFolder, "Settings").Replace("\\", "/");

                        // Create Settings folder if it doesn't exist
                        CreateFolderIfMissing(settingsFolder);

                        instance = CreateInstance<UIViewerSettings>();

                        // Set default values
                        instance.enableInspectorPreview = true;
                        instance.autoGenerateThumbnails = false;

                        // Save as asset
                        string assetPath = Path.Combine(settingsFolder, "UIViewerSettings.asset").Replace("\\", "/");
                        AssetDatabase.CreateAsset(instance, assetPath);
                        AssetDatabase.SaveAssets();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Open UI Viewer settings window
        /// </summary>
        [MenuItem("Tools/UIViewer/Settings/UIViewer Settings")]
        public static void OpenSettings()
        {
            Selection.activeObject = Instance;
            EditorGUIUtility.PingObject(Instance);
        }

        /// <summary>
        /// Create folder hierarchy if it doesn't exist
        /// </summary>
        private static void CreateFolderIfMissing(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            // Split path into components
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0]; // Start with "Assets"

            // Build path incrementally
            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }
    }
}
