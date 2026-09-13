using UnityEditor;

namespace EffectViewer.Editor
{
    /// <summary>
    /// Processor to retrieve GUID before asset deletion
    /// </summary>
    public class EffectAssetDeletionProcessor : UnityEditor.AssetModificationProcessor
    {
        /// <summary>
        /// Callback invoked before asset deletion
        /// </summary>
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            // Folder deletion: process all prefabs inside the folder
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { assetPath });

                foreach (string guid in prefabGuids)
                {
                    EffectTagManager.RemoveEffectMapping(guid);
                    EffectThumbnailManager.ClearThumbnail(guid);
                }

                if (prefabGuids.Length > 0)
                {
                    EditorApplication.delayCall += EffectViewerWindowRefresher.RefreshAll;
                }
            }
            // Single prefab deletion
            else if (assetPath.EndsWith(".prefab"))
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                if (!string.IsNullOrEmpty(guid))
                {
                    EffectTagManager.RemoveEffectMapping(guid);
                    EffectThumbnailManager.ClearThumbnail(guid);
                    EditorApplication.delayCall += EffectViewerWindowRefresher.RefreshAll;
                }
            }

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
