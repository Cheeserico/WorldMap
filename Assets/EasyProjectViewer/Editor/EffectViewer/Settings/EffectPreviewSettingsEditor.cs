using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Custom inspector for EffectPreviewSettings
    /// </summary>
    [CustomEditor(typeof(EffectPreviewSettings))]
    public class EffectPreviewSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Display default inspector
            DrawDefaultInspector();


            // Display grid material information
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Grid material uses Built-in Render Pipeline shader. URP/HDRP environments may require a custom grid material.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
