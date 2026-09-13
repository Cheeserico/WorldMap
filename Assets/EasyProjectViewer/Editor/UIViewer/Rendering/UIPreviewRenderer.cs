using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

namespace UIViewer.Editor.Rendering
{
    /// <summary>
    /// Renders UI Prefab previews using PreviewRenderUtility for complete
    /// scene isolation. Objects are never created in the user's active scene.
    ///
    /// Used by both UIInspectorPreview (real-time preview) and
    /// UIThumbnailGenerator (disk-cached thumbnails).
    /// </summary>
    public static class UIPreviewRenderer
    {
        private const float CameraDepthOffset = -10f;
        private const float CameraNearClip = 0.3f;
        private const float CameraFarClip = 1000f;
        private const int MinTextureSize = 16;
        private const float DefaultCanvasWidth = 1920f;
        private const float DefaultCanvasHeight = 1080f;

        private static readonly Color BackgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);

        /// <summary>
        /// Holds the state of a preview render session.
        /// Encapsulates PreviewRenderUtility setup, camera framing, and texture dimensions
        /// so that Render and RenderAsync share the same preparation logic.
        /// </summary>
        private sealed class PreviewSession : System.IDisposable
        {
            public PreviewRenderUtility PreviewUtility;
            public Camera Camera;
            public Bounds FrameBounds;
            public int TexWidth;
            public int TexHeight;

            public void Dispose()
            {
                PreviewUtility?.Cleanup();
                PreviewUtility = null;
            }
        }

        /// <summary>
        /// Creates a fully prepared preview session: PreviewRenderUtility, camera,
        /// canvas, instantiated prefab, layout, bounds, and texture dimensions.
        /// Caller is responsible for disposing the session.
        /// </summary>
        private static PreviewSession SetupPreviewSession(GameObject prefab, int resolution)
        {
            var previewUtility = new PreviewRenderUtility();
            var camera = previewUtility.camera;

            SetupCamera(camera);
            var canvas = CreateWorldSpaceCanvas(previewUtility, camera);
            var instance = InstantiatePrefabSafely(prefab, canvas, previewUtility);

            RebuildAllLayouts(canvas);
            FixZeroSizeRectTransform(instance, canvas);
            Canvas.ForceUpdateCanvases();
            ForceTMPMeshUpdates(canvas);

            var frameBounds = CalculateFrameBounds(instance, canvas);
            FrameCamera(camera, frameBounds);

            CalculateTextureDimensions(frameBounds, resolution, out int texWidth, out int texHeight);

            return new PreviewSession
            {
                PreviewUtility = previewUtility,
                Camera = camera,
                FrameBounds = frameBounds,
                TexWidth = texWidth,
                TexHeight = texHeight,
            };
        }

        /// <summary>
        /// Renders a UI Prefab and returns the captured Texture2D.
        /// Uses PreviewRenderUtility to avoid polluting the active scene.
        /// </summary>
        /// <param name="prefab">Source UI Prefab asset (not modified)</param>
        /// <param name="resolution">Maximum dimension of the output texture</param>
        /// <returns>Captured texture, or null on failure</returns>
        public static Texture2D Render(GameObject prefab, int resolution = 256)
        {
            if (prefab == null)
                return null;

            try
            {
                using (var session = SetupPreviewSession(prefab, resolution))
                {
                    return CaptureSync(session);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIViewer] Preview render failed for {prefab.name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Renders a UI Prefab with async GPU readback support.
        /// Calls <paramref name="onComplete"/> when the texture is ready.
        /// PreviewRenderUtility is cleaned up immediately after rendering;
        /// only the standalone RenderTexture copy is kept alive for readback.
        /// </summary>
        public static void RenderAsync(
            GameObject prefab,
            int resolution,
            System.Action<Texture2D> onComplete)
        {
            if (prefab == null)
            {
                onComplete?.Invoke(null);
                return;
            }

            // Check for ParticleSystem — Unity bug prevents preview (Issue#1399450)
            if (prefab.GetComponentsInChildren<ParticleSystem>().Length > 0)
            {
                Debug.LogWarning($"[UIViewer] Skipping preview for {prefab.name}: contains ParticleSystem (Unity limitation).");
                onComplete?.Invoke(null);
                return;
            }

            PreviewSession session = null;
            try
            {
                session = SetupPreviewSession(prefab, resolution);
                CaptureAsync(session, onComplete);
            }
            catch (System.Exception e)
            {
                session?.Dispose();
                Debug.LogError($"[UIViewer] Preview render failed for {prefab.name}: {e.Message}");
                onComplete?.Invoke(null);
            }
        }

        // ──────────────────────────────────────────────
        // Capture methods
        // ──────────────────────────────────────────────

        private static RenderTexture CreateRenderTarget(int width, int height)
        {
            int depth = 0;
#if UNITY_6000_0_OR_NEWER
            depth = 16;
#endif
            return new RenderTexture(width, height, depth, RenderTextureFormat.Default);
        }

        /// <summary>
        /// Renders the camera and reads pixels back synchronously.
        /// </summary>
        private static Texture2D CaptureSync(PreviewSession session)
        {
            var rt = CreateRenderTarget(session.TexWidth, session.TexHeight);
            session.Camera.targetTexture = rt;

            var savedRT = RenderTexture.active;
            RenderTexture.active = rt;

            URPRenderHelper.RenderCamera(session.Camera);

            var texture = new Texture2D(session.TexWidth, session.TexHeight, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, session.TexWidth, session.TexHeight), 0, 0);
            texture.Apply();

            RenderTexture.active = savedRT;
            session.Camera.targetTexture = null;
            Object.DestroyImmediate(rt);

            return texture;
        }

        /// <summary>
        /// Renders the camera, copies the result to a standalone RenderTexture via
        /// Graphics.Blit, then immediately disposes the PreviewRenderUtility.
        /// The async GPU readback runs against the standalone copy, avoiding
        /// lifetime issues with the preview scene's internal resources.
        /// </summary>
        private static void CaptureAsync(PreviewSession session, System.Action<Texture2D> onComplete)
        {
            var rt = CreateRenderTarget(session.TexWidth, session.TexHeight);
            session.Camera.targetTexture = rt;

            var savedRT = RenderTexture.active;
            RenderTexture.active = rt;

            URPRenderHelper.RenderCamera(session.Camera);

            // Copy to a standalone RT that outlives PreviewRenderUtility
            var standalone = CreateRenderTarget(session.TexWidth, session.TexHeight);
            Graphics.Blit(rt, standalone);

            // Clean up PreviewRenderUtility immediately — no longer needed
            RenderTexture.active = savedRT;
            session.Camera.targetTexture = null;
            Object.DestroyImmediate(rt);
            session.Dispose();

            var output = new Texture2D(session.TexWidth, session.TexHeight, TextureFormat.RGBA32, false);
            output.wrapMode = TextureWrapMode.Clamp;

            if (SystemInfo.supportsAsyncGPUReadback)
            {
                AsyncGPUReadback.Request(standalone, 0, TextureFormat.RGBA32, (request) =>
                {
                    // Guard against domain reload or premature destruction
                    if (output == null || standalone == null)
                    {
                        if (standalone != null) Object.DestroyImmediate(standalone);
                        if (output != null) Object.DestroyImmediate(output);
                        return;
                    }

                    if (request.hasError)
                    {
                        Debug.LogError("[UIViewer] Async GPU readback failed.");
                        Object.DestroyImmediate(standalone);
                        Object.DestroyImmediate(output);
                        onComplete?.Invoke(null);
                        return;
                    }

                    output.LoadRawTextureData(request.GetData<uint>());
                    output.Apply();

                    Object.DestroyImmediate(standalone);
                    onComplete?.Invoke(output);
                });
            }
            else
            {
                RenderTexture.active = standalone;
                output.ReadPixels(new Rect(0, 0, session.TexWidth, session.TexHeight), 0, 0);
                output.Apply();
                RenderTexture.active = savedRT;

                Object.DestroyImmediate(standalone);
                onComplete?.Invoke(output);
            }
        }

        // ──────────────────────────────────────────────
        // Setup helpers
        // ──────────────────────────────────────────────

        private static void SetupCamera(Camera camera)
        {
            camera.transform.localPosition = new Vector3(0, 0, CameraDepthOffset);
            camera.transform.localRotation = Quaternion.identity;
            camera.transform.localScale = Vector3.one;

            camera.orthographic = true;
            camera.backgroundColor = BackgroundColor;
            camera.clearFlags = CameraClearFlags.Color;
            camera.cameraType = CameraType.SceneView;
            camera.nearClipPlane = CameraNearClip;
            camera.farClipPlane = CameraFarClip;
            camera.useOcclusionCulling = false;

            URPRenderHelper.EnsureURPCameraData(camera);
        }

        private static Canvas CreateWorldSpaceCanvas(PreviewRenderUtility previewUtility, Camera camera)
        {
            var go = new GameObject("PreviewCanvas", typeof(Canvas));
            go.hideFlags = HideFlags.HideAndDontSave;
            previewUtility.AddSingleGO(go);

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            return canvas;
        }

        private static GameObject InstantiatePrefabSafely(
            GameObject prefab,
            Canvas canvas,
            PreviewRenderUtility previewUtility)
        {
            // Instantiate while inactive to prevent Awake/OnEnable side effects
            bool wasActive = prefab.activeSelf;
            prefab.SetActive(false);

            var instance = Object.Instantiate(prefab);
            // Immediately hide to prevent any rendering in the active scene
            instance.hideFlags = HideFlags.HideAndDontSave;

            // Strip custom scripts before activation
            try
            {
                UIComponentCleaner.StripNonEssentialScripts(instance);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UIViewer] Script cleanup warning: {e.Message}");
            }

            // Restore original prefab state
            prefab.SetActive(wasActive);

            // Move to preview scene, then activate and parent to canvas
            previewUtility.AddSingleGO(instance);
            instance.SetActive(true);
            instance.transform.SetParent(canvas.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localScale = Vector3.one;
            instance.transform.localRotation = Quaternion.identity;

            return instance;
        }

        private static void RebuildAllLayouts(Canvas canvas)
        {
            var rects = canvas.GetComponentsInChildren<RectTransform>();
            // Rebuild from leaves to root for correct layout propagation
            for (int i = rects.Length - 1; i >= 0; i--)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rects[i]);
            }
        }

        private static void FixZeroSizeRectTransform(GameObject instance, Canvas canvas)
        {
            var rootRect = instance.GetComponent<RectTransform>();
            if (rootRect == null)
                return;

            if (rootRect.rect.width > 0 && rootRect.rect.height > 0)
                return;

            // Determine reference dimensions from CanvasScaler or defaults
            float refWidth = DefaultCanvasWidth;
            float refHeight = DefaultCanvasHeight;

            if (canvas.TryGetComponent<CanvasScaler>(out var scaler))
            {
                refWidth = scaler.referenceResolution.x;
                refHeight = scaler.referenceResolution.y;
            }
            else if (canvas.TryGetComponent<RectTransform>(out var canvasRect))
            {
                if (canvasRect.rect.width > 0)
                    refWidth = canvasRect.rect.width;
                if (canvasRect.rect.height > 0)
                    refHeight = canvasRect.rect.height;
            }

            // Estimate element size from anchor spread
            var anchorSpread = rootRect.anchorMax - rootRect.anchorMin;
            float estimatedWidth = refWidth * Mathf.Abs(anchorSpread.x);
            float estimatedHeight = refHeight * Mathf.Abs(anchorSpread.y);

            // Scale up tiny elements to be visible
            if (estimatedWidth < 10f && estimatedHeight < 10f)
            {
                float scale = Mathf.Min(100f, 100f / Mathf.Max(0.01f, Mathf.Min(estimatedWidth, estimatedHeight)));
                estimatedWidth *= scale;
                estimatedHeight *= scale;
            }

            // Collapse anchors to center and apply computed size
            var center = rootRect.anchorMin + anchorSpread * 0.5f;
            rootRect.anchorMin = center;
            rootRect.anchorMax = center;
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, estimatedWidth);
            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, estimatedHeight);

            // Re-layout after size change
            RebuildAllLayouts(canvas);
        }

        private static void ForceTMPMeshUpdates(Canvas canvas)
        {
            var tmpTexts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmpTexts)
            {
                if (tmp.gameObject.activeInHierarchy)
                    tmp.ForceMeshUpdate(false, true);
            }
        }

        private static Bounds CalculateFrameBounds(GameObject instance, Canvas canvas)
        {
            var bounds = UIBoundsUtility.CalculateVisibleBounds(instance);

            // Snap to grid to allow RenderTexture reuse across similar-sized previews
            int gridSize = (canvas.renderMode == RenderMode.WorldSpace &&
                           (bounds.size.x < 50f || bounds.size.y < 50f))
                ? 1
                : 50;

            return UIBoundsUtility.SnapToGrid(bounds, gridSize);
        }

        private static void FrameCamera(Camera camera, Bounds bounds)
        {
            camera.transform.position = new Vector3(
                bounds.center.x,
                bounds.center.y,
                camera.transform.position.z
            );

            float boundsWidth = bounds.size.x;
            float boundsHeight = bounds.size.y;

            if (boundsWidth > boundsHeight)
            {
                float aspect = boundsWidth / boundsHeight;
                camera.orthographicSize = boundsWidth / (2f * aspect);
            }
            else
            {
                camera.orthographicSize = boundsHeight / 2f;
            }
        }

        private static void CalculateTextureDimensions(Bounds bounds, int maxSize, out int width, out int height)
        {
            width = maxSize;
            height = maxSize;

            if (bounds.size.x > 0 && bounds.size.y > 0)
            {
                if (bounds.size.x > bounds.size.y)
                    height = Mathf.RoundToInt(maxSize * bounds.size.y / bounds.size.x);
                else if (bounds.size.x < bounds.size.y)
                    width = Mathf.RoundToInt(maxSize * bounds.size.x / bounds.size.y);
            }

            width = Mathf.Max(MinTextureSize, width);
            height = Mathf.Max(MinTextureSize, height);
        }
    }
}
