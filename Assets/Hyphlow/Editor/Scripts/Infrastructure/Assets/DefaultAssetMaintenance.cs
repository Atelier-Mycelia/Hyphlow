using AtMycelia.Hyphlow.Sys;
using UnityEditor;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using AtMycelia.HyphaTween;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// For ensuring that certain default assets are present in the project.
    /// </summary>
    public static class DefaultAssetMaintenance
    {
        public static void InitializeAfterAssembliesLoaded()
        {
            AssemblyReloadEvents.afterAssemblyReload -= DoTheEnsuringDelayed;
            AssemblyReloadEvents.afterAssemblyReload += DoTheEnsuringDelayed;
        }

        public static void InitializeBeforeSceneLoad()
        {
            // This is to help make sure that the Singletons aren't lost for too long.
            DoTheEnsuring();
        }

        private static void DoTheEnsuringDelayed()
        {
            EditorApplication.delayCall += DoTheEnsuring;
        }

        private static void DoTheEnsuring()
        {
            Debug.Log($"Doing default asset maintenance...");
            EnsureFlowchartGlobalDefaults();
            EnsureHyphlowRuntimeSysAssets();//
            EnsureVariableRegistryConfigs();

        }

        public static HyphlowRuntimeSysAssets EnsureHyphlowRuntimeSysAssets()
        {
            HyphlowRuntimeSysAssets assets = HyphlowRuntimeSysAssets.S;

            if (assets == null)
            {
                string[] guidsRaw = AssetDatabase.FindAssets($"t:{nameof(HyphlowRuntimeSysAssets)}");
                List<string> guids = new List<string>(guidsRaw);
                // Sort the guids by alphabetical order so we can be sure to check the ones in
                // the Packages folder first.
                guids.Sort();
                string logMessage;
                if (guids.Count > 0)
                {
                    if (guids.Count > 1)
                    {
                        logMessage = $"Multiple {nameof(HyphlowRuntimeSysAssets)} assets found. " +
                            $"Using the last.";
                        Debug.LogWarning(logMessage);
                    }

                    string path = AssetDatabase.GUIDToAssetPath(guids[guids.Count - 1]);
                    assets = AssetDatabase.LoadAssetAtPath<HyphlowRuntimeSysAssets>(path);
                }
                else
                {
                    logMessage = $"Couldn't find any instances of {nameof(HyphlowRuntimeSysAssets)} " +
                        $"in the Assets folder. Will create one. If you see this message outside of a " +
                        $"Dev build of Hyphlow, please file a bug report.";
                    Debug.LogWarning(logMessage);
                }
            }

            if (assets == null)
            {
                assets = SOUtils.EnsureSOExists<HyphlowRuntimeSysAssets>(_pathToAtMyceliaResourceFolder,
                    "HyphlowRuntimeSysAssets");
            }

            HyphlowRuntimeSysAssets.S = assets;
            return assets;
        }

        private static readonly string _pathToRuntimeResourceFolder = "Runtime"; // Relative to Resources
        private static readonly string _pathToAtMyceliaResourceFolder = "AtMycelia";
        // Relative to Resources under Assets/

        public static FlowchartGlobalDefaults EnsureFlowchartGlobalDefaults()
        {
            DefaultFlowchartConfigMaintenance.EnsureFcGlobalDefaults();
            return FlowchartGlobalDefaults.S;
        }

        public static IReadOnlyList<VariableRegistryConfig> EnsureVariableRegistryConfigs()
        {
            var sysAssets = HyphlowRuntimeSysAssets.S;

            // VarRegistryConfigs should be under the Assets folder isntead of the Packages folder, so
            // we don't need to make sure we're working with one under Packag4es.
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(VariableRegistryConfig)}");
            if (guids.Length > 0)
            {
                List<VariableRegistryConfig> configsFound = new List<VariableRegistryConfig>(guids.Length);
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    VariableRegistryConfig config = AssetDatabase.LoadAssetAtPath<VariableRegistryConfig>(path);
                    if (config != null)
                    {
                        configsFound.Add(config);
                    }
                }

                sysAssets.AddMultiVrcs(configsFound);
            }
            else
            {
                Debug.LogWarning($"Couldn't find any instances of {nameof(VariableRegistryConfig)} " +
                    $"in the Assets folder. Will create a default one.");
                var defaultConfig = SOUtils.EnsureSOExists<VariableRegistryConfig>(_pathToAtMyceliaResourceFolder,
                    "VariableRegistryConfig");
                sysAssets.AddVrc(defaultConfig);
            }

            return sysAssets.VariableRegistryConfigs;
        }
    }
}