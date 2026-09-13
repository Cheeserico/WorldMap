using System.Collections.Generic;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Database storing Tag definitions and Tag mappings to effects
    /// </summary>
    public class EffectTagDatabase : ScriptableObject
    {
        /// <summary>List of all registered Tags</summary>
        public List<string> tags = new List<string>();

        /// <summary>GUID → Tags mapping</summary>
        public List<EffectTagMapping> mappings = new List<EffectTagMapping>();

        /// <summary>
        /// Get mapping for specified GUID (create if not exists)
        /// </summary>
        public EffectTagMapping GetOrCreateMapping(string guid)
        {
            var mapping = mappings.Find(m => m.guid == guid);
            if (mapping == null)
            {
                mapping = new EffectTagMapping { guid = guid };
                mappings.Add(mapping);
            }
            return mapping;
        }

        /// <summary>
        /// Get mapping for specified GUID
        /// </summary>
        public EffectTagMapping GetMapping(string guid)
        {
            return mappings.Find(m => m.guid == guid);
        }

        /// <summary>
        /// Check if Tag is being used
        /// </summary>
        public bool IsTagUsed(string tag)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.tags.Contains(tag))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Cleanup unused mappings
        /// </summary>
        public void CleanupUnusedMappings()
        {
            mappings.RemoveAll(m => m.tags.Count == 0);
        }

        /// <summary>
        /// Remove mapping for specified GUID
        /// </summary>
        public void RemoveMapping(string guid)
        {
            mappings.RemoveAll(m => m.guid == guid);
        }

        /// <summary>
        /// Reset all Tags and mappings
        /// </summary>
        public void ResetAll()
        {
            tags.Clear();
            mappings.Clear();
        }
    }

    /// <summary>
    /// Mapping between effect (GUID) and Tags
    /// </summary>
    [System.Serializable]
    public class EffectTagMapping
    {
        /// <summary>Prefab GUID</summary>
        public string guid;

        /// <summary>List of Tags assigned to this effect</summary>
        public List<string> tags = new List<string>();
    }
}
