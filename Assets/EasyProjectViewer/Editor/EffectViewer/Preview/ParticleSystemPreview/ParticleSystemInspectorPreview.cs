// Based on concepts from UnityParticleSystemPreview by WuHuan
// https://github.com/akof1314/UnityParticleSystemPreview

using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// ParticleSystem inspector preview implementation
    /// Extends BaseEffectInspectorPreview with ParticleSystem-specific functionality
    /// Integrates with Unity's internal API to resolve environment-dependent issues
    /// </summary>
    [CustomPreview(typeof(GameObject))]
    internal class ParticleSystemInspectorPreview : BaseEffectInspectorPreview
    {
        /// <summary>
        /// Simulation mode flag
        /// true: Use lockedParticleSystem (Unity standard behavior - recommended)
        /// false: Use Simulate() method (manual control)
        /// </summary>
        private bool m_IsLockParticleSystem = true;

        /// <summary>
        /// Default constructor for ParticleSystem preview handler
        /// </summary>
        public ParticleSystemInspectorPreview()
        {
        }

        /// <summary>
        /// Check if target has ParticleSystem for preview
        /// </summary>
        protected override bool HasEffectComponent(GameObject go)
        {
            return go.GetComponentInChildren<ParticleSystem>() != null;
        }

        /// <summary>
        /// Start ParticleSystem simulation
        /// Resolves environment-dependent issues by setting Unity's internal lockedParticleSystem
        /// </summary>
        protected override void StartEffectSimulation()
        {
            ParticleSystem[] allPS = m_PreviewInstance.GetComponentsInChildren<ParticleSystem>();
            if (allPS.Length == 0)
            {
                Debug.LogWarning("[Preview] No ParticleSystem found");
                return;
            }

            // Register with Unity's internal API to resolve environment-dependent issues
            SetSimulateMode();

            // Check if reflection failed
            if (!ParticleSystemEditorUtilsReflect.IsAvailable)
            {
                Debug.LogWarning("[Preview] Failed to access Unity internal API. Falling back to manual simulation mode.");
                m_IsLockParticleSystem = false; // Force manual mode
            }

            if (m_IsLockParticleSystem)
            {
                // Method 1: Use lockedParticleSystem (Unity standard behavior)
                ParticleSystemEditorUtilsReflect.editorIsScrubbing = false;
            }

            // Set simulation speed and play
            int playedCount = 0;
            foreach (var ps in allPS)
            {
                // Skip ParticleSystems with disabled renderers (dummy parent systems)
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && !renderer.enabled)
                {
                    continue;
                }

                var main = ps.main;
                main.simulationSpeed = m_PlaybackSpeed;
                ps.Play();
                playedCount++;
            }
        }

        /// <summary>
        /// Stop ParticleSystem simulation
        /// Cleanup Unity's internal state to ensure reliable stop
        /// </summary>
        protected override void StopEffectSimulation()
        {
            // Cleanup Unity's internal state (resolves environment-dependent issues)
            if (m_IsLockParticleSystem)
            {
                ParticleSystemEditorUtilsReflect.editorIsScrubbing = false;
                ParticleSystemEditorUtilsReflect.editorPlaybackTime = 0f;
                ParticleSystemEditorUtilsReflect.StopEffect();
            }

            if (m_PreviewInstance == null) return;
            ParticleSystem[] allPS = m_PreviewInstance.GetComponentsInChildren<ParticleSystem>();

            int stoppedCount = 0;
            foreach (var ps in allPS)
            {
                // Skip ParticleSystems with disabled renderers (dummy parent systems)
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && !renderer.enabled)
                    continue;

                // Set simulation speed to 0 to completely freeze the simulation
                var main = ps.main;
                main.simulationSpeed = 0f;

                // Stop emitting and clear all particles
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // Clear all particles including children
                ps.Clear(true);
                stoppedCount++;
            }
        }

        /// <summary>
        /// Update ParticleSystem playback speed
        /// </summary>
        protected override void UpdatePlaybackSpeed(float speed)
        {
            ParticleSystem[] allPS = m_PreviewInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in allPS)
            {
                // Skip ParticleSystems with disabled renderers (dummy parent systems)
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && !renderer.enabled)
                    continue;

                var main = ps.main;
                main.simulationSpeed = speed;
            }
        }

        /// <summary>
        /// Force all particle systems to loop (for preview continuity)
        /// </summary>
        protected override void ForceLoopEffect(bool force)
        {
            if (m_PreviewInstance == null)
                return;

            ParticleSystem[] allPS = m_PreviewInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in allPS)
            {
                // Skip ParticleSystems with disabled renderers (dummy parent systems)
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && !renderer.enabled)
                    continue;

                var main = ps.main;
                main.loop = force;
            }
        }

        /// <summary>
        /// Get preview title
        /// </summary>
        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent("Particle Preview");
        }

        /// <summary>
        /// Set simulation mode
        /// Resolves environment-dependent issues by setting Unity's internal lockedParticleSystem
        /// </summary>
        private void SetSimulateMode()
        {
            if (m_PreviewInstance == null)
            {
                Debug.LogWarning("[Preview:Simulate] Preview instance is null");
                return;
            }

            ParticleSystem particleSystem = m_PreviewInstance.GetComponentInChildren<ParticleSystem>(true);
            if (particleSystem == null)
            {
                Debug.LogWarning("[Preview:Simulate] No ParticleSystem found");
                return;
            }

            // Reset per-prefab (avoid carrying over state from a previous prefab)
            m_IsLockParticleSystem = true;

            // If the root GameObject has no ParticleSystem, lockedParticleSystem cannot
            // cover sibling ParticleSystems — fall back to manual Simulate() mode.
            ParticleSystem rootPS = m_PreviewInstance.GetComponent<ParticleSystem>();
            if (rootPS == null)
            {
                m_IsLockParticleSystem = false;
            }

            if (m_IsLockParticleSystem)
            {
                // Register with Unity's internal lockedParticleSystem
                // This ensures Unity's official particle management system recognizes the target and reliably updates it
                if (ParticleSystemEditorUtilsReflect.lockedParticleSystem != particleSystem)
                {
                    ParticleSystemEditorUtilsReflect.lockedParticleSystem = particleSystem;
                }
            }
            else
            {
                // Unregister (manual control mode)
                ParticleSystemEditorUtilsReflect.lockedParticleSystem = null;
            }
        }
        /// <summary>
        /// Update loop for simulation
        /// Override to handle manual simulation when reflection is unavailable
        /// </summary>
        protected override void InspectorUpdate()
        {
            // Calculate delta before calling base (which updates m_PreviousTime)
            double delta = EditorApplication.timeSinceStartup - m_PreviousTime;

            base.InspectorUpdate();

            if (m_Playing && !m_IsLockParticleSystem && m_PreviewInstance != null)
            {
                // Manual simulation fallback
                ParticleSystem[] allPS = m_PreviewInstance.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in allPS)
                {
                    // Skip ParticleSystems with disabled renderers (dummy parent systems)
                    ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null && !renderer.enabled)
                        continue;

                    // Manually simulate each PS individually.
                    // withChildren=false because we already iterate all PS via GetComponentsInChildren,
                    // avoiding double-simulation for nested PS hierarchies.
                    ps.Simulate((float)delta * m_PlaybackSpeed, false, false);
                }
            }
        }
    }
}
