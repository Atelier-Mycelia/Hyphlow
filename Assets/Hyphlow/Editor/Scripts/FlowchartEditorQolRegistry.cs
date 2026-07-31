using System;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Editor-only registry that keeps an up-to-date cache of all FlowchartEditorQol assets
    /// in the project and raises an event when the set changes.
    /// </summary>
    [InitializeOnLoad]
    public static class FlowchartEditorQolRegistry
    {
        static FlowchartEditorQolRegistry()
        {
            EnsureInitialized();//
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            // Optionally refresh on domain reload as well
        }

        public static void EnsureInitialized(bool force = false)
        {
            lock (_syncLock)
            {
                if (_cache.Count > 0 && !force)
                {
                    return;
                }

                RefreshInternal();
            }
        }

        private static readonly object _syncLock = new object();
        private static readonly List<FlowchartEditorQol> _cache = new List<FlowchartEditorQol>();

        internal static void RefreshInternal()
        {
            _cache.Clear();

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FlowchartEditorQol)}", 
                new[] { "Assets" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<FlowchartEditorQol>(path);
                if (asset != null)
                {
                    _cache.Add(asset);
                }
            }

            _cache.Sort(SortCacheInAlphabeticalOrder);
            RegistryRefreshed?.Invoke();
        }

        private static int SortCacheInAlphabeticalOrder(FlowchartEditorQol first, 
            FlowchartEditorQol second)
        {
            return string.Compare(first.name, second.name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Raised whenever the registry is refreshed (initialization, 
        /// asset changes, assembly reload).
        /// </summary>
        public static event Action RegistryRefreshed = delegate { };

        private static void OnAfterAssemblyReload()
        {
            EnsureInitialized(force: true);
        }

        public static IReadOnlyList<FlowchartEditorQol> GetAll()
        {
            // No defensive list here; the return type already specifies read-only,
            // so if the user tries mutating the list, that's on them.
            lock (_syncLock)
            {
                return _cache;
            }
        }

        /// <summary>
        /// Called from the asset postprocessor when assets change in the project.
        /// </summary>
        internal static void OnAssetsChanged(string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            // Quick heuristics: if any changed asset is of the type we care about, refresh.
            bool shouldRefresh = false;

            foreach (var path in imported)
            {
                if (IsAssetFlowchartEditorQol(path)) { shouldRefresh = true; break; }
            }

            if (!shouldRefresh)
            {
                foreach (var path in deleted)
                {
                    if (IsAssetFlowchartEditorQol(path)) { shouldRefresh = true; break; }
                }
            }

            if (!shouldRefresh)
            {
                foreach (var path in moved)
                {
                    if (IsAssetFlowchartEditorQol(path)) { shouldRefresh = true; break; }
                }
            }

            if (shouldRefresh)
            {
                RefreshInternal();
            }
        }

        private static bool IsAssetFlowchartEditorQol(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            var t = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            return t == typeof(FlowchartEditorQol);
        }

        public static int GetIndexOf(FlowchartEditorQol qol)
        {
            lock (_syncLock)
            {
                return _cache.IndexOf(qol);
            }
        }
    }

    // AssetPostprocessor that notifies the registry of imports/deletes/moves
    public class FlowchartEditorQolAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            FlowchartEditorQolRegistry.OnAssetsChanged(importedAssets, deletedAssets, 
                movedAssets, movedFromAssetPaths);
        }
    }
}