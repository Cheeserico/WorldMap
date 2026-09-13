#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UIViewer.Editor.Rendering;
using Object = UnityEngine.Object;
#if UNITY_6000_4_OR_NEWER
using ObjectId = UnityEngine.EntityId;
#else
using ObjectId = System.Int32;
#endif

namespace UIViewer.Editor
{
    /// <summary>
    /// Displays a live preview of UI Prefabs in the Inspector's Preview tab.
    /// Manages a per-prefab texture cache keyed by file content hash
    /// so previews update automatically when the source prefab changes.
    /// </summary>
    [CustomPreview(typeof(GameObject))]
    public class UIInspectorPreview : ObjectPreview
    {
        private const int StandardResolution = 256;
        private const int CanvasRootResolution = 512;
        private const int MaxCacheSize = 50;

        private GUIContent titleContent;

        /// <summary>
        /// In-memory cache: maps prefab Object → (preview texture, file MD5 hash, access order).
        /// Hash is checked on Initialize to detect prefab modifications.
        /// When the cache exceeds MaxCacheSize, the least recently accessed entries are evicted.
        /// </summary>
        private static readonly Dictionary<Object, (Texture2D texture, string hash, long accessOrder)> PreviewCache
            = new Dictionary<Object, (Texture2D, string, long)>();

        private static long s_AccessCounter;

        // ──────────────────────────────────────────────
        // Target resolution — floating preview windows do not update target properly
        // ──────────────────────────────────────────────

        public override Object target
        {
            get
            {
                if (m_Targets == null || (m_Targets.Length == 1 && base.target != Selection.activeObject))
                    return Selection.activeObject;
                return base.target;
            }
        }

#if UNITY_2021_1_OR_NEWER
        public void OnDisable() => Cleanup();

        public override void Cleanup()
        {
            base.Cleanup();
        }
#endif

        // ──────────────────────────────────────────────
        // Visibility & validation
        // ──────────────────────────────────────────────

        private readonly Dictionary<ObjectId, bool> visibilityCache = new Dictionary<ObjectId, bool>();

        /// <summary>
        /// Whether this preview instance is active (used by UIPreviewReorderHelper).
        /// </summary>
        internal bool IsUIPreviewActive => IsPreviewAvailable();

        private bool IsPreviewAvailable()
        {
            if (target == null) return false;

#if UNITY_6000_4_OR_NEWER
            ObjectId id = target.GetEntityId();
#else
            ObjectId id = target.GetInstanceID();
#endif
            if (visibilityCache.TryGetValue(id, out bool cached))
                return cached;

            bool available = !EditorApplication.isPlayingOrWillChangePlaymode
                             && UIViewerSettings.Instance.enableInspectorPreview
                             && IsUIPrefab();

            visibilityCache[id] = available;
            return available;
        }

        private bool IsUIPrefab()
        {
            if (PrefabUtility.GetPrefabAssetType(target) == PrefabAssetType.NotAPrefab)
                return false;
            if (!AssetDatabase.Contains(target))
                return false;

            var go = target as GameObject;
            return go != null && go.GetComponent<RectTransform>() != null;
        }

        // ──────────────────────────────────────────────
        // ObjectPreview overrides
        // ──────────────────────────────────────────────

        public override GUIContent GetPreviewTitle()
        {
            if (!IsPreviewAvailable())
                return base.GetPreviewTitle();

            if (titleContent == null)
                titleContent = EditorGUIUtility.TrTextContent("UI Preview");
            return titleContent;
        }

        public override bool HasPreviewGUI()
        {
            bool available = IsPreviewAvailable();
            if (available)
                UIPreviewReorderHelper.ScheduleReorderPreviews();
            return available || base.HasPreviewGUI();
        }

        public override void Initialize(Object[] targets)
        {
            // Clear visibility cache on re-initialization to prevent unbounded growth
            visibilityCache.Clear();

            if (!IsPreviewAvailable())
            {
                base.Initialize(targets);
                return;
            }

            if (targets == null) return;

            foreach (var t in targets)
            {
                if (!PreviewCache.ContainsKey(t) || HasPrefabChanged(t))
                    GeneratePreview(t);
            }

            base.Initialize(targets);
        }

        public override void OnPreviewSettings()
        {
            base.OnPreviewSettings();
            if (!IsPreviewAvailable()) return;

            if (GUILayout.Button("Update"))
                GeneratePreview(target);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!IsPreviewAvailable()) return;

            base.OnPreviewGUI(r, background);

            if (PreviewCache.TryGetValue(target, out var entry) && entry.texture != null)
            {
                // Update access order for LRU eviction
                PreviewCache[target] = (entry.texture, entry.hash, ++s_AccessCounter);
                GUI.DrawTexture(r, entry.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Label(r, $"{target.name} — preview not available.");
            }
        }

        // ──────────────────────────────────────────────
        // Preview generation
        // ──────────────────────────────────────────────

        private static void GeneratePreview(Object prefab)
        {
            var go = prefab as GameObject;
            if (go == null) return;

            // Higher resolution for Canvas-root prefabs (full screens)
            bool isCanvasRoot = go.GetComponent<Canvas>() != null;
            int resolution = isCanvasRoot ? CanvasRootResolution : StandardResolution;

            UIPreviewRenderer.RenderAsync(go, resolution, texture =>
            {
                StorePreview(prefab, texture);
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            });
        }

        private static void StorePreview(Object prefab, Texture2D texture)
        {
            if (texture == null) return;

            // Dispose old texture before replacing
            if (PreviewCache.TryGetValue(prefab, out var old) && old.texture != null)
                Object.DestroyImmediate(old.texture);

            PreviewCache[prefab] = (texture, ComputeFileHash(prefab), ++s_AccessCounter);

            EvictIfOverLimit();
        }

        private static void EvictIfOverLimit()
        {
            // Purge entries whose key Object has been destroyed (e.g. deleted prefabs)
            var staleKeys = PreviewCache.Keys.Where(k => k == null).ToList();
            foreach (var key in staleKeys)
            {
                if (PreviewCache.TryGetValue(key, out var stale) && stale.texture != null)
                    Object.DestroyImmediate(stale.texture);
                PreviewCache.Remove(key);
            }

            // Evict least recently accessed entries until within limit (O(n) per eviction)
            while (PreviewCache.Count > MaxCacheSize)
            {
                Object oldestKey = null;
                long oldestOrder = long.MaxValue;

                foreach (var kvp in PreviewCache)
                {
                    if (kvp.Value.accessOrder < oldestOrder)
                    {
                        oldestOrder = kvp.Value.accessOrder;
                        oldestKey = kvp.Key;
                    }
                }

                if (oldestKey == null) break;

                if (PreviewCache.TryGetValue(oldestKey, out var entry) && entry.texture != null)
                    Object.DestroyImmediate(entry.texture);

                PreviewCache.Remove(oldestKey);
            }
        }

        // ──────────────────────────────────────────────
        // Public API for external cache invalidation
        // ──────────────────────────────────────────────

        /// <summary>
        /// Forces re-generation of cached previews for the specified prefabs.
        /// Called by UIPrefabAssetPostprocessor when prefabs are modified.
        /// </summary>
        public static void RegeneratePreviewCache(List<Object> prefabs)
        {
            foreach (var prefab in prefabs)
            {
                if (PreviewCache.TryGetValue(prefab, out var entry) && entry.texture != null)
                    Object.DestroyImmediate(entry.texture);

                PreviewCache.Remove(prefab);
                GeneratePreview(prefab);
            }
        }

        // ──────────────────────────────────────────────
        // Change detection via file hash
        // ──────────────────────────────────────────────

        private static bool HasPrefabChanged(Object prefab)
        {
            if (!PreviewCache.TryGetValue(prefab, out var entry))
                return true;
            return entry.hash != ComputeFileHash(prefab);
        }

        private static string ComputeFileHash(Object prefab)
        {
            try
            {
                string assetPath = AssetDatabase.GetAssetPath(prefab);
                string fullPath = System.IO.Path.Combine(
                    Application.dataPath.Substring(0, Application.dataPath.Length - 6),
                    assetPath);

                using (var md5 = MD5.Create())
                using (var stream = System.IO.File.OpenRead(fullPath))
                {
                    var bytes = md5.ComputeHash(stream);
                    return System.BitConverter.ToString(bytes).Replace("-", "");
                }
            }
            catch
            {
                // File locked or inaccessible — treat as changed
                return string.Empty;
            }
        }
    }
}
#endif
