using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Dialog window to rename Tag
    /// </summary>
    public class TagRenameDialog : EditorWindow
    {
        private string oldTagName;
        private string newTagName = "";
        private System.Action onTagRenamed;

        public static void ShowDialog(string currentTagName, System.Action onRenamed = null)
        {
            var window = CreateInstance<TagRenameDialog>();
            window.titleContent = new GUIContent("Rename Tag");
            window.minSize = new Vector2(350, 150);
            window.maxSize = new Vector2(350, 150);
            window.oldTagName = currentTagName;
            window.newTagName = currentTagName;
            window.onTagRenamed = onRenamed;
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Current Name:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(oldTagName, EditorStyles.helpBox);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("New Name:", EditorStyles.boldLabel);
            GUI.SetNextControlName("NewTagNameField");
            newTagName = EditorGUILayout.TextField(newTagName);

            // Initial focus
            if (Event.current.type == EventType.Layout)
            {
                EditorGUI.FocusTextInControl("NewTagNameField");
            }

            // Rename with Enter key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                RenameTag();
                Event.current.Use();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Rename", GUILayout.Width(80), GUILayout.Height(25)))
            {
                RenameTag();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(25)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RenameTag()
        {
            if (!string.IsNullOrWhiteSpace(newTagName) && newTagName != oldTagName)
            {
                if (EffectTagManager.RenameTag(oldTagName, newTagName))
                {
                    onTagRenamed?.Invoke();
                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        $"Failed to rename tag to '{newTagName}'.\nIt may already exist or be invalid.",
                        "OK"
                    );
                }
            }
        }
    }
}
