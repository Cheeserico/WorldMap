using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Utility class for refreshing EffectViewerWindow
    /// </summary>
    internal static class EffectViewerWindowRefresher
    {
        /// <summary>
        /// Refresh all open EffectViewerWindow instances
        /// </summary>
        public static void RefreshAll()
        {
            try
            {
                var windows = Resources.FindObjectsOfTypeAll<EffectViewerWindow>();
                foreach (var window in windows)
                {
                    window.RefreshAfterAssetChange();
                    window.Repaint();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Effect Viewer] Failed to refresh window: {e.Message}");
            }
        }

        /// <summary>
        /// Notify all open windows that a new effect prefab was added
        /// </summary>
        /// <param name="guid">Prefab GUID</param>
        public static void NotifyPrefabAdded(string guid)
        {
            try
            {
                var windows = Resources.FindObjectsOfTypeAll<EffectViewerWindow>();
                foreach (var window in windows)
                {
                    window.AddEffectPrefab(guid);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Effect Viewer] Failed to notify prefab added: {e.Message}");
            }
        }
    }
}
