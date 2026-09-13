using UnityEngine;
using UIViewer.Editor.Rendering;

namespace UIViewer.Editor
{
    /// <summary>
    /// Generates static thumbnail images for UI Prefabs.
    /// Delegates all rendering to <see cref="UIPreviewRenderer"/>.
    /// </summary>
    public class UIThumbnailGenerator
    {
        private const int DefaultResolution = 256;

        public void Initialize()
        {
            // No initialization needed — rendering is stateless via UIPreviewRenderer.
        }

        public void Cleanup()
        {
            // No cleanup needed — preview scenes are disposed per-render.
        }

        /// <summary>
        /// Renders a UI Prefab to a Texture2D thumbnail.
        /// </summary>
        /// <param name="prefab">UI Prefab containing RectTransform hierarchy</param>
        /// <param name="width">Desired width (used as max resolution)</param>
        /// <param name="height">Desired height (ignored — aspect ratio is preserved)</param>
        /// <returns>Generated thumbnail texture, or null on failure</returns>
        public Texture2D GenerateStaticThumbnail(GameObject prefab, int width = DefaultResolution, int height = DefaultResolution)
        {
            int resolution = Mathf.Max(width, height);
            return UIPreviewRenderer.Render(prefab, resolution);
        }
    }
}
