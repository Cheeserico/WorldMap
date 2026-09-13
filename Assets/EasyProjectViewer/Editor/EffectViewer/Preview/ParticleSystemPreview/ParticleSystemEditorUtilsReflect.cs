// Based on UnityParticleSystemPreview by WuHuan
// https://github.com/akof1314/UnityParticleSystemPreview
// Copyright (c) 2018 WuHuan
// Licensed under MIT License

using System;
using UnityEngine;
using System.Reflection;
using UnityEditor;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Utility class to access Unity's internal ParticleSystemEditorUtils via Reflection
    /// Resolves environment-dependent preview issues by integrating with Unity's official particle management system
    /// </summary>
    public class ParticleSystemEditorUtilsReflect
    {
        private static Type realType;
        private static Type realType2;
        private static PropertyInfo property_editorResimulation;
        private static PropertyInfo property_editorPlaybackTime;
        private static Func<float> getFunc_editorPlaybackTime;
        private static PropertyInfo property_editorIsScrubbing;
        private static PropertyInfo property_lockedParticleSystem;
        private static MethodInfo method_StopEffect;
        private static bool s_InitializationFailed = false;

        public static void InitType()
        {
            if (realType != null || s_InitializationFailed)
                return;

            try
            {
                var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
                realType = assembly.GetType("UnityEditor.ParticleSystemEditorUtils");

                if (realType == null)
                {
                    Debug.LogError("[Preview:Reflect] Failed to get ParticleSystemEditorUtils type");
                    s_InitializationFailed = true;
                    return;
                }

#if UNITY_2018_1_OR_NEWER
                property_editorResimulation = realType.GetProperty("resimulation", BindingFlags.Static | BindingFlags.NonPublic);
                property_editorPlaybackTime = realType.GetProperty("playbackTime", BindingFlags.Static | BindingFlags.NonPublic);

                property_editorIsScrubbing = realType.GetProperty("playbackIsScrubbing", BindingFlags.Static | BindingFlags.NonPublic);
                property_lockedParticleSystem = realType.GetProperty("lockedParticleSystem", BindingFlags.Static | BindingFlags.NonPublic);

                realType2 = assembly.GetType("UnityEditor.ParticleSystemEffectUtils");
                if (realType2 == null)
                {
                    Debug.LogError("[Preview:Reflect] Failed to get ParticleSystemEffectUtils type");
                    s_InitializationFailed = true;
                    return;
                }
                method_StopEffect = realType2.GetMethod("StopEffect", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { }, new ParameterModifier[] { });
#else
                property_editorResimulation = realType.GetProperty("editorResimulation", BindingFlags.Static | BindingFlags.NonPublic);
                property_editorPlaybackTime = realType.GetProperty("editorPlaybackTime", BindingFlags.Static | BindingFlags.NonPublic);

                property_editorIsScrubbing = realType.GetProperty("editorIsScrubbing", BindingFlags.Static | BindingFlags.NonPublic);
                property_lockedParticleSystem = realType.GetProperty("lockedParticleSystem", BindingFlags.Static | BindingFlags.NonPublic);
                method_StopEffect = realType.GetMethod("StopEffect", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { }, new ParameterModifier[] { });
#endif

                // Validate all required properties/methods were found
                if (property_editorResimulation == null || property_editorPlaybackTime == null ||
                    property_editorIsScrubbing == null || property_lockedParticleSystem == null ||
                    method_StopEffect == null)
                {
                    Debug.LogError("[Preview:Reflect] Failed to get required properties or methods");
                    s_InitializationFailed = true;
                    return;
                }

                // Create delegate after null validation
                var getter = property_editorPlaybackTime.GetGetMethod(true);
                if (getter == null)
                {
                    Debug.LogError("[Preview:Reflect] Failed to get playbackTime getter method");
                    s_InitializationFailed = true;
                    return;
                }
                getFunc_editorPlaybackTime = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), getter);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Preview:Reflect] Error during Reflection initialization: {e.Message}");
                s_InitializationFailed = true;
            }
        }

        /// <summary>
        /// Check if reflection initialization was successful
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                InitType();
                return !s_InitializationFailed;
            }
        }

        public static bool editorResimulation
        {
            set
            {
                InitType();
                if (s_InitializationFailed) return;
                property_editorResimulation.SetValue(null, value, null);
            }
        }

        public static float editorPlaybackTime
        {
            get
            {
                InitType();
                if (s_InitializationFailed) return 0f;
                return getFunc_editorPlaybackTime();
            }
            set
            {
                InitType();
                if (s_InitializationFailed) return;
                property_editorPlaybackTime.SetValue(null, value, null);
            }
        }

        public static bool editorIsScrubbing
        {
            set
            {
                InitType();
                if (s_InitializationFailed) return;
                property_editorIsScrubbing.SetValue(null, value, null);
            }
        }

        /// <summary>
        /// Access Unity's internal lockedParticleSystem property
        /// Setting this ensures Unity's official particle management system recognizes and reliably updates the target particle system
        /// </summary>
        public static ParticleSystem lockedParticleSystem
        {
            get
            {
                InitType();
                if (s_InitializationFailed) return null;
                return (ParticleSystem)property_lockedParticleSystem.GetValue(null, null);
            }
            set
            {
                InitType();
                if (s_InitializationFailed) return;

                try
                {
                    property_lockedParticleSystem.SetValue(null, value, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Preview:Reflect] Error setting lockedParticleSystem: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Call Unity's internal StopEffect() to stop particle effects
        /// Must be called when preview ends to cleanup Unity's internal state
        /// </summary>
        public static void StopEffect()
        {
            InitType();
            if (s_InitializationFailed) return;

            try
            {
                method_StopEffect.Invoke(null, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Preview:Reflect] Error calling StopEffect(): {e.Message}");
            }
        }

        public static ParticleSystem GetRoot(ParticleSystem ps)
        {
            if (ps == null)
            {
                return null;
            }
            Transform transform = ps.transform;
            while (transform.parent && transform.parent.gameObject.GetComponent<ParticleSystem>() != null)
            {
                transform = transform.parent;
            }
            return transform.gameObject.GetComponent<ParticleSystem>();
        }
    }
}
