using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyProjectViewer.Editor.ProjectWindowThumbnail
{
    /// <summary>
    /// Settings for Project Window thumbnail display
    /// </summary>
    public class ProjectWindowThumbnailSettings : ScriptableObject
    {
        private static ProjectWindowThumbnailSettings instance;

        [Header("Display Settings")]
        [Tooltip("Show custom thumbnails in Unity's Project Browser window")]
        public bool showThumbnails = true;

        [Header("Setup Wizard")]
        [Tooltip("Show the setup wizard on editor startup")]
        public bool showWizardOnStart = true;

        public static ProjectWindowThumbnailSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find existing asset by type
                    string[] guids = AssetDatabase.FindAssets($"t:{typeof(ProjectWindowThumbnailSettings).Name}");
                    if (guids.Length > 0)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        instance = AssetDatabase.LoadAssetAtPath<ProjectWindowThumbnailSettings>(assetPath);
                    }

                    if (instance == null)
                    {
                        // Determine settings folder path relative to script location
                        var script = MonoScript.FromScriptableObject(CreateInstance<ProjectWindowThumbnailSettings>());
                        string scriptPath = AssetDatabase.GetAssetPath(script);
                        string scriptDir = Path.GetDirectoryName(scriptPath);
                        // Navigate: ProjectWindowThumbnail(current) -> Editor -> EasyProjectViewer
                        string rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(scriptDir));
                        string settingsFolder = Path.Combine(rootFolder, "Settings").Replace("\\", "/");

                        // Create folders if they don't exist
                        string[] folderParts = settingsFolder.Split('/');
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

                        instance = CreateInstance<ProjectWindowThumbnailSettings>();
                        instance.showThumbnails = true;
                        instance.showWizardOnStart = true;

                        string assetPath = Path.Combine(settingsFolder, "ProjectWindowThumbnailSettings.asset").Replace("\\", "/");
                        AssetDatabase.CreateAsset(instance, assetPath);
                        AssetDatabase.SaveAssets();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Open settings window
        /// </summary>
        public static void OpenSettings()
        {
            Selection.activeObject = Instance;
            EditorGUIUtility.PingObject(Instance);
        }

        /// <summary>
        /// Toggle thumbnail display in Project Window
        /// </summary>
        [MenuItem("Tools/EasyProjectViewer/Show Thumbnails in Project Window")]
        public static void ToggleThumbnailDisplay()
        {
            var settings = Instance;
            settings.showThumbnails = !settings.showThumbnails;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            if (settings.showThumbnails)
            {
                // Reload all thumbnails from registered loaders
                ProjectWindowThumbnailDisplay.ReloadAllThumbnails();
            }
            else
            {
                // Clear thumbnails from display
                ProjectWindowThumbnailDisplay.ClearCache();
            }
        }

        /// <summary>
        /// Validate thumbnail display menu (show checkmark)
        /// </summary>
        [MenuItem("Tools/EasyProjectViewer/Show Thumbnails in Project Window", true)]
        public static bool ToggleThumbnailDisplayValidate()
        {
            Menu.SetChecked("Tools/EasyProjectViewer/Show Thumbnails in Project Window", Instance.showThumbnails);
            return true;
        }
    }
}
