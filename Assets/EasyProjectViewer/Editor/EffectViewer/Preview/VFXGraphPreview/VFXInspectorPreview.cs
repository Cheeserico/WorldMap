#if VFX_GRAPH_AVAILABLE
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace EffectViewer.Editor
{
    /// <summary>
    /// VFXGraph inspector preview implementation
    /// Extends BaseEffectInspectorPreview with VFXGraph-specific functionality
    /// </summary>
    [CustomPreview(typeof(GameObject))]
    internal class VFXInspectorPreview : BaseEffectInspectorPreview
    {
        /// <summary>
        /// Default constructor for VFXGraph preview handler
        /// </summary>
        public VFXInspectorPreview()
        {
        }

        /// <summary>
        /// Check if target has VisualEffect for preview
        /// Note: Both previews appear as separate tabs when both components exist
        /// </summary>
        protected override bool HasEffectComponent(GameObject go)
        {
            return go.GetComponentInChildren<VisualEffect>() != null;
        }

        /// <summary>
        /// Start VFXGraph simulation
        /// </summary>
        protected override void StartEffectSimulation()
        {
            VisualEffect[] allVFX = m_PreviewInstance.GetComponentsInChildren<VisualEffect>();
            if (allVFX.Length == 0)
                return;

            foreach (var vfx in allVFX)
            {
                // Reinitialize VFX for clean start
                vfx.Reinit();

                // Set playRate to 0 to disable native simulation loop
                // Manual Simulate() in InspectorUpdate() will handle time step with playback speed
                vfx.playRate = 0f;

                // Start playback
                vfx.Play();
            }
        }

        /// <summary>
        /// Stop VFXGraph simulation
        /// </summary>
        protected override void StopEffectSimulation()
        {
            VisualEffect[] allVFX = m_PreviewInstance.GetComponentsInChildren<VisualEffect>();

            foreach (var vfx in allVFX)
            {
                // Stop playback
                vfx.Stop();

                // Reinitialize to completely reset state
                vfx.Reinit();
            }
        }

        /// <summary>
        /// Update VFXGraph playback speed
        /// Note: m_PlaybackSpeed is updated by base class OnPreviewSettings()
        /// InspectorUpdate() uses it for manual Simulate(delta * m_PlaybackSpeed) calls
        /// </summary>
        protected override void UpdatePlaybackSpeed(float speed)
        {
            // No action needed here - base class already updated m_PlaybackSpeed
            // Manual simulation in InspectorUpdate() will use the new value
        }

        /// <summary>
        /// Force VFXGraph to loop (for preview continuity)
        /// </summary>
        protected override void ForceLoopEffect(bool force)
        {
            if (m_PreviewInstance == null)
                return;

            VisualEffect[] allVFX = m_PreviewInstance.GetComponentsInChildren<VisualEffect>();

            foreach (var vfx in allVFX)
            {
                // Set loop if loop parameter exists in VFX asset
                if (vfx.HasBool("loop"))
                {
                    vfx.SetBool("loop", force);
                }
                // Note: VFXGraph loop behavior is typically controlled in the asset
                // Many VFX graphs loop by default
            }
        }

        /// <summary>
        /// Update loop for manual VFXGraph simulation with playback speed control
        /// Ensures playback speed changes are reflected in the preview
        /// </summary>
        protected override void InspectorUpdate()
        {
            // Calculate delta before calling base (which updates m_PreviousTime)
            double delta = EditorApplication.timeSinceStartup - m_PreviousTime;

            base.InspectorUpdate();

            if (m_Playing && m_PreviewInstance != null)
            {
                // Manual simulation of VFX with playback speed
                // This ensures speed changes are reflected (vfx.playRate alone doesn't work in preview scenes)
                VisualEffect[] allVFX = m_PreviewInstance.GetComponentsInChildren<VisualEffect>();
                foreach (var vfx in allVFX)
                {
                    vfx.Simulate((float)delta * m_PlaybackSpeed);
                }
            }
        }

        /// <summary>
        /// Get preview title
        /// </summary>
        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("VFX Graph Preview");
        }
    }
}
#endif
