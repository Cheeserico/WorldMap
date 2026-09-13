#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EasyProjectViewer.Editor.ProjectWindowThumbnail;
using EffectViewer.Editor;
using UIViewer.Editor;

namespace EasyProjectViewer.Editor
{
    /// <summary>
    /// Welcome setup window shown on first import into a project.
    /// Provides individual buttons to generate Effect / UI thumbnails one at a time.
    /// Reopenable from Tools > EasyProjectViewer > Setup Wizard.
    /// </summary>
    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {
        private bool isGenerating;
        private bool effectGenerated;
        private bool uiGenerated;

        static WelcomeWindow()
        {
            AssetDatabase.importPackageCompleted += OnPackageImported;
        }

        private static void OnPackageImported(string packageName)
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                if (ProjectWindowThumbnailSettings.Instance.showWizardOnStart)
                {
                    ShowWindow();
                }
            };
        }

        [MenuItem("Tools/EasyProjectViewer/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<WelcomeWindow>(true, "Easy Project Viewer - Setup");
            window.minSize = new Vector2(450, 440);
            window.maxSize = new Vector2(450, 440);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Easy Project Viewer", titleStyle);

            EditorGUILayout.Space(5);

            // Description
            var descStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(
                "Generate thumbnails for your project.\nPress each button to enable and generate.",
                descStyle);

            EditorGUILayout.Space(15);

            // Disable all buttons while generating
            GUI.enabled = !isGenerating;

            // Effect section
            DrawFeatureSection(
                "Effect Thumbnails (ParticleSystem / VFX Graph)",
                "Generate Effect Thumbnails",
                effectGenerated,
                OnGenerateEffectThumbnails);

            EditorGUILayout.Space(8);

            // UI section
            DrawFeatureSection(
                "UI Thumbnails",
                "Generate UI Thumbnails",
                uiGenerated,
                OnGenerateUIThumbnails);

            GUI.enabled = true;

            EditorGUILayout.Space(10);

            // Hint: Effect Viewer window
            EditorGUILayout.HelpBox(
                "You can browse and preview effect thumbnails from:\n" +
                "  Ctrl + Shift + E  or  Tools > EffectViewer > Viewer",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // Hint: Settings
            EditorGUILayout.HelpBox(
                "You can change these settings later from:\n" +
                "  Tools > EffectViewer > Settings\n" +
                "  Tools > UIViewer > Settings",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Close button
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Close", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    EnsureSettingsExist();

                    var thumbnailSettings = ProjectWindowThumbnailSettings.Instance;
                    thumbnailSettings.showWizardOnStart = false;
                    EditorUtility.SetDirty(thumbnailSettings);
                    AssetDatabase.SaveAssets();

                    Close();
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFeatureSection(string label, string buttonLabel, bool isDone, System.Action onGenerate)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    if (isDone)
                    {
                        var doneStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            normal = { textColor = new Color(0.2f, 0.7f, 0.2f) },
                            alignment = TextAnchor.MiddleCenter
                        };
                        GUILayout.Label("Done", doneStyle, GUILayout.Width(200), GUILayout.Height(28));
                    }
                    else
                    {
                        if (GUILayout.Button(buttonLabel, GUILayout.Width(200), GUILayout.Height(28)))
                        {
                            onGenerate?.Invoke();
                        }
                    }

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);
            }
            EditorGUILayout.EndVertical();
        }

        private void OnGenerateEffectThumbnails()
        {
            isGenerating = true;

            var settings = EffectViewerSettings.Instance;
            settings.autoGenerateThumbnails = true;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            EffectPrefabAutoThumbnail.RegenerateAllThumbnails();

            effectGenerated = true;
            isGenerating = false;
            Repaint();
        }

        private void OnGenerateUIThumbnails()
        {
            isGenerating = true;

            var settings = UIViewerSettings.Instance;
            settings.autoGenerateThumbnails = true;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            UIPrefabAutoThumbnail.RegenerateAllThumbnails();

            uiGenerated = true;
            isGenerating = false;
            Repaint();
        }

        private void EnsureSettingsExist()
        {
            // Access Instance to trigger auto-creation of settings assets
            _ = EffectViewerSettings.Instance;
            _ = UIViewerSettings.Instance;
        }
    }
}
#endif
