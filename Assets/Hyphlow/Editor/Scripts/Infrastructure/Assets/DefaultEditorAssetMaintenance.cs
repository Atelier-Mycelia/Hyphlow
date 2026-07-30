using System;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// For ensuring that certain editor-only assets are created and maintained,
    /// such as the HyphlowEditorSysAssets asset.
    /// </summary>
    public static class DefaultEditorAssetMaintenance 
    {
        public static void InitializeAfterAssembliesLoaded()
        {
            _framesToWait = 10;
            AssemblyReloadEvents.afterAssemblyReload -= DoTheEnsuringDelayed;
            AssemblyReloadEvents.afterAssemblyReload += DoTheEnsuringDelayed;
        }

        private static int _framesToWait = 10;

        private static void DoTheEnsuringDelayed()
        {
            EditorApplication.delayCall += () =>
            {
                _framesToWait--;
                if (_framesToWait <= 0)
                {
                    DoTheEnsuring();
                }
                else
                {
                    EditorApplication.delayCall += DoTheEnsuringDelayed;
                }
            };
        }

        private static void DoTheEnsuring()
        {
            Debug.Log($"Doing default editor asset maintenance.");
            EnsureHyphlowEditorResourcesAsset();
            EnsureFcWindowConfig();
        }

        private static HyphlowEditorSysAssets EnsureHyphlowEditorResourcesAsset()
        {
            HyphlowEditorSysAssets assets = HyphlowEditorSysAssets.S;
            
            if (assets != null)
            {
                return assets;
            }
            Debug.DebugBreak();
            assets = TryGetHyphlowEditorResourcesAssetFromPackages();//

            if (assets != null)
            {
                return assets;
            }

            HyphlowEditorSysAssets[] all = Resources.LoadAll<HyphlowEditorSysAssets>("");
            if (all.Length > 0)
            {
                return all[0];
            }

            string logMessage = $"Could not find {nameof(HyphlowEditorSysAssets)} in Packages " +
                $"or Resources. Creating a new one at path: " +
                $"{_pathToEditorResourceFolder}/{nameof(HyphlowEditorSysAssets)}.asset\n" + 
                "If you see this message outside of a dev build, please file a bug report.";
            Debug.Log(logMessage);
            assets = SOUtils.EnsureSOExists<HyphlowEditorSysAssets>(_pathToEditorResourceFolder,
                "HyphlowEditorSysAssets");
            return assets;
        }

        private static HyphlowEditorSysAssets TryGetHyphlowEditorResourcesAssetFromPackages()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(HyphlowEditorSysAssets)}");
            string logMessage = $"Guid count: {guids.Length}. Guids: \n{string.Join(", ", guids)}";
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                bool isInPackages = path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
                bool isHyphlowPackagePath = path.IndexOf("hyphlow", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isInPackages || !isHyphlowPackagePath)
                {
                    continue;
                }

                HyphlowEditorSysAssets assets = AssetDatabase.LoadAssetAtPath<HyphlowEditorSysAssets>(path);
                if (assets != null)
                {
                    Debug.Log($"Found {nameof(HyphlowEditorSysAssets)} in packages at path: {path}");
                    return assets;
                }
            }

            logMessage = $"Could not find {nameof(HyphlowEditorSysAssets)} in packages. If you see " +
                $"this message outside of a dev build, please file a bug report.";
            Debug.LogError(logMessage);
            return null;
        }

        private static readonly string _pathToEditorResourceFolder = "Editor"; // Relative to Resources

        private static FlowchartWindowConfig EnsureFcWindowConfig()
        {
            FlowchartWindowConfig config = HyphlowEditorSysAssets.FcwConfig;
            if (config == null)
            {
                var all = Resources.LoadAll<FlowchartWindowConfig>("");
                if (all.Length > 0)
                {
                    config = all[0];
                }
            }

            if (config == null)
            {
                config = SOUtils.EnsureSOExists<FlowchartWindowConfig>(_pathToEditorResourceFolder,
                    "FlowchartWindowConfig");
            }

            return config;
        }
    }
}