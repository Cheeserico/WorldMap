using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Effect Viewer settings (thumbnail capture settings)
    /// </summary>
    public class EffectViewerSettings : ScriptableObject
    {
        private static EffectViewerSettings instance;

        [Header("Manual Management")]
        [Tooltip("List of manually added prefab GUIDs")]
        public List<string> manuallyAddedPrefabGuids = new List<string>();

        [Tooltip("List of excluded prefab GUIDs (won't show in viewer)")]
        public List<string> excludedPrefabGuids = new List<string>();

        [Header("Thumbnail Capture Settings")]
        [Tooltip("Camera yaw angle (degrees)")]
        [Range(-180f, 180f)]
        public float thumbnailCameraYaw = 45f;

        [Tooltip("Camera pitch angle (degrees)")]
        [Range(-89f, 89f)]
        public float thumbnailCameraPitch = -30f;

        [Tooltip("Camera distance from target")]
        [Range(1f, 50f)]
        public float thumbnailCameraDistance = 12f;

        [Header("Advanced Thumbnail Camera Settings")]
        [Tooltip("Camera field of view (degrees)")]
        [Range(10f, 90f)]
        public float thumbnailCameraFOV = 30f;

        [Tooltip("Camera zoom factor (1 = default distance)")]
        [Range(0.1f, 5f)]
        public float thumbnailZoomFactor = 1f;

        [Tooltip("Pivot offset for camera target")]
        public Vector3 thumbnailPivotOffset = Vector3.zero;

        [Tooltip("Thumbnail background color")]
        public Color thumbnailBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Header("Thumbnail Generation Settings")]
        [Tooltip("Thumbnail image size")]
        public int thumbnailSize = 128;

        [Tooltip("Simulation time (seconds)")]
        [Range(0f, 5f)]
        public float thumbnailSimulationTime = 0.5f;

        [Header("Auto-Generation Settings")]
        [Tooltip("Automatically generate thumbnails when effect prefabs are imported or modified")]
        public bool autoGenerateThumbnails = true;

        public static EffectViewerSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find existing asset by type
                    string[] guids = AssetDatabase.FindAssets($"t:{typeof(EffectViewerSettings).Name}");
                    if (guids.Length > 0)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        instance = AssetDatabase.LoadAssetAtPath<EffectViewerSettings>(assetPath);
                    }

                    if (instance == null)
                    {
                        // Determine settings folder path relative to script location
                        var script = MonoScript.FromScriptableObject(CreateInstance<EffectViewerSettings>());
                        string scriptPath = AssetDatabase.GetAssetPath(script);
                        string scriptDir = Path.GetDirectoryName(scriptPath);
                        // Navigate: Settings(current) -> EffectViewer -> Editor -> EasyProjectViewer -> Settings
                        string rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(scriptDir)));
                        string settingsFolder = Path.Combine(rootFolder, "Settings").Replace("\\", "/");

                        // Create Settings folder if it doesn't exist
                        EditorFolderUtility.CreateFolderIfMissing(settingsFolder);

                        instance = CreateInstance<EffectViewerSettings>();

                        // Set default values (aligned with EffectPreviewSettings)
                        instance.thumbnailCameraYaw = 45f;
                        instance.thumbnailCameraPitch = -30f;
                        instance.thumbnailCameraDistance = 12f;
                        instance.thumbnailCameraFOV = 30f;
                        instance.thumbnailZoomFactor = 1f;
                        instance.thumbnailPivotOffset = Vector3.zero;
                        instance.thumbnailBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                        instance.thumbnailSize = 128;
                        instance.thumbnailSimulationTime = 0.5f;
                        instance.autoGenerateThumbnails = true;

                        // Save as asset
                        string assetPath = Path.Combine(settingsFolder, "EffectViewerSettings.asset").Replace("\\", "/");
                        AssetDatabase.CreateAsset(instance, assetPath);
                        AssetDatabase.SaveAssets();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Open viewer settings window
        /// </summary>
        [MenuItem("Tools/EffectViewer/Settings/Viewer Settings")]
        public static void OpenSettings()
        {
            Selection.activeObject = Instance;
            EditorGUIUtility.PingObject(Instance);
        }

        /// <summary>
        /// Add prefab to manual list
        /// </summary>
        public void AddManualPrefab(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return;
            if (manuallyAddedPrefabGuids.Contains(guid)) return;

            manuallyAddedPrefabGuids.Add(guid);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Remove prefab from manual list
        /// </summary>
        public void RemoveManualPrefab(string guid)
        {
            if (manuallyAddedPrefabGuids.Remove(guid))
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Add prefab to exclusion list
        /// </summary>
        public void ExcludePrefab(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return;
            if (excludedPrefabGuids.Contains(guid)) return;

            excludedPrefabGuids.Add(guid);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Remove prefab from exclusion list
        /// </summary>
        public void UnexcludePrefab(string guid)
        {
            if (excludedPrefabGuids.Remove(guid))
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Check if prefab is manually added
        /// </summary>
        public bool IsManuallyAdded(string guid)
        {
            return manuallyAddedPrefabGuids.Contains(guid);
        }

        /// <summary>
        /// Check if prefab is excluded
        /// </summary>
        public bool IsExcluded(string guid)
        {
            return excludedPrefabGuids.Contains(guid);
        }
    }
}
