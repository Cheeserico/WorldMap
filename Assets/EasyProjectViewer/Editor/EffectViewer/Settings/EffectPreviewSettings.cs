using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Effect Inspector Preview settings (display, camera, grid, etc.)
    /// </summary>
    public class EffectPreviewSettings : ScriptableObject
    {
        private static EffectPreviewSettings instance;

        [Header("Preview Display Settings")]
        [Tooltip("Enable/disable inspector preview for effect prefabs")]
        public bool enableInspectorPreview = true;

        [Tooltip("Preview background color")]
        public Color previewBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Tooltip("Show grid in preview")]
        public bool showGrid = true;

        [Tooltip("Default force loop setting for effect preview")]
        public bool defaultForceLoop = false;

        [Tooltip("Camera field of view")]
        [Range(10f, 90f)]
        public float cameraFOV = 30f;

        [Header("Camera Default Settings")]
        [Tooltip("Camera yaw angle (degrees)")]
        [Range(-180f, 180f)]
        public float cameraYaw = 0f;

        [Tooltip("Camera pitch angle (degrees)")]
        [Range(-89f, 89f)]
        public float cameraPitch = -30f;

        [Tooltip("Camera distance from target")]
        [Range(1f, 50f)]
        public float cameraDistance = 12f;

        [Header("Grid Display Settings")]
        [Tooltip("Grid material (works with all render pipelines)")]
        public Material gridMaterial = null;

        [Tooltip("Grid plane size (width and depth)")]
        [Range(1f, 100f)]
        public float gridPlaneSize = 10f;

        [Tooltip("Grid height offset")]
        [Range(-10f, 10f)]
        public float gridHeightOffset = 0f;

        public static EffectPreviewSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find existing asset by type
                    string[] guids = AssetDatabase.FindAssets($"t:{typeof(EffectPreviewSettings).Name}");
                    if (guids.Length > 0)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        instance = AssetDatabase.LoadAssetAtPath<EffectPreviewSettings>(assetPath);
                    }

                    if (instance == null)
                    {
                        // Determine settings folder path relative to script location
                        var script = MonoScript.FromScriptableObject(CreateInstance<EffectPreviewSettings>());
                        string scriptPath = AssetDatabase.GetAssetPath(script);
                        string scriptDir = Path.GetDirectoryName(scriptPath);
                        // Navigate: Settings(current) -> EffectViewer -> Editor -> EasyProjectViewer -> Settings
                        string rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(scriptDir)));
                        string settingsFolder = Path.Combine(rootFolder, "Settings").Replace("\\", "/");

                        // Create Settings folder if it doesn't exist
                        EditorFolderUtility.CreateFolderIfMissing(settingsFolder);

                        instance = CreateInstance<EffectPreviewSettings>();

                        // Set default values
                        instance.enableInspectorPreview = true;
                        instance.previewBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                        instance.showGrid = true;
                        instance.defaultForceLoop = false;
                        instance.cameraFOV = 30f;
                        instance.cameraYaw = 0f;
                        instance.cameraPitch = -30f;
                        instance.cameraDistance = 12f;
                        instance.gridMaterial = null;
                        instance.gridPlaneSize = 10f;
                        instance.gridHeightOffset = 0f;

                        // Save as asset
                        string assetPath = Path.Combine(settingsFolder, "EffectPreviewSettings.asset").Replace("\\", "/");
                        AssetDatabase.CreateAsset(instance, assetPath);
                        AssetDatabase.SaveAssets();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Get grid material (works with all render pipelines)
        /// </summary>
        public Material GridMaterial
        {
            get
            {
                return gridMaterial;
            }
        }

        /// <summary>
        /// Open preview settings window
        /// </summary>
        [MenuItem("Tools/EffectViewer/Settings/Preview Settings")]
        public static void OpenSettings()
        {
            Selection.activeObject = Instance;
            EditorGUIUtility.PingObject(Instance);
        }
    }
}
