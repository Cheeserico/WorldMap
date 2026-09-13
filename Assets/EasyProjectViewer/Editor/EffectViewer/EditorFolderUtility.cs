using UnityEditor;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Utility class for Editor folder operations
    /// </summary>
    internal static class EditorFolderUtility
    {
        /// <summary>
        /// Create folder hierarchy if it doesn't exist
        /// </summary>
        /// <param name="folderPath">Full folder path (e.g., "Assets/EffectViewer/Settings")</param>
        public static void CreateFolderIfMissing(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            // Split path into components
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0]; // Start with "Assets"

            // Build path incrementally
            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;

            }
        }
    }
}
