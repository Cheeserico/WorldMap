using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Static class for managing effect tags
    /// </summary>
    public static class EffectTagManager
    {
        private static EffectTagDatabase database;

        /// <summary>
        /// Initialize database (create if doesn't exist)
        /// </summary>
        public static void Initialize()
        {
            // Try to find existing database by type
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(EffectTagDatabase).Name}");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<EffectTagDatabase>(assetPath);
            }

            if (database == null)
            {
                // Create database if it doesn't exist
                database = ScriptableObject.CreateInstance<EffectTagDatabase>();

                // Determine database path relative to this script's location
                var scriptGuids = AssetDatabase.FindAssets("t:MonoScript EffectTagManager");
                string rootFolder = "Assets/EasyProjectViewer"; // Default fallback
                if (scriptGuids.Length > 0)
                {
                    string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                    string scriptDir = Path.GetDirectoryName(scriptPath);
                    // Navigate: Tag(current) -> EffectViewer -> Editor -> EasyProjectViewer
                    rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(scriptDir))).Replace("\\", "/");
                }

                // Create directory if it doesn't exist
                string[] folderParts = rootFolder.Split('/');
                string currentPath = folderParts[0];
                for (int i = 1; i < folderParts.Length; i++)
                {
                    string nextPath = currentPath + "/" + folderParts[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folderParts[i]);
                    }
                    currentPath = nextPath;
                }

                string databasePath = Path.Combine(rootFolder, "EffectTagDatabase.asset").Replace("\\", "/");
                AssetDatabase.CreateAsset(database, databasePath);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Save database
        /// </summary>
        public static void Save()
        {
            if (database != null)
            {
                EditorUtility.SetDirty(database);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Get all tags
        /// </summary>
        public static List<string> GetAllTags()
        {
            if (database == null) Initialize();
            return new List<string>(database.tags);
        }

        /// <summary>
        /// Add new tag
        /// </summary>
        public static bool AddTag(string tagName)
        {
            if (database == null) Initialize();

            tagName = tagName.Trim();
            if (string.IsNullOrWhiteSpace(tagName))
            {
                Debug.LogWarning("[Effect Viewer] Tag name cannot be empty.");
                return false;
            }

            if (database.tags.Contains(tagName))
            {
                Debug.LogWarning($"[Effect Viewer] Tag '{tagName}' already exists.");
                return false;
            }

            database.tags.Add(tagName);
            Save();
            return true;
        }

        /// <summary>
        /// Remove tag (remove from all effects)
        /// </summary>
        public static void RemoveTag(string tagName)
        {
            if (database == null) Initialize();

            if (!database.tags.Contains(tagName))
            {
                Debug.LogWarning($"[Effect Viewer] Tag '{tagName}' does not exist.");
                return;
            }

            // Remove this tag from all mappings
            foreach (var mapping in database.mappings)
            {
                mapping.tags.Remove(tagName);
            }

            // Remove from tag definitions
            database.tags.Remove(tagName);

            // Cleanup unused mappings
            database.CleanupUnusedMappings();

            Save();
        }

        /// <summary>
        /// Rename tag
        /// </summary>
        public static bool RenameTag(string oldName, string newName)
        {
            if (database == null) Initialize();

            newName = newName.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                Debug.LogWarning("[Effect Viewer] New tag name cannot be empty.");
                return false;
            }

            if (!database.tags.Contains(oldName))
            {
                Debug.LogWarning($"[Effect Viewer] Tag '{oldName}' does not exist.");
                return false;
            }

            if (database.tags.Contains(newName))
            {
                Debug.LogWarning($"[Effect Viewer] Tag '{newName}' already exists.");
                return false;
            }

            // Change tag definition
            int index = database.tags.IndexOf(oldName);
            database.tags[index] = newName;

            // Change tag name in all mappings
            foreach (var mapping in database.mappings)
            {
                for (int i = 0; i < mapping.tags.Count; i++)
                {
                    if (mapping.tags[i] == oldName)
                    {
                        mapping.tags[i] = newName;
                    }
                }
            }

            Save();
            return true;
        }

        /// <summary>
        /// Add tag to effect
        /// </summary>
        public static void AddTagToEffect(string guid, string tag)
        {
            if (database == null) Initialize();

            if (!database.tags.Contains(tag))
            {
                Debug.LogWarning($"[Effect Viewer] Tag '{tag}' does not exist. Add it first.");
                return;
            }

            var mapping = database.GetOrCreateMapping(guid);
            if (!mapping.tags.Contains(tag))
            {
                mapping.tags.Add(tag);
                Save();
            }
        }

        /// <summary>
        /// Remove tag from effect
        /// </summary>
        public static void RemoveTagFromEffect(string guid, string tag)
        {
            if (database == null) Initialize();

            var mapping = database.GetMapping(guid);
            if (mapping != null)
            {
                mapping.tags.Remove(tag);
                Save();
            }
        }

        /// <summary>
        /// Remove entire prefab mapping
        /// </summary>
        public static void RemoveEffectMapping(string guid)
        {
            if (database == null) Initialize();

            database.RemoveMapping(guid);
            Save();
        }

        /// <summary>
        /// Get list of tags assigned to effect
        /// </summary>
        public static List<string> GetEffectTags(string guid)
        {
            if (database == null) Initialize();

            var mapping = database.GetMapping(guid);
            if (mapping != null)
            {
                return new List<string>(mapping.tags);
            }

            return new List<string>();
        }

        /// <summary>
        /// Filter by specified tag
        /// </summary>
        public static List<EffectPrefabData> FilterByTag(List<EffectPrefabData> effects, string tag)
        {
            if (database == null) Initialize();

            return effects.Where(e =>
            {
                var mapping = database.GetMapping(e.Guid);
                return mapping != null && mapping.tags.Contains(tag);
            }).ToList();
        }

        /// <summary>
        /// Get count of effects with specified tag
        /// </summary>
        public static int GetEffectCountForTag(List<EffectPrefabData> allEffects, string tag)
        {
            if (database == null) Initialize();

            return allEffects.Count(e =>
            {
                var mapping = database.GetMapping(e.Guid);
                return mapping != null && mapping.tags.Contains(tag);
            });
        }

        /// <summary>
        /// Check if tag is being used
        /// </summary>
        public static bool IsTagUsed(string tag)
        {
            if (database == null) Initialize();
            return database.IsTagUsed(tag);
        }

        /// <summary>
        /// Reset database (for debugging)
        /// </summary>
        public static void ResetDatabase()
        {
            if (database != null)
            {
                database.tags.Clear();
                database.mappings.Clear();
                Save();
            }
        }
    }
}
