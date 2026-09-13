using System.Collections.Generic;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Effect type
    /// </summary>
    public enum EffectType
    {
        Unknown,
        ParticleSystem,
        VFXGraph
    }

    /// <summary>
    /// Data model for Prefab containing ParticleSystem
    /// </summary>
    public class EffectPrefabData
    {
        /// <summary>Prefab asset path</summary>
        public string AssetPath { get; set; }

        /// <summary>Prefab GUID</summary>
        public string Guid { get; set; }

        /// <summary>Prefab name</summary>
        public string Name { get; set; }

        /// <summary>Prefab GameObject</summary>
        public GameObject Prefab { get; set; }

        /// <summary>Number of contained ParticleSystems</summary>
        public int ParticleSystemCount { get; set; }

        /// <summary>Effect type</summary>
        public EffectType Type { get; set; }

        /// <summary>Static thumbnail (for Phase 1)</summary>
        public Texture2D StaticThumbnail { get; set; }

        /// <summary>Animation frames (used in Phase 2)</summary>
        public List<Texture2D> AnimationFrames { get; set; }

        /// <summary>Whether cache is valid</summary>
        public bool IsCached { get; set; }

        /// <summary>Maximum Duration of effect (seconds)</summary>
        public float MaxDuration { get; set; }

        /// <summary>Whether this prefab was manually added to the viewer</summary>
        public bool IsManuallyAdded { get; set; }

        public EffectPrefabData()
        {
            AnimationFrames = new List<Texture2D>();
        }

        /// <summary>
        /// Cleanup resources
        /// </summary>
        public void Cleanup()
        {
            if (StaticThumbnail != null && !UnityEditor.AssetDatabase.Contains(StaticThumbnail))
            {
                Object.DestroyImmediate(StaticThumbnail);
                StaticThumbnail = null;
            }

            if (AnimationFrames != null)
            {
                foreach (var frame in AnimationFrames)
                {
                    if (frame != null && !UnityEditor.AssetDatabase.Contains(frame))
                    {
                        Object.DestroyImmediate(frame);
                    }
                }
                AnimationFrames.Clear();
            }
        }
    }
}
