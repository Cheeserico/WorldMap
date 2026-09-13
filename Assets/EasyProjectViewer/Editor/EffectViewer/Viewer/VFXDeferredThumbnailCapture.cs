#if VFX_GRAPH_AVAILABLE
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.VFX;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Deferred capture system for VFX Graph thumbnails.
    /// Uses PreviewRenderUtility (same as InspectorPreview) with multi-frame warmup.
    /// PreviewRenderUtility is reused across queue items to avoid repeated GPU resource allocation.
    /// </summary>
    internal static class VFXDeferredThumbnailCapture
    {
        private const int WarmupFrames = 6;

        private struct CaptureRequest
        {
            public string guid;
            public string assetPath;
            public GameObject prefab;
            public int textureSize;
        }

        private struct ActiveCapture
        {
            public string guid;
            public string assetPath;
            public GameObject instance;
            public int frameCount;
            public int textureSize;
        }

        private static Queue<CaptureRequest> _queue = new Queue<CaptureRequest>();
        private static ActiveCapture? _current = null;
        private static bool _isProcessing = false;
        private static PreviewRenderUtility _sharedPreviewUtility = null;
        private static List<(string guid, string filePath)> _pendingImports = new List<(string, string)>();
        private static List<string> _pendingGuids = new List<string>();

        public static void Enqueue(string guid, string assetPath, GameObject prefab, int textureSize)
        {
            if (prefab == null)
                return;

            _queue.Enqueue(new CaptureRequest
            {
                guid = guid,
                assetPath = assetPath,
                prefab = prefab,
                textureSize = textureSize
            });

            if (!_isProcessing)
            {
                _isProcessing = true;
                EditorApplication.update -= ProcessQueue;
                EditorApplication.update += ProcessQueue;
            }
        }

        private static void ProcessQueue()
        {
            if (_current == null)
            {
                if (_queue.Count == 0)
                {
                    // Queue empty — batch import all pending thumbnails, then cleanup
                    CleanupSharedPreviewUtility();
                    BatchImportPendingThumbnails();
                    EditorApplication.update -= ProcessQueue;
                    _isProcessing = false;
                    return;
                }

                _current = SetupCapture(_queue.Dequeue());
                if (_current == null)
                    return;
            }

            var cap = _current.Value;
            cap.frameCount++;

            // Render one frame (same pattern as BaseEffectInspectorPreview)
            RenderFrame(cap);

            if (cap.frameCount >= WarmupFrames)
            {
                // Final render and capture
                Texture2D thumbnail = CaptureFrame(cap);

                if (thumbnail != null)
                {
                    // Save to disk only (no AssetDatabase import yet)
                    string filePath = EffectThumbnailManager.SaveThumbnailRaw(cap.guid, thumbnail);
                    if (filePath != null)
                    {
                        _pendingImports.Add((cap.guid, filePath));
                        _pendingGuids.Add(cap.guid);
                    }
                }
                else
                {
                    Debug.LogWarning($"[Effect Viewer] Failed to capture VFX thumbnail for: {cap.assetPath}");
                }

                CleanupInstance(cap);
                _current = null;
            }
            else
            {
                _current = cap;
            }
        }

        private static ActiveCapture? SetupCapture(CaptureRequest request)
        {
            GameObject instance = null;
            try
            {
                var settings = EffectViewerSettings.Instance;

                // Reuse or create PreviewRenderUtility
                if (_sharedPreviewUtility == null)
                {
                    _sharedPreviewUtility = new PreviewRenderUtility();
                }

                // Instantiate VFX and reset transform to origin (same pattern as ParticleSystem)
                instance = Object.Instantiate(request.prefab);
                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                _sharedPreviewUtility.AddSingleGO(instance);

                // Camera setup (same as EffectThumbnailGenerator.GenerateParticleSystemThumbnail)
                _sharedPreviewUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                _sharedPreviewUtility.camera.backgroundColor = settings.thumbnailBackgroundColor;
                _sharedPreviewUtility.camera.nearClipPlane = 0.3f;
                _sharedPreviewUtility.camera.farClipPlane = 1000f;
                _sharedPreviewUtility.camera.fieldOfView = settings.thumbnailCameraFOV;

                float distance = settings.thumbnailCameraDistance * settings.thumbnailZoomFactor;
                Quaternion rot = Quaternion.Euler(-settings.thumbnailCameraPitch, -settings.thumbnailCameraYaw, 0f);
                Vector3 camPos = rot * (Vector3.forward * -distance) + instance.transform.position + settings.thumbnailPivotOffset;
                _sharedPreviewUtility.camera.transform.position = camPos;
                _sharedPreviewUtility.camera.transform.rotation = rot;

                // Ensure URP camera data (required for Unity 6+)
                URPCameraHelper.EnsureURPCameraData(_sharedPreviewUtility.camera);

                // Light setup
                _sharedPreviewUtility.lights[0].intensity = 1.4f;
                _sharedPreviewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);

                // Initialize VFX
                float simTime = settings.thumbnailSimulationTime;
                foreach (var vfx in instance.GetComponentsInChildren<VisualEffect>())
                {
                    vfx.Reinit();
                    if (simTime > 0)
                    {
                        uint steps = (uint)Mathf.Max(1, Mathf.RoundToInt(simTime * 60f));
                        vfx.Simulate(1f / 60f, steps);
                    }
                    vfx.Play();
                }

                return new ActiveCapture
                {
                    guid = request.guid,
                    assetPath = request.assetPath,
                    instance = instance,
                    frameCount = 0,
                    textureSize = request.textureSize
                };
            }
            catch (System.Exception e)
            {
                if (instance != null)
                    Object.DestroyImmediate(instance);
                Debug.LogError($"[Effect Viewer] Error setting up VFX capture: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Render one warmup frame (result discarded)
        /// </summary>
        private static void RenderFrame(ActiveCapture cap)
        {
            Rect rect = new Rect(0, 0, cap.textureSize, cap.textureSize);

            InternalEditorUtility.SetCustomLighting(_sharedPreviewUtility.lights, new Color(0.1f, 0.1f, 0.1f, 0f));
            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            _sharedPreviewUtility.BeginPreview(rect, GUIStyle.none);
            _sharedPreviewUtility.Render(true);
            _sharedPreviewUtility.EndPreview();

            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
            InternalEditorUtility.RemoveCustomLighting();
        }

        /// <summary>
        /// Final render and capture to Texture2D
        /// </summary>
        private static Texture2D CaptureFrame(ActiveCapture cap)
        {
            int size = cap.textureSize;
            Rect rect = new Rect(0, 0, size, size);

            InternalEditorUtility.SetCustomLighting(_sharedPreviewUtility.lights, new Color(0.1f, 0.1f, 0.1f, 0f));
            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            _sharedPreviewUtility.BeginPreview(rect, GUIStyle.none);
            _sharedPreviewUtility.Render(true);
            Texture resultTexture = _sharedPreviewUtility.EndPreview();

            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
            InternalEditorUtility.RemoveCustomLighting();

            // Convert to Texture2D via Blit (try-finally to prevent RT leak on exception)
            Texture2D thumbnail = new Texture2D(size, size, TextureFormat.RGBA32, false);
            RenderTexture previousRT = RenderTexture.active;
            RenderTexture tempRT = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);

            try
            {
                Graphics.Blit(resultTexture, tempRT);
                RenderTexture.active = tempRT;
                thumbnail.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                thumbnail.Apply();
            }
            finally
            {
                RenderTexture.active = previousRT;
                RenderTexture.ReleaseTemporary(tempRT);
            }

            return thumbnail;
        }

        private static void CleanupInstance(ActiveCapture cap)
        {
            if (cap.instance != null)
                Object.DestroyImmediate(cap.instance);
        }

        private static void BatchImportPendingThumbnails()
        {
            if (_pendingImports.Count == 0)
                return;

            // Batch import all saved thumbnails at once
            EffectThumbnailManager.BatchImportThumbnails(_pendingImports);

            // Notify EffectViewerWindow for all captured thumbnails
            var guidsToNotify = new List<string>(_pendingGuids);
            EditorApplication.delayCall += () =>
            {
                var windows = Resources.FindObjectsOfTypeAll<EffectViewerWindow>();
                foreach (var window in windows)
                {
                    foreach (var guid in guidsToNotify)
                    {
                        window.UpdateSinglePrefabThumbnail(guid);
                    }
                }
                EditorApplication.RepaintProjectWindow();
            };

            _pendingImports.Clear();
            _pendingGuids.Clear();
        }

        private static void CleanupSharedPreviewUtility()
        {
            _sharedPreviewUtility?.Cleanup();
            _sharedPreviewUtility = null;
        }
    }
}
#endif
