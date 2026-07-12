using AtMycelia.Hyphlow.Sys;
using UnityEditor;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

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

        public static FlowchartGlobalDefaults EnsureFlowchartGlobalDefaults()
        {
            DefaultFlowchartConfigMaintenance.EnsureFcGlobalDefaults();
            return FlowchartGlobalDefaults.S;
        }

        public static HyphlowRuntimeSysAssets EnsureHyphlowRuntimeSysAssets()
        {
            HyphlowRuntimeSysAssets assets = HyphlowRuntimeSysAssets.S;
            string logMessage;
            if (assets == null)
            {
                string[] guidsRaw = AssetDatabase.FindAssets($"t:{nameof(HyphlowRuntimeSysAssets)}");
                List<string> guids = new List<string>(guidsRaw);
                // Sort the guids by alphabetical order so we can be sure to check the ones in
                // the Packages folder first.
                guids.Sort();
                
                if (guids.Count > 0)
                {
                    if (guids.Count > 1)
                    {
                        logMessage = $"Multiple {nameof(HyphlowRuntimeSysAssets)} assets found. " +
                            $"Using the last.";
                        Debug.LogWarning(logMessage);
                    }

                    string path = AssetDatabase.GUIDToAssetPath(guids[guids.Count - 1]);
                    Debug.Log($"Found {nameof(HyphlowRuntimeSysAssets)} at path: {path}");
                    assets = AssetDatabase.LoadAssetAtPath<HyphlowRuntimeSysAssets>(path);
                }
            }

            // Be it in a dev build or on users' actual projects, we expect to have the runtime sys
            // assets in the Resouurces folder. Hence why if we don't find it there,
            // we create a new one in the expected location.
            if (assets == null)
            {
                logMessage = $"Creating a new instance of {nameof(HyphlowRuntimeSysAssets)} in the " +
                    $"{_pathToRuntimeResourceFolder} folder.";
                Debug.Log(logMessage);
                assets = SOUtils.EnsureSOExists<HyphlowRuntimeSysAssets>(_pathToRuntimeResourceFolder,
                    "HyphlowRuntimeSysAssets");
            }

            HyphlowRuntimeSysAssets.S = assets;
            return assets;
        }

        private static readonly string _pathToRuntimeResourceFolder = "AtMycelia/Runtime"; // Relative to Resources
        // Relative to Resources under Assets/

        public static IReadOnlyList<VariableRegistryConfig> EnsureVariableRegistryConfigs()
        {
            var sysAssets = HyphlowRuntimeSysAssets.S;
            string logMessage;
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
                logMessage = $"(If you just installed Hyphlow, please ignore this warning.)\n" +
                    $"No instances of {nameof(VariableRegistryConfig)} found in the " +
                    $"Assets folder. Creating a default one at {_pathToRuntimeResourceFolder}.";
                Debug.LogWarning(logMessage);
                var defaultConfig = SOUtils.EnsureSOExists<VariableRegistryConfig>(_pathToRuntimeResourceFolder,
                    "VariableRegistryConfig");
                sysAssets.AddVrc(defaultConfig);
            }

            return sysAssets.VariableRegistryConfigs;
        }
    }
}