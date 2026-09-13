using UnityEditor;
using UnityEngine;

namespace EffectViewer.Editor
{
    /// <summary>
    /// GUIStyle factory for EffectViewerWindow
    /// </summary>
    internal static class EffectViewerStyles
    {
        private const int THUMBNAIL_PADDING = 5;

        /// <summary>
        /// Generate solid color texture (for selection styles)
        /// </summary>
        public static Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Create thumbnail box style (normal)
        /// </summary>
        public static GUIStyle CreateThumbnailBoxStyle()
        {
            return new GUIStyle(GUI.skin.box)
            {
                margin = new RectOffset(THUMBNAIL_PADDING, THUMBNAIL_PADDING, THUMBNAIL_PADDING, THUMBNAIL_PADDING),
                padding = new RectOffset(5, 5, 5, 5)
            };
        }

        /// <summary>
        /// Create thumbnail box style (selected)
        /// </summary>
        public static GUIStyle CreateSelectedThumbnailStyle()
        {
            return new GUIStyle(GUI.skin.box)
            {
                margin = new RectOffset(THUMBNAIL_PADDING, THUMBNAIL_PADDING, THUMBNAIL_PADDING, THUMBNAIL_PADDING),
                padding = new RectOffset(5, 5, 5, 5),
                normal = { background = MakeTexture(2, 2, new Color(0.2f, 0.5f, 0.8f, 0.5f)) }
            };
        }

        /// <summary>
        /// Create thumbnail label style
        /// </summary>
        public static GUIStyle CreateThumbnailLabelStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 11
            };
        }

        /// <summary>
        /// Create tag button style (normal)
        /// </summary>
        public static GUIStyle CreateTagButtonStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(2, 2, 2, 2),
                normal =
                {
                    background = MakeTexture(2, 2, new Color(0.25f, 0.25f, 0.25f, 1f)),
                    textColor = new Color(0.9f, 0.9f, 0.9f, 1f)
                },
                hover =
                {
                    background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 1f)),
                    textColor = Color.white
                },
                active =
                {
                    background = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.2f, 1f))
                }
            };
        }

        /// <summary>
        /// Create tag button style (selected)
        /// </summary>
        public static GUIStyle CreateSelectedTagButtonStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(2, 2, 2, 2),
                normal =
                {
                    background = MakeTexture(2, 2, new Color(0.2f, 0.5f, 0.8f, 0.8f)),
                    textColor = Color.white
                },
                hover =
                {
                    background = MakeTexture(2, 2, new Color(0.25f, 0.55f, 0.85f, 0.9f)),
                    textColor = Color.white
                },
                active =
                {
                    background = MakeTexture(2, 2, new Color(0.15f, 0.45f, 0.75f, 0.8f))
                }
            };
        }

        /// <summary>
        /// Create tag button style (drag hover)
        /// </summary>
        public static GUIStyle CreateDragHoverTagButtonStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(2, 2, 2, 2),
                normal =
                {
                    background = MakeTexture(2, 2, new Color(0.3f, 0.7f, 0.3f, 0.9f)),
                    textColor = Color.white
                },
                hover =
                {
                    background = MakeTexture(2, 2, new Color(0.35f, 0.75f, 0.35f, 0.95f)),
                    textColor = Color.white
                },
                active =
                {
                    background = MakeTexture(2, 2, new Color(0.25f, 0.65f, 0.25f, 0.9f))
                }
            };
        }
    }
}
