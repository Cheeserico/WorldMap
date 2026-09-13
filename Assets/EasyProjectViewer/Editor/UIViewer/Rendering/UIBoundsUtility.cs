using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIViewer.Editor.Rendering
{
    /// <summary>
    /// Calculates visible bounds of UI hierarchies for camera framing.
    /// Iterates RectTransforms and uses world-space corners to determine
    /// the minimal enclosing rectangle of all visible UI elements.
    /// </summary>
    internal static class UIBoundsUtility
    {
        private static readonly Vector3[] CornerBuffer = new Vector3[4];

        /// <summary>
        /// Computes a world-space bounding box that encloses all visible UI elements
        /// (those with Graphic, TextMeshProUGUI, or RectMask2D components).
        /// Children hidden by a RectMask2D ancestor are excluded, but the mask
        /// element itself is included if it has visible content.
        /// </summary>
        public static Bounds CalculateVisibleBounds(GameObject root)
        {
            var rects = root.GetComponentsInChildren<RectTransform>();
            bool initialized = false;
            var enclosing = new Bounds();

            // Track active masks as a stack so nested masks and sibling subtrees
            // are handled correctly. A mask's scope ends when we leave its subtree.
            var maskStack = new Stack<RectMask2D>();

            for (int i = 0; i < rects.Length; i++)
            {
                var rt = rects[i];

                if (!rt.gameObject.activeInHierarchy)
                    continue;

                // Pop masks whose subtree we have left
                while (maskStack.Count > 0 && !rt.IsChildOf(maskStack.Peek().transform))
                    maskStack.Pop();

                // Detect new mask on this node
                var mask = rt.GetComponent<RectMask2D>();
                bool isNewMask = mask != null && mask.enabled;

                // Skip the root Canvas itself (we want content bounds, not container bounds)
                if (rt.gameObject == root && rt.GetComponent<Canvas>() != null)
                    continue;

                // Skip children clipped by an active RectMask2D (but not the mask element itself)
                if (maskStack.Count > 0 && !isNewMask)
                    continue;

                // Only include elements that contribute to visual output
                if (!HasVisibleContent(rt))
                    continue;

                rt.GetWorldCorners(CornerBuffer);

                // corners[0] = bottom-left, corners[2] = top-right
                var elementBounds = new Bounds(CornerBuffer[0], Vector3.zero);
                elementBounds.Encapsulate(CornerBuffer[2]);

                if (!initialized)
                {
                    enclosing = elementBounds;
                    initialized = true;
                }
                else
                {
                    enclosing.Encapsulate(elementBounds);
                }

                // Push new mask AFTER including the mask element in bounds
                if (isNewMask)
                    maskStack.Push(mask);
            }

            return enclosing;
        }

        /// <summary>
        /// Rounds up the bounds dimensions to the nearest multiple of <paramref name="gridSize"/>.
        /// Helps reuse similarly-sized RenderTextures across multiple previews.
        /// </summary>
        public static Bounds SnapToGrid(Bounds bounds, int gridSize)
        {
            var size = bounds.size;
            size.x = Mathf.CeilToInt(size.x / gridSize) * gridSize;
            size.y = Mathf.CeilToInt(size.y / gridSize) * gridSize;
            bounds.size = size;
            return bounds;
        }

        private static bool HasVisibleContent(RectTransform rt)
        {
            if (rt.GetComponent<Graphic>() != null)
                return true;

            if (rt.GetComponent<RectMask2D>() != null)
                return true;

            // TextMeshPro uses its own rendering path separate from uGUI Graphic
            var tmp = rt.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
                return true;

            return false;
        }
    }
}
