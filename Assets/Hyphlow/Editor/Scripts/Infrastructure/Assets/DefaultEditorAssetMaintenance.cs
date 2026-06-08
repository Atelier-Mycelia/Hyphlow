using System;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// For ensuring that certain editor-only assets are created and maintained, such as the HyphlowEditorSysAssets asset.
    /// </summary>
    public static class DefaultEditorAssetMaintenance 
    {
        public static void InitializeAfterAssembliesLoaded()
        {
            AssemblyReloadEvents.afterAssemblyReload -= DoTheEnsuring;
            AssemblyReloadEvents.afterAssemblyReload += DoTheEnsuring;
        }

        private static void DoTheEnsuring()
        {
            Debug.Log($"Doing default editor asset maintenance...");
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

            assets = TryGetHyphlowEditorResourcesAssetFromPackages();

            if (assets != null)
            {
                return assets;
            }

            HyphlowEditorSysAssets[] all = Resources.LoadAll<HyphlowEditorSysAssets>("");
            if (all.Length > 0)
            {
                return all[0];
            }

            assets = SOUtils.EnsureSOExists<HyphlowEditorSysAssets>(_pathToEditorResourceFolder,
                "HyphlowEditorSysAssets");
            return assets;
        }

        private static HyphlowEditorSysAssets TryGetHyphlowEditorResourcesAssetFromPackages()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(HyphlowEditorSysAssets)}");
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
                    return assets;
                }
            }

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