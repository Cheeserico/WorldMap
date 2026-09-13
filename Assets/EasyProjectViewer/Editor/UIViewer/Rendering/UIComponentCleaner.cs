using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UIViewer.Editor.Rendering
{
    /// <summary>
    /// Strips non-essential MonoBehaviours from instantiated prefabs before preview rendering.
    /// Prevents ExecuteAlways / Awake side effects from custom scripts during thumbnail capture.
    /// </summary>
    internal static class UIComponentCleaner
    {
        private static readonly HashSet<string> PreservedNamespacePrefixes = new HashSet<string>
        {
            "UnityEngine",
            "UnityEditor",
            "TMPro"
        };

        /// <summary>
        /// Removes all custom MonoBehaviours from the hierarchy, keeping only
        /// Unity built-in and TextMeshPro components intact.
        /// </summary>
        public static void StripNonEssentialScripts(GameObject root)
        {
            var scripts = root.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (var script in scripts)
            {
                if (script == null)
                    continue;

                if (IsPreservedScript(script))
                    continue;

                if (HasDependentComponent(script))
                    continue;

                RemoveWithDependencies(script);
            }
        }

        private static bool IsPreservedScript(MonoBehaviour script)
        {
            var ns = script.GetType().Namespace;
            if (ns == null)
                return false;

            return PreservedNamespacePrefixes.Any(prefix => ns.StartsWith(prefix));
        }

        /// <summary>
        /// Checks whether any other component on the same GameObject
        /// declares this component as a [RequireComponent] dependency.
        /// </summary>
        private static bool HasDependentComponent(Component target)
        {
            var targetType = target.GetType();
            var siblings = target.gameObject.GetComponents<Component>();

            return siblings.Any(sibling =>
            {
                if (sibling == null || sibling == target)
                    return false;

                return sibling.GetType()
                    .GetCustomAttributes(typeof(RequireComponent), true)
                    .Cast<RequireComponent>()
                    .Any(attr =>
                        (attr.m_Type0 != null && attr.m_Type0.IsAssignableFrom(targetType)) ||
                        (attr.m_Type1 != null && attr.m_Type1.IsAssignableFrom(targetType)) ||
                        (attr.m_Type2 != null && attr.m_Type2.IsAssignableFrom(targetType))
                    );
            });
        }

        /// <summary>
        /// Destroys a component and any components it depends on via [RequireComponent],
        /// using an iterative approach with a stack to avoid deep recursion.
        /// Preserved-namespace components (Unity built-ins, TMPro) are never removed,
        /// even if they appear as dependencies of a custom script being stripped.
        /// </summary>
        private static void RemoveWithDependencies(Component target)
        {
            var pending = new Stack<Component>();
            pending.Push(target);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current == null)
                    continue;

                // Never destroy transforms
                if (current is Transform || current is RectTransform)
                    continue;

                // Never destroy preserved-namespace components (Unity built-ins, TMPro)
                if (current is MonoBehaviour mb && IsPreservedScript(mb))
                    continue;

                // Collect dependencies before destroying
                var dependencies = CollectDependencies(current);

                Object.DestroyImmediate(current);

                foreach (var dep in dependencies)
                {
                    if (dep != null)
                        pending.Push(dep);
                }
            }
        }

        /// <summary>
        /// Collects the components that <paramref name="component"/> depends on
        /// via its [RequireComponent] attributes.
        /// </summary>
        private static IEnumerable<Component> CollectDependencies(Component component)
        {
            var go = component.gameObject;

            return component.GetType()
                .GetCustomAttributes(typeof(RequireComponent), true)
                .Cast<RequireComponent>()
                .SelectMany(attr => new[] { attr.m_Type0, attr.m_Type1, attr.m_Type2 })
                .Where(type => type != null)
                .Select(type => go.GetComponent(type))
                .Where(comp => comp != null);
        }
    }
}
