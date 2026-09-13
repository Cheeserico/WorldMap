using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Custom inspector for EffectViewerSettings
    /// </summary>
    [CustomEditor(typeof(EffectViewerSettings))]
    public class EffectViewerSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Display default inspector
            DrawDefaultInspector();

            // Display information
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("These settings are used for thumbnail generation in the Effect Viewer window.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
