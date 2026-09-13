using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UIViewer.Editor.Rendering
{
    /// <summary>
    /// Handles URP-specific rendering workarounds for editor preview scenes.
    /// URP's 2D lighting pipeline requires SceneView.currentDrawingSceneView to be set,
    /// which is null when rendering outside of a SceneView context.
    /// </summary>
    internal static class URPRenderHelper
    {
        private static readonly Lazy<FieldInfo> SceneViewField = new Lazy<FieldInfo>(() =>
            typeof(SceneView).GetField(
                "s_CurrentDrawingSceneView",
                BindingFlags.NonPublic | BindingFlags.Static
            )
        );

        /// <summary>
        /// Whether the current project uses Universal Render Pipeline.
        /// </summary>
        public static bool IsURP
        {
            get
            {
                var pipeline = GraphicsSettings.defaultRenderPipeline;
                return pipeline != null && pipeline.GetType().FullName.Contains("Universal");
            }
        }

        /// <summary>
        /// Renders the camera, applying the URP SceneView workaround if needed.
        /// Without this workaround, URP 2D lighting causes a NullReferenceException
        /// when SceneView.currentDrawingSceneView is null.
        /// </summary>
        public static void RenderCamera(Camera camera)
        {
            if (!NeedsWorkaround())
            {
                camera.Render();
                return;
            }

            var fallbackView = SceneView.lastActiveSceneView;
            if (fallbackView == null)
                return;

            var original = SceneView.currentDrawingSceneView;
            try
            {
                OverrideCurrentSceneView(fallbackView);
                camera.Render();
            }
            finally
            {
                OverrideCurrentSceneView(original);
            }
        }

        private static bool NeedsWorkaround()
        {
            return SceneView.currentDrawingSceneView == null && IsURP;
        }

        private static void OverrideCurrentSceneView(SceneView view)
        {
            SceneViewField.Value?.SetValue(null, view);
        }

        private static Type s_URPCameraDataType;
        private static bool s_CameraDataTypeResolved;

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

        private static Type GetURPCameraDataType()
        {
            if (s_CameraDataTypeResolved) return s_URPCameraDataType;
            s_CameraDataTypeResolved = true;

            if (!IsURP) return null;

            s_URPCameraDataType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");

            return s_URPCameraDataType;
        }
    }
}
