using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    [InitializeOnLoad]
    public static class DefaultFlowchartConfigMaintenance
    {
        private const string ResourcesRootPath = "Assets/Resources";
        private const string RelativeResourcesFolderPath = "AtMycelia/Hyphlow";
        private const string AssetName = "FcDefaultConfig";
        private const string SearchFilter = "t:FlowchartGlobalDefaults";

        private static readonly string _defaultAssetPath =
            $"{ResourcesRootPath}/{RelativeResourcesFolderPath}/{AssetName}.asset";

        static DefaultFlowchartConfigMaintenance()
        {
            AssemblyReloadEvents.afterAssemblyReload -= EnsureFcGlobalDefaults;
            AssemblyReloadEvents.afterAssemblyReload += EnsureFcGlobalDefaults;

            EditorApplication.delayCall += EnsureFcGlobalDefaults;
        }

        public static void EnsureFcGlobalDefaults()
        {
            string[] guids = AssetDatabase.FindAssets(SearchFilter);
            List<string> foundPaths = new List<string>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    foundPaths.Add(path);
                }
            }

            if (foundPaths.Count == 0)
            {
                FlowchartGlobalDefaults created = CreateDefaultConfigAsset();
                FlowchartGlobalDefaults.S = created;
                return;
            }

            string primaryPath = ChoosePrimaryPath(foundPaths);
            DeleteExtras(foundPaths, primaryPath);

            FlowchartGlobalDefaults primary =
                AssetDatabase.LoadAssetAtPath<FlowchartGlobalDefaults>(primaryPath);

            if (primary == null)
            {
                primary = CreateDefaultConfigAsset();
            }

            FlowchartGlobalDefaults.S = primary;
        }

        private static string ChoosePrimaryPath(IList<string> paths)
        {
            if (paths.Contains(_defaultAssetPath))
            {
                return _defaultAssetPath;
            }

            return paths.OrderBy(path => path).First();
        }

        private static void DeleteExtras(IList<string> allPaths, string keepPath)
        {
            bool deletedAny = false;

            for (int i = 0; i < allPaths.Count; i++)
            {
                string path = allPaths[i];
                if (path == keepPath)
                {
                    continue;
                }

                bool deleted = AssetDatabase.DeleteAsset(path);
                if (deleted)
                {
                    deletedAny = true;
                }
            }

            if (deletedAny)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static FlowchartGlobalDefaults CreateDefaultConfigAsset()
        {
            EnsureFolderPath($"{ResourcesRootPath}/{RelativeResourcesFolderPath}");

            FlowchartGlobalDefaults config = ScriptableObject.CreateInstance<FlowchartGlobalDefaults>();
            AssetDatabase.CreateAsset(config, _defaultAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<FlowchartGlobalDefaults>(_defaultAssetPath);
        }

        private static void EnsureFolderPath(string absoluteFolderPath)
        {
            string normalized = absoluteFolderPath.Replace("\\", "/");
            string[] parts = normalized.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}