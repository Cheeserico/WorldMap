using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Dialog window to create new Tag
    /// </summary>
    public class TagCreateDialog : EditorWindow
    {
        private string newTagName = "";
        private System.Action onTagCreated;

        public static void ShowDialog(System.Action onCreated = null)
        {
            var window = CreateInstance<TagCreateDialog>();
            window.titleContent = new GUIContent("Create New Tag");
            window.minSize = new Vector2(350, 120);
            window.maxSize = new Vector2(350, 120);
            window.onTagCreated = onCreated;
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Tag Name:", EditorStyles.boldLabel);
            GUI.SetNextControlName("TagNameField");
            newTagName = EditorGUILayout.TextField(newTagName);

            // Initial focus
            if (Event.current.type == EventType.Layout)
            {
                EditorGUI.FocusTextInControl("TagNameField");
            }

            // Create with Enter key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                CreateTag();
                Event.current.Use();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(80), GUILayout.Height(25)))
            {
                CreateTag();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(25)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateTag()
        {
            if (!string.IsNullOrWhiteSpace(newTagName))
            {
                if (EffectTagManager.AddTag(newTagName))
                {
                    onTagCreated?.Invoke();
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        $"Failed to create tag '{newTagName}'.\nIt may already exist or be invalid.",
                        "OK"
                    );
                }
            }
        }
    }
}
