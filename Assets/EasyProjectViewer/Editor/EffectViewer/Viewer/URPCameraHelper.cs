using UnityEngine;
using UnityEngine.Rendering;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Provides URP-specific camera setup helpers using reflection.
    /// Ensures compatibility across Built-in RP, URP, and HDRP without direct assembly references.
    /// </summary>
    internal static class URPCameraHelper
    {
        private static System.Type s_URPCameraDataType;
        private static bool s_TypeResolved;

        /// <summary>
        /// Ensures the camera has UniversalAdditionalCameraData when running under URP.
        /// Unity 6's URP requires this component; without it, RenderSingleCameraInternal
        /// throws NullReferenceException. Safe to call in any render pipeline.
        /// </summary>
        public static void EnsureURPCameraData(Camera camera)
        {
            if (camera == null) return;

            var type = GetURPCameraDataType();
            if (type == null) return;

            if (camera.GetComponent(type) != null) return;

            camera.gameObject.AddComponent(type);
        }

        private static System.Type GetURPCameraDataType()
        {
            if (s_TypeResolved) return s_URPCameraDataType;
            s_TypeResolved = true;

            var pipelineAsset = GraphicsSettings.defaultRenderPipeline;
            if (pipelineAsset == null) return null;

            if (!pipelineAsset.GetType().FullName.Contains("Universal")) return null;

            s_URPCameraDataType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");

            return s_URPCameraDataType;
        }
    }
}
