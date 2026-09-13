using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Generates custom thumbnails for ParticleSystem and VFXGraph
    /// Renders effects using PreviewRenderUtility
    /// </summary>
    public class EffectThumbnailGenerator
    {
        private PreviewRenderUtility previewRenderUtility;
        private GameObject previewInstance;

        /// <summary>
        /// Initialize thumbnail generation
        /// </summary>
        public void Initialize()
        {
            if (previewRenderUtility == null)
            {
                previewRenderUtility = new PreviewRenderUtility();

                // Camera settings
                previewRenderUtility.camera.transform.position = new Vector3(0, 0, -6);
                previewRenderUtility.camera.transform.rotation = Quaternion.identity;
                previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                previewRenderUtility.camera.backgroundColor = EffectViewerSettings.Instance.thumbnailBackgroundColor;
                previewRenderUtility.camera.fieldOfView = 30f;
                previewRenderUtility.camera.nearClipPlane = 0.3f;
                previewRenderUtility.camera.farClipPlane = 1000f;

                // Ensure URP camera data (required for Unity 6+)
                URPCameraHelper.EnsureURPCameraData(previewRenderUtility.camera);

                // Add light
                var lightA = previewRenderUtility.lights[0];
                lightA.intensity = 1.4f;
                lightA.transform.rotation = Quaternion.Euler(40f, 40f, 0);
            }
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Cleanup()
        {
            if (previewInstance != null)
            {
                Object.DestroyImmediate(previewInstance);
                previewInstance = null;
            }

            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
                previewRenderUtility = null;
            }
        }

        /// <summary>
        /// Generate static thumbnail (at time specified in Settings)
        /// Priority: ParticleSystem > VFXGraph
        /// </summary>
        /// <param name="prefab">Prefab containing ParticleSystem or VFXGraph</param>
        /// <param name="width">Thumbnail width</param>
        /// <param name="height">Thumbnail height</param>
        /// <returns>Generated thumbnail</returns>
        public Texture2D GenerateStaticThumbnail(GameObject prefab, int width = 128, int height = 128)
        {
            // Get time from Settings
            float simulationTime = EffectViewerSettings.Instance.thumbnailSimulationTime;

            // Priority: ParticleSystem > VFXGraph
            var particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems != null && particleSystems.Length > 0)
            {
                return GenerateParticleSystemThumbnail(prefab, simulationTime, width, height);
            }

            // Note: VFX Graph thumbnails are handled by VFXDeferredThumbnailCapture (requires multi-frame GPU warmup)

            return null;
        }

        /// <summary>
        /// Generate ParticleSystem thumbnail at specified time
        /// </summary>
        /// <param name="prefab">Prefab containing ParticleSystem</param>
        /// <param name="time">Simulation time (seconds)</param>
        /// <param name="width">Thumbnail width</param>
        /// <param name="height">Thumbnail height</param>
        /// <returns>Generated thumbnail</returns>
        private Texture2D GenerateParticleSystemThumbnail(GameObject prefab, float time, int width = 128, int height = 128)
        {
            if (prefab == null || previewRenderUtility == null)
                return null;

            try
            {
                // Create preview instance
                if (previewInstance != null)
                {
                    Object.DestroyImmediate(previewInstance);
                }
                previewInstance = Object.Instantiate(prefab);
                previewInstance.hideFlags = HideFlags.HideAndDontSave;

                // Get ParticleSystem and simulate
                var particleSystems = previewInstance.GetComponentsInChildren<ParticleSystem>(true);
                if (particleSystems == null || particleSystems.Length == 0)
                {
                    Object.DestroyImmediate(previewInstance);
                    return null;
                }

                // Simulate all particle systems
                foreach (var ps in particleSystems)
                {
                    ps.Stop();
                    ps.Clear();
                    ps.Simulate(time, true, true);
                    ps.Play();
                }

                // Camera positioning (unified with InspectorPreview)
                float distance = EffectViewerSettings.Instance.thumbnailCameraDistance * EffectViewerSettings.Instance.thumbnailZoomFactor;

                // Camera angles from settings
                float pitch = EffectViewerSettings.Instance.thumbnailCameraPitch;
                float yaw = EffectViewerSettings.Instance.thumbnailCameraYaw;
                Vector3 pivotOffset = EffectViewerSettings.Instance.thumbnailPivotOffset;

                // Position camera (same logic as InspectorPreview)
                Quaternion rot = Quaternion.Euler(-pitch, -yaw, 0f);
                Vector3 camPos = rot * (Vector3.forward * -distance) + previewInstance.transform.position + pivotOffset;

                previewRenderUtility.camera.transform.position = camPos;
                previewRenderUtility.camera.transform.rotation = rot;
                previewRenderUtility.camera.fieldOfView = EffectViewerSettings.Instance.thumbnailCameraFOV;

                // Rendering
                previewRenderUtility.BeginPreview(new Rect(0, 0, width, height), GUIStyle.none);

                // Add object to PreviewRenderUtility scene and render
                previewRenderUtility.AddSingleGO(previewInstance);

                previewRenderUtility.camera.Render();

                // EndPreview() returns Texture, convert to Texture2D
                Texture resultTexture = previewRenderUtility.EndPreview();
                Texture2D thumbnail = ConvertRenderTextureToTexture2D(resultTexture, width, height);

                return thumbnail;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Effect Viewer] Failed to generate thumbnail for {prefab.name}: {e.Message}");
                Debug.LogException(e);
                return null;
            }
            finally
            {
                if (previewInstance != null)
                {
                    Object.DestroyImmediate(previewInstance);
                    previewInstance = null;
                }
            }
        }

        /// <summary>
        /// Convert Texture (from PreviewRenderUtility) to Texture2D
        /// </summary>
        /// <param name="sourceTexture">Source texture from PreviewRenderUtility.EndPreview()</param>
        /// <param name="width">Target width</param>
        /// <param name="height">Target height</param>
        /// <returns>Converted Texture2D</returns>
        private Texture2D ConvertRenderTextureToTexture2D(Texture sourceTexture, int width, int height)
        {
            // Create Texture2D
            Texture2D thumbnail = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Copy pixel data via RenderTexture (try-finally to prevent RT leak on exception)
            RenderTexture previousRT = RenderTexture.active;
            RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

            try
            {
                Graphics.Blit(sourceTexture, tempRT);
                RenderTexture.active = tempRT;

                thumbnail.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                thumbnail.Apply();
            }
            finally
            {
                RenderTexture.active = previousRT;
                RenderTexture.ReleaseTemporary(tempRT);
            }

            return thumbnail;
        }

        /// <summary>
        /// Calculate bounds containing all Renderers in GameObject
        /// </summary>
        private Bounds CalculateBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(go.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            // Fallback for extremely small bounds
            if (bounds.size.magnitude < 0.1f)
            {
                bounds.size = Vector3.one;
            }

            return bounds;
        }

        // Note: VFX Graph thumbnails are generated by VFXDeferredThumbnailCapture
        // (GPU particles require multi-frame warmup that PreviewRenderUtility cannot provide)
    }
}
