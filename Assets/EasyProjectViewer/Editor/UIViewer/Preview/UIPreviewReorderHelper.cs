#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UIViewer.Editor
{
    /// <summary>
    /// Uses reflection to reorder preview tabs in Inspector window and force-select
    /// the active UI preview tab.
    /// Prevents Unity 6's built-in "Inspected Events" tab from overriding UI Preview.
    /// </summary>
    internal static class UIPreviewReorderHelper
    {
        private static bool s_CacheBuilt;
        private static bool s_TypesFound;

        private static Type s_InspectorWindowType;
        private static FieldInfo s_AllInspectorsInfo;
        private static FieldInfo s_PreviewsInfo;
        private static FieldInfo s_SelectedPreviewInfo;

        public static void ScheduleReorderPreviews()
        {
            EditorApplication.update -= ReorderPreviews;
            EditorApplication.update += ReorderPreviews;
        }

        private static void ReorderPreviews()
        {
            EditorApplication.update -= ReorderPreviews;

            if (!s_CacheBuilt)
                BuildReflectionCache();

            if (!s_TypesFound)
                return;

            IList allInspectors = (IList)s_AllInspectorsInfo.GetValue(null);
            if (allInspectors == null)
                return;

            foreach (var inspectorWindow in allInspectors)
            {
                IList previews = (IList)s_PreviewsInfo.GetValue(inspectorWindow);
                if (previews == null)
                    continue;

                // Find the first active UIInspectorPreview
                object activePreview = null;
                int index = -1;
                for (int i = 0; i < previews.Count; i++)
                {
                    var uiPreview = previews[i] as UIInspectorPreview;
                    if (uiPreview != null && uiPreview.IsUIPreviewActive)
                    {
                        activePreview = previews[i];
                        index = i;
                        break;
                    }
                }

                if (activePreview == null)
                    continue;

                // Swap to front if not already first
                if (index > 0)
                {
                    var tmp = previews[0];
                    previews[0] = previews[index];
                    previews[index] = tmp;
                }

                // Force-select the UI preview tab
                if (s_SelectedPreviewInfo != null)
                {
                    s_SelectedPreviewInfo.SetValue(inspectorWindow, activePreview);
                }
            }
        }

        private static void BuildReflectionCache()
        {
            s_CacheBuilt = true;

            try
            {
                var editorWindowTypes = typeof(EditorWindow).Assembly.GetTypes();
                s_InspectorWindowType = editorWindowTypes.FirstOrDefault(t => t.Name == "InspectorWindow");

                if (s_InspectorWindowType != null)
                {
                    var searchType = s_InspectorWindowType;
                    while (searchType != null)
                    {
                        if (s_AllInspectorsInfo == null)
                        {
                            s_AllInspectorsInfo = searchType.GetField(
                                "m_AllInspectors",
                                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                        }

                        if (s_PreviewsInfo == null)
                        {
                            s_PreviewsInfo = searchType.GetField(
                                "m_Previews",
                                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        }

                        if (s_SelectedPreviewInfo == null)
                        {
                            s_SelectedPreviewInfo = searchType.GetField(
                                "m_SelectedPreview",
                                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        }

                        if (s_AllInspectorsInfo != null && s_PreviewsInfo != null && s_SelectedPreviewInfo != null)
                            break;

                        searchType = searchType.BaseType;
                    }

                    s_TypesFound = s_AllInspectorsInfo != null && s_PreviewsInfo != null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UIPreviewReorderHelper] Reflection cache failed: {e.Message}");
                s_TypesFound = false;
            }
        }
    }
}
#endif
