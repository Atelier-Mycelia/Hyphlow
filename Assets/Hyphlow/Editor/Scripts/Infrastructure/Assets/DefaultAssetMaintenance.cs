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
            _framesToWait = 15;
            AssemblyReloadEvents.afterAssemblyReload -= DoTheEnsuringDelayed;
            AssemblyReloadEvents.afterAssemblyReload += DoTheEnsuringDelayed;
        }

        private static float _framesToWait = 15;

        public static void InitializeBeforeSceneLoad()
        {
            // This is to help make sure that the Singletons aren't lost for too long.
            DoTheEnsuring();
        }

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
            Debug.Log($"Doing default asset maintenance.");
            EnsureFlowchartGlobalDefaults();
            EnsureHyphlowRuntimeSysAssets();
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

    }
}