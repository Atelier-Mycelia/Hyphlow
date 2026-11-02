using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Finds the UXML that row handlers are built from. A handler's
    /// <see cref="RowVisualHandlerAttribute"/> says where its default template lives; clients
    /// can swap that for any content type via <see cref="Override"/> without subclassing the handler.
    /// </summary>
    public static class RowTemplates
    {
        /// <summary>
        /// Makes rows for variables holding <paramref name="contentType"/> use the template at
        /// <paramref name="pathToTemplate"/> (relative to a Resources folder, no extension).
        /// Overrides don't survive a domain reload, so register them from an
        /// [InitializeOnLoadMethod].
        /// </summary>
        public static void Override(Type contentType, string pathToTemplate)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            if (string.IsNullOrEmpty(pathToTemplate))
            {
                throw new ArgumentException("Path to the template can't be empty.", nameof(pathToTemplate));
            }

            _overrides[contentType] = pathToTemplate;
        }

        public static bool RemoveOverride(Type contentType)
        {
            return contentType != null && _overrides.Remove(contentType);
        }

        public static void ClearOverrides()
        {
            _overrides.Clear();
        }

        public static VisualTreeAsset Resolve(Type handlerType)
        {
            if (handlerType == null)
            {
                Debug.LogError("RowTemplates was asked to resolve a template for a null handler type.");
                return null;
            }

            RowVisualHandlerAttribute attribute = handlerType.GetCustomAttribute<RowVisualHandlerAttribute>();
            if (attribute == null)
            {
                LogOnce(handlerType.FullName, $"{handlerType.Name} is missing RowVisualHandlerAttribute.");
                return null;
            }

            string path = _overrides.TryGetValue(attribute.ContentType, out string overridePath) ?
                overridePath :
                attribute.PathToTemplate;

            if (_cache.TryGetValue(path, out VisualTreeAsset cached) && cached != null)
            {
                return cached;
            }

            VisualTreeAsset loaded = Resources.Load<VisualTreeAsset>(path);
            if (loaded == null)
            {
                LogOnce(path, $"Template for {handlerType.Name} not found at '{path}'.\n" +
                    "Please update the path in the RowVisualHandlerAttribute of the former, " +
                    "or in the RowTemplates.Override that targets it.");
                return null;
            }

            _cache[path] = loaded;
            return loaded;
        }

        public static void ClearCache()
        {
            _cache.Clear();
            _alreadyLogged.Clear();
        }

        private static void LogOnce(string key, string message)
        {
            if (_alreadyLogged.Add(key))
            {
                Debug.LogError(message);
            }
        }

        private static readonly Dictionary<Type, string> _overrides = new Dictionary<Type, string>();
        private static readonly Dictionary<string, VisualTreeAsset> _cache =
            new Dictionary<string, VisualTreeAsset>(StringComparer.Ordinal);
        private static readonly HashSet<string> _alreadyLogged = new HashSet<string>(StringComparer.Ordinal);
    }
}
