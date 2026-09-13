using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
#if UNITY_6000_4_OR_NEWER
using ObjectId = UnityEngine.EntityId;
#else
using ObjectId = System.Int32;
#endif

namespace EffectViewer.Editor
{
    /// <summary>
    /// Base class for effect inspector preview
    /// Provides common functionality for ParticleSystem and VFXGraph previews
    /// </summary>
    internal abstract class BaseEffectInspectorPreview : ObjectPreview
    {
        // Preview rendering
        protected PreviewRenderUtility m_PreviewUtility;
        protected GameObject m_PreviewInstance;
        protected const int PreviewCullingLayer = 31;

        // Renderer state preservation
        protected Dictionary<ObjectId, bool> m_OriginalRendererStates;

        // Camera control
        protected Vector2 m_PreviewDir; // yaw, pitch
        protected Vector2 m_DefaultPreviewDir;
        protected float m_ZoomFactor = 1f;
        protected Vector3 m_PivotPositionOffset = Vector3.zero;
        protected float m_AvatarScale = 1f;
        protected float m_CameraFOV;


        // View tool
        protected enum ViewTool { None, Pan, Zoom, Orbit }
        protected ViewTool m_ViewTool;

        // Simulation
        protected bool m_Playing;
        protected float m_RunningTime;
        protected double m_PreviousTime;
        protected float m_PlaybackSpeed = 1f;
        protected bool m_ForceLoop = true;

        // Preview state
        protected bool m_HasPreview;
        protected bool m_Loaded;
        protected UnityEditor.Editor m_CacheEditor;
        protected Object m_PreviousTarget;

        /// <summary>
        /// Whether this preview instance is active (used by EffectPreviewReorderHelper).
        /// Avoids re-entrancy by not calling HasPreviewGUI().
        /// </summary>
        internal bool IsEffectPreviewActive =>
            m_HasPreview && EffectPreviewSettings.Instance.enableInspectorPreview;

        // Background and grid
        protected Color m_BackgroundColor;
        protected bool m_ShowGrid;
        protected GameObject m_GridPlane;

        protected const float kDuration = 99f;

        /// <summary>
        /// Default constructor required by Unity's ObjectPreview registration system
        /// </summary>
        public BaseEffectInspectorPreview()
        {
            // Initialize with default values (fields already initialized at declaration)

            // Register for play mode state changes to cleanup properly
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Handle play mode state changes to avoid null reference exceptions
        /// </summary>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Cleanup before entering or exiting play mode to prevent null reference exceptions
            if (state == PlayModeStateChange.ExitingEditMode ||
                state == PlayModeStateChange.ExitingPlayMode)
            {
                // Stop simulation and unregister update callback
                SimulateDisable();

                // Clear editor reference to prevent stale repaint calls
                m_CacheEditor = null;
            }
        }

        // ===== Abstract methods (must be implemented by subclasses) =====

        /// <summary>
        /// Check if target has effect component for preview
        /// </summary>
        protected abstract bool HasEffectComponent(GameObject go);

        /// <summary>
        /// Start effect simulation on preview instance
        /// </summary>
        protected abstract void StartEffectSimulation();

        /// <summary>
        /// Stop effect simulation on preview instance
        /// </summary>
        protected abstract void StopEffectSimulation();

        /// <summary>
        /// Update playback speed of effect
        /// </summary>
        protected abstract void UpdatePlaybackSpeed(float speed);

        /// <summary>
        /// Force effect to loop (for preview continuity)
        /// </summary>
        protected virtual void ForceLoopEffect(bool force) { }

        /// <summary>
        /// Get preview title
        /// </summary>
        public abstract override GUIContent GetPreviewTitle();

        // ===== Common implementation methods =====

        /// <summary>
        /// Set the parent editor for Repaint calls
        /// </summary>
        public void SetEditor(UnityEditor.Editor editor)
        {
            m_CacheEditor = editor;
        }

        /// <summary>
        /// Initialize preview with targets
        /// </summary>
        public override void Initialize(Object[] targets)
        {
            base.Initialize(targets);

            // Load settings from EffectPreviewSettings
            var settings = EffectPreviewSettings.Instance;
            m_DefaultPreviewDir = new Vector2(settings.cameraYaw, settings.cameraPitch);
            m_BackgroundColor = settings.previewBackgroundColor;
            m_ShowGrid = settings.showGrid;
            m_ForceLoop = settings.defaultForceLoop;

            // Detect target change and reset camera to ensure consistent default view
            if (m_PreviousTarget != target)
            {
                m_PreviewDir = m_DefaultPreviewDir;
                m_ZoomFactor = 1f;
                m_PivotPositionOffset = Vector3.zero;
                m_CameraFOV = settings.cameraFOV;
                m_PreviousTarget = target;
            }

            // Find active editor if not set
            if (m_CacheEditor == null)
            {
                var editors = ActiveEditorTracker.sharedTracker.activeEditors;
                foreach (var editor in editors)
                {
                    if (editor.target == target)
                    {
                        m_CacheEditor = editor;
                        break;
                    }
                }
            }

            // Check if preview is available
            m_HasPreview = EditorUtility.IsPersistent(target) && HasStaticPreview();
            if (targets.Length != 1)
            {
                m_HasPreview = false;
            }
        }

        /// <summary>
        /// Check if target has effect for preview
        /// </summary>
        private bool HasStaticPreview()
        {
            GameObject go = target as GameObject;
            if (go == null)
                return false;

            return HasEffectComponent(go);
        }

        /// <summary>
        /// Check if preview GUI is available
        /// </summary>
        public override bool HasPreviewGUI()
        {
            // Check if inspector preview is enabled in settings
            if (!EffectPreviewSettings.Instance.enableInspectorPreview)
                return false;

            if (m_HasPreview)
            {
                // Reorder preview tabs so this effect preview becomes the default tab.
                // Without this, selecting a different object and re-selecting resets to Unity's default preview.
                EffectPreviewReorderHelper.ScheduleReorderPreviews();
            }

            return m_HasPreview;
        }

        /// <summary>
        /// Main preview rendering
        /// </summary>
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            InitPreview();
            if (m_PreviewUtility == null)
            {
                return;
            }

            Event current = Event.current;

            if (current.type == EventType.Repaint)
            {
                m_PreviewUtility.BeginPreview(r, background);
                DoRenderPreview();
                m_PreviewUtility.EndAndDrawPreview(r);
            }

            // Handle camera controls
            HandleViewTool(current, r);

            // Display playback time
            EditorGUI.DropShadowLabel(new Rect(r.x, r.yMax - 20f, r.width, 20f),
                (r.width > 140f ? "Playback Time:" : string.Empty) + string.Format("{0:F2}", m_RunningTime));
        }

        /// <summary>
        /// Preview toolbar settings
        /// </summary>
        public override void OnPreviewSettings()
        {
            InitPreview();
            if (m_PreviewUtility == null)
            {
                return;
            }

            // Play/Stop button
            GUIContent playStopIcon = m_Playing
                ? new GUIContent("■", "Stop")
                : new GUIContent("▶", "Play");

            bool newPlaying = GUILayout.Toggle(m_Playing, playStopIcon, EditorStyles.toolbarButton, GUILayout.Width(25));
            if (newPlaying != m_Playing)
            {
                if (newPlaying)
                {
                    SimulateEnable();
                }
                else
                {
                    SimulateDisable();
                }
            }

            // Restart button
            GUIContent restartIcon = new GUIContent("↻", "Restart");
            if (GUILayout.Button(restartIcon, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                RestartSimulation();
            }

            // Speed Reset button
            GUIContent speedResetIcon = new GUIContent("1×", "Reset Speed to 1x");
            if (GUILayout.Button(speedResetIcon, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                m_PlaybackSpeed = 1f;
                if (m_PreviewInstance != null)
                {
                    UpdatePlaybackSpeed(m_PlaybackSpeed);
                }
            }

            GUILayout.Space(5);

            // Speed slider
            EditorGUI.BeginChangeCheck();
            m_PlaybackSpeed = GUILayout.HorizontalSlider(m_PlaybackSpeed, 0.01f, 3f, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck() && m_PreviewInstance != null)
            {
                UpdatePlaybackSpeed(m_PlaybackSpeed);
            }
            GUILayout.Label(m_PlaybackSpeed.ToString("F2") + "x", EditorStyles.miniLabel, GUILayout.Width(30));

            GUILayout.Space(5);

            // Loop toggle
            EditorGUI.BeginChangeCheck();
            GUIContent loopIcon = new GUIContent("∞", m_ForceLoop ? "Force Loop: ON" : "Force Loop: OFF");
            m_ForceLoop = GUILayout.Toggle(m_ForceLoop, loopIcon, EditorStyles.toolbarButton, GUILayout.Width(25));
            if (EditorGUI.EndChangeCheck())
            {
                ForceLoopEffect(m_ForceLoop);
            }

            GUILayout.Space(5);

            // Background color
            EditorGUI.BeginChangeCheck();
            m_BackgroundColor = EditorGUILayout.ColorField(GUIContent.none, m_BackgroundColor, false, false, false, GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }

            // Grid toggle
            EditorGUI.BeginChangeCheck();
            GUIContent gridIcon = new GUIContent("⊞", m_ShowGrid ? "Hide Grid" : "Show Grid");
            m_ShowGrid = GUILayout.Toggle(m_ShowGrid, gridIcon, EditorStyles.toolbarButton, GUILayout.Width(25));
            if (EditorGUI.EndChangeCheck() && m_GridPlane != null)
            {
                Renderer gridRenderer = m_GridPlane.GetComponent<Renderer>();
                if (gridRenderer != null)
                {
                    gridRenderer.enabled = m_ShowGrid;
                }
            }

            GUILayout.Space(5);

            // Camera reset
            GUIContent cameraIcon = EditorGUIUtility.IconContent("SceneViewCamera", "|Reset Camera");
            if (GUILayout.Button(cameraIcon, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                ResetCamera();
            }

            // Zoom slider
            EditorGUI.BeginChangeCheck();
            m_ZoomFactor = GUILayout.HorizontalSlider(m_ZoomFactor, 0.1f, 5f, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }

            GUILayout.Space(5);

            // Thumbnail update button
            if (GUILayout.Button("◉ Thumbnail", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                UpdateThumbnail();
            }

            // Preview Settings button
            GUIContent previewSettingsIcon = EditorGUIUtility.IconContent("_Popup", "|Preview Settings");
            if (GUILayout.Button(previewSettingsIcon, EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                EffectPreviewSettings.OpenSettings();
            }
        }

        /// <summary>
        /// Initialize preview if not loaded
        /// </summary>
        protected void InitPreview()
        {
            if (m_Loaded)
                return;

            m_Loaded = true;

            GameObject go = target as GameObject;
            if (go == null)
                return;

            // Create preview utility
            m_PreviewUtility = new PreviewRenderUtility();
            m_PreviewUtility.camera.fieldOfView = m_CameraFOV;
            m_PreviewUtility.camera.nearClipPlane = 0.1f;
            m_PreviewUtility.camera.farClipPlane = 1000f;
            m_PreviewUtility.camera.cullingMask = 1 << PreviewCullingLayer;

            // Ensure URP camera data (required for Unity 6+)
            URPCameraHelper.EnsureURPCameraData(m_PreviewUtility.camera);

            // Setup lights
            m_PreviewUtility.lights[0].intensity = 1.0f;
            m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 330f, 0f);

            // Create preview instance
            CreatePreviewInstance();

            // Create grid plane
            CreateGridPlane();

            // Auto-start playback
            SimulateEnable();
        }

        /// <summary>
        /// Create preview instance
        /// </summary>
        protected void CreatePreviewInstance()
        {
            GameObject go = target as GameObject;
            if (go == null)
                return;

            // Store original renderer states before instantiation
            m_OriginalRendererStates = new Dictionary<ObjectId, bool>();
            Renderer[] originalRenderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in originalRenderers)
            {
                if (renderer != null)
                {
#if UNITY_6000_4_OR_NEWER
                    m_OriginalRendererStates[renderer.GetEntityId()] = renderer.enabled;
#else
                    m_OriginalRendererStates[renderer.GetInstanceID()] = renderer.enabled;
#endif
                }
            }

            // Instantiate
            m_PreviewInstance = Object.Instantiate(go);
            m_PreviewInstance.hideFlags = HideFlags.HideAndDontSave;

            // Add to preview scene (not main scene)
            m_PreviewUtility.AddSingleGO(m_PreviewInstance);

            // Set layer recursively
            SetLayerRecursively(m_PreviewInstance.transform, PreviewCullingLayer);

            // Restore original renderer states instead of forcing all enabled
            RestoreRendererStates();

            // Force loop for effects (for preview continuity)
            if (m_ForceLoop)
            {
                ForceLoopEffect(true);
            }

            // Use fixed scale for consistent camera distance across all effects
            m_AvatarScale = 1f;
        }

        /// <summary>
        /// Create grid plane for preview
        /// </summary>
        protected void CreateGridPlane()
        {
            var settings = EffectPreviewSettings.Instance;
            if (settings == null || settings.GridMaterial == null)
                return;

            // Create plane (remove auto-generated collider — not needed for preview)
            m_GridPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            m_GridPlane.hideFlags = HideFlags.HideAndDontSave;
            m_GridPlane.name = "PreviewGrid";

            var collider = m_GridPlane.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            // Add to preview scene
            m_PreviewUtility.AddSingleGO(m_GridPlane);

            // Set layer
            m_GridPlane.layer = PreviewCullingLayer;

            // Set position and scale
            m_GridPlane.transform.position = new Vector3(0f, settings.gridHeightOffset, 0f);
            float scale = settings.gridPlaneSize / 10f; // Default plane is 10x10
            m_GridPlane.transform.localScale = new Vector3(scale, 1f, scale);

            // Apply material
            Renderer renderer = m_GridPlane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = settings.GridMaterial;
                renderer.enabled = m_ShowGrid;
            }
        }

        /// <summary>
        /// Set layer recursively
        /// </summary>
        protected void SetLayerRecursively(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform child in transform)
            {
                SetLayerRecursively(child, layer);
            }
        }

        /// <summary>
        /// Get renderable bounds
        /// </summary>
        protected Bounds GetRenderableBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(go.transform.position, Vector3.one * 2f);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        /// <summary>
        /// Render preview scene
        /// </summary>
        protected void DoRenderPreview()
        {
            if (m_PreviewInstance == null)
                return;

            // Setup lighting
            bool oldFog = SetupPreviewLightingAndFx();

            // Setup camera clip planes
            m_PreviewUtility.camera.nearClipPlane = 0.5f * m_ZoomFactor;
            m_PreviewUtility.camera.farClipPlane = 100f * m_AvatarScale;

            // Set background color and FOV
            m_PreviewUtility.camera.backgroundColor = m_BackgroundColor;
            m_PreviewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            m_PreviewUtility.camera.fieldOfView = m_CameraFOV;

            // Position camera (unified with EffectPreviewWindow)
            float dist = EffectPreviewSettings.Instance.cameraDistance * m_ZoomFactor;
            Quaternion rot = Quaternion.Euler(-m_PreviewDir.y, -m_PreviewDir.x, 0f);
            Vector3 camPos = rot * (Vector3.forward * -dist) + m_PreviewInstance.transform.position + m_PivotPositionOffset;

            m_PreviewUtility.camera.transform.position = camPos;
            m_PreviewUtility.camera.transform.rotation = rot;

            // Render (renderers are always enabled)
            m_PreviewUtility.Render(true);

            // Cleanup lighting
            TeardownPreviewLightingAndFx(oldFog);
        }

        /// <summary>
        /// Setup preview lighting
        /// </summary>
        protected bool SetupPreviewLightingAndFx()
        {
            m_PreviewUtility.lights[0].intensity = 1.4f;
            m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
            m_PreviewUtility.lights[1].intensity = 1.4f;

            Color ambient = new Color(0.1f, 0.1f, 0.1f, 0f);
            InternalEditorUtility.SetCustomLighting(m_PreviewUtility.lights, ambient);

            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);
            return oldFog;
        }

        /// <summary>
        /// Teardown preview lighting
        /// </summary>
        protected void TeardownPreviewLightingAndFx(bool oldFog)
        {
            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
            InternalEditorUtility.RemoveCustomLighting();
        }

        /// <summary>
        /// Enable/disable renderers recursively
        /// </summary>
        protected void SetEnabledRecursive(GameObject go, bool enabled)
        {
            if (go == null)
                return;

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = enabled;
            }
        }

        /// <summary>
        /// Restore renderer states from original prefab
        /// </summary>
        protected void RestoreRendererStates()
        {
            if (m_PreviewInstance == null || m_OriginalRendererStates == null)
                return;

            Renderer[] previewRenderers = m_PreviewInstance.GetComponentsInChildren<Renderer>(true);

            // Map original renderers to preview instance renderers by hierarchy path
            GameObject originalGo = target as GameObject;
            if (originalGo == null)
                return;

            Renderer[] originalRenderers = originalGo.GetComponentsInChildren<Renderer>(true);

            // Create path-based mapping for reliable matching between original and instance
            for (int i = 0; i < originalRenderers.Length && i < previewRenderers.Length; i++)
            {
                if (originalRenderers[i] != null && previewRenderers[i] != null)
                {
#if UNITY_6000_4_OR_NEWER
                    ObjectId originalID = originalRenderers[i].GetEntityId();
#else
                    ObjectId originalID = originalRenderers[i].GetInstanceID();
#endif
                    if (m_OriginalRendererStates.TryGetValue(originalID, out bool wasEnabled))
                    {
                        previewRenderers[i].enabled = wasEnabled;
                    }
                }
            }
        }

        /// <summary>
        /// Handle view tool for camera control
        /// </summary>
        protected void HandleViewTool(Event evt, Rect rect)
        {
            if (!rect.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    DetermineViewTool(evt);
                    evt.Use();
                    break;

                case EventType.MouseUp:
                    m_ViewTool = ViewTool.None;
                    evt.Use();
                    break;

                case EventType.MouseDrag:
                    if (m_ViewTool == ViewTool.Orbit)
                    {
                        m_PreviewDir -= evt.delta * 0.5f;
                        evt.Use();
                    }
                    else if (m_ViewTool == ViewTool.Zoom)
                    {
                        m_ZoomFactor = Mathf.Max(0.1f, m_ZoomFactor + evt.delta.y * 0.01f);
                        evt.Use();
                    }
                    else if (m_ViewTool == ViewTool.Pan)
                    {
                        Vector3 up = m_PreviewUtility.camera.transform.up;
                        Vector3 right = m_PreviewUtility.camera.transform.right;
                        // Match Scene View pan behavior: mouse direction = view direction
                        m_PivotPositionOffset -= (right * evt.delta.x - up * evt.delta.y) * 0.01f;
                        evt.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    m_ZoomFactor = Mathf.Max(0.1f, m_ZoomFactor + evt.delta.y * 0.05f);
                    evt.Use();
                    break;
            }
        }

        /// <summary>
        /// Determine view tool from event
        /// </summary>
        protected void DetermineViewTool(Event evt)
        {
            if (evt.button == 1) // Right click for orbit
            {
                m_ViewTool = ViewTool.Orbit;
            }
            else if (evt.button == 2) // Middle click for pan
            {
                m_ViewTool = ViewTool.Pan;
            }
            else
            {
                m_ViewTool = ViewTool.None;
            }
        }

        /// <summary>
        /// Start effect simulation
        /// </summary>
        protected void SimulateEnable()
        {
            if (m_PreviewInstance == null)
                return;

            StartEffectSimulation();

            // Register update callback
            m_PreviousTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= InspectorUpdate;
            EditorApplication.update += InspectorUpdate;

            m_RunningTime = 0f;
            m_Playing = true;
        }

        /// <summary>
        /// Stop effect simulation
        /// </summary>
        protected void SimulateDisable()
        {
            if (m_PreviewInstance != null)
            {
                StopEffectSimulation();
            }

            // Unregister update callback
            EditorApplication.update -= InspectorUpdate;

            m_RunningTime = 0f;
            m_Playing = false;
        }

        /// <summary>
        /// Restart effect simulation from the beginning
        /// </summary>
        protected void RestartSimulation()
        {
            if (m_PreviewInstance == null)
                return;

            // Stop and restart
            StopEffectSimulation();
            m_RunningTime = 0f;
            m_PreviousTime = EditorApplication.timeSinceStartup;
            StartEffectSimulation();
        }

        /// <summary>
        /// Update loop for simulation
        /// </summary>
        protected virtual void InspectorUpdate()
        {
            double delta = EditorApplication.timeSinceStartup - m_PreviousTime;
            m_PreviousTime = EditorApplication.timeSinceStartup;

            if (m_Playing && m_PreviewInstance != null)
            {
                m_RunningTime = Mathf.Clamp(m_RunningTime + (float)delta, 0f, kDuration);

                // Native playback handles simulation timing
                Repaint();
            }
        }

        /// <summary>
        /// Request repaint
        /// Supports ObjectSelector window to mitigate environment-dependent issues
        /// </summary>
        protected void Repaint()
        {
            // Support for ObjectSelector window (ensures update during preview selection)
            EditorWindow ew = EditorWindow.focusedWindow;
            if (ew != null && ew.titleContent.text.StartsWith("Select ", System.StringComparison.Ordinal))
            {
                ew.Repaint();
                return;
            }

            // Normal inspector preview
#if UNITY_6000_0_OR_NEWER
            // On Unity 6+, ObjectPreview repaints are throttled, causing playback to look choppy.
            // Repaint all editor views while playing so the preview render keeps up every frame.
            if (m_Playing)
            {
                InternalEditorUtility.RepaintAllViews();
                return;
            }
#endif

            // Check if editor is still valid (not destroyed) before repaint
            if (m_CacheEditor != null && m_CacheEditor.target != null)
            {
                m_CacheEditor.Repaint();
            }
        }

        /// <summary>
        /// Reset camera to default position
        /// </summary>
        protected void ResetCamera()
        {
            m_PreviewDir = m_DefaultPreviewDir;
            m_ZoomFactor = 1f;
            m_PivotPositionOffset = Vector3.zero;
            m_CameraFOV = 30f;
            Repaint();
        }

        /// <summary>
        /// Update thumbnail from current preview state
        /// </summary>
        protected void UpdateThumbnail()
        {
            if (target == null || m_PreviewUtility == null)
            {
                Debug.LogWarning("[BaseEffectInspectorPreview] Cannot update thumbnail: missing target or preview utility");
                return;
            }

            GameObject go = target as GameObject;
            if (go == null)
            {
                Debug.LogWarning("[BaseEffectInspectorPreview] Target is not a GameObject");
                return;
            }

            // Get asset path and GUID
            string assetPath = AssetDatabase.GetAssetPath(go);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("[BaseEffectInspectorPreview] Target is not an asset");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("[BaseEffectInspectorPreview] Failed to get GUID for asset");
                return;
            }

            // Get thumbnail size from settings
            int thumbnailSize = EffectViewerSettings.Instance.thumbnailSize;

            // Capture current preview state to Texture2D
            Texture2D newThumbnail = CapturePreviewToTexture2D(thumbnailSize, thumbnailSize);

            if (newThumbnail != null)
            {
                // Save thumbnail to cache
                EffectThumbnailManager.SaveThumbnail(guid, newThumbnail);

                // Cleanup temporary texture
                Object.DestroyImmediate(newThumbnail);

                // Update only this prefab's thumbnail in EffectViewerWindow (no full scan)
                var windows = Resources.FindObjectsOfTypeAll<EffectViewerWindow>();
                if (windows.Length > 0)
                {
                    windows[0].UpdateSinglePrefabThumbnail(guid);
                }
            }
            else
            {
                Debug.LogWarning($"[BaseEffectInspectorPreview] Failed to capture thumbnail for {go.name}");
            }
        }

        /// <summary>
        /// Capture current preview to Texture2D
        /// </summary>
        protected Texture2D CapturePreviewToTexture2D(int width, int height)
        {
            if (m_PreviewUtility == null || m_PreviewInstance == null)
                return null;

            // Create temporary RenderTexture for capture
            RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = m_PreviewUtility.camera.targetTexture;

            try
            {
                // Setup lighting
                bool oldFog = SetupPreviewLightingAndFx();

                // Setup camera
                m_PreviewUtility.camera.targetTexture = tempRT;
                m_PreviewUtility.camera.nearClipPlane = 0.5f * m_ZoomFactor;
                m_PreviewUtility.camera.farClipPlane = 100f * m_AvatarScale;
                m_PreviewUtility.camera.backgroundColor = m_BackgroundColor;
                m_PreviewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                m_PreviewUtility.camera.fieldOfView = m_CameraFOV;

                // Position camera (unified with EffectPreviewWindow)
                float dist = EffectPreviewSettings.Instance.cameraDistance * m_ZoomFactor;
                Quaternion rot = Quaternion.Euler(-m_PreviewDir.y, -m_PreviewDir.x, 0f);
                Vector3 camPos = rot * (Vector3.forward * -dist) + m_PreviewInstance.transform.position + m_PivotPositionOffset;
                m_PreviewUtility.camera.transform.position = camPos;
                m_PreviewUtility.camera.transform.rotation = rot;

                // Render with lighting enabled (includes all preview objects like grid)
                m_PreviewUtility.Render(true);

                // Read pixels from RenderTexture
                RenderTexture.active = tempRT;
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                // Cleanup lighting
                TeardownPreviewLightingAndFx(oldFog);

                return texture;
            }
            finally
            {
                // Restore state
                m_PreviewUtility.camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(tempRT);
            }
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public override void Cleanup()
        {
            // Unregister play mode state change callback
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            // Stop simulation
            SimulateDisable();

            // Destroy preview instance
            if (m_PreviewInstance != null)
            {
                Object.DestroyImmediate(m_PreviewInstance);
                m_PreviewInstance = null;
            }

            // Destroy grid plane
            if (m_GridPlane != null)
            {
                Object.DestroyImmediate(m_GridPlane);
                m_GridPlane = null;
            }

            // Clear renderer state cache
            if (m_OriginalRendererStates != null)
            {
                m_OriginalRendererStates.Clear();
                m_OriginalRendererStates = null;
            }

            // Cleanup preview utility (exception-safe to prevent GPU resource leaks)
            if (m_PreviewUtility != null)
            {
                try
                {
                    m_PreviewUtility.Cleanup();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[EffectViewer] PreviewRenderUtility cleanup failed: {e.Message}");
                }
                m_PreviewUtility = null;
            }

            // Reset camera state for next preview to ensure clean slate
            m_PreviewDir = m_DefaultPreviewDir;
            m_ZoomFactor = 1f;
            m_PivotPositionOffset = Vector3.zero;
            m_CameraFOV = 30f;
            m_Loaded = false;

            // Call base cleanup
            base.Cleanup();
        }
    }
}
