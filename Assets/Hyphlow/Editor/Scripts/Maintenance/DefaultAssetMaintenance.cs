using AtMycelia.Hyphlow.Sys;
using UnityEditor;
using UnityEngine;
using AtMycelia.AmaniTween;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;
using System.Linq;

namespace AtMycelia.Hyphlow.EditorUtils
{
    /// <summary>
    /// For ensuring that certain default assets are present in the project.
    /// </summary>
    public static class DefaultAssetMaintenance 
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        [InitializeOnLoadMethod]
        public static void Init()
        {
            AssemblyReloadEvents.afterAssemblyReload -= DoTheEnsuring;
            AssemblyReloadEvents.afterAssemblyReload += DoTheEnsuring;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitInEditor()
        {
            // This is to help make sure that the Singletons aren't lost for too long.
#if UNITY_EDITOR
            DoTheEnsuring();
#endif
        }

        private static void DoTheEnsuring()
        {
            Debug.Log($"Doing default asset maintenance...");
            EnsureHyphlowRuntimeSysAssets();//
            EnsureDefaultTweenAdapter();
            EnsureVariableRegistryConfigs();
        }

        public static HyphlowRuntimeSysAssets EnsureHyphlowRuntimeSysAssets()
        {
            HyphlowRuntimeSysAssets assets = HyphlowRuntimeSysAssets.S;

            if (assets == null)
            {
                var all = Resources.LoadAll<HyphlowRuntimeSysAssets>("");
                if (all.Length > 0)
                {
                    assets = all[0];
                }
                else
                {
                    Debug.LogWarning($"Couldn't find an instance of {nameof(HyphlowRuntimeSysAssets)} " +
                        $"in the Resources folder. Will create one.");
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
        private static readonly string _pathToAtMyceliaResourceFolder = "AtMycelia"; // Relative to Resources under Assets/
        public static DefaultTweenAdapter EnsureDefaultTweenAdapter()
        {
            DefaultTweenAdapter adaptor = HyphlowRuntimeSysAssets.S.TweenAdapter;
            if (adaptor == null)
            {
                adaptor = SOUtils.EnsureSOExists<DefaultTweenAdapter>(_pathToRuntimeResourceFolder,
                    "DefaultTweenAdapter");
            }

            HyphlowRuntimeSysAssets.S.TweenAdapter = adaptor;
            return adaptor;
        }

        public static IReadOnlyList<VariableRegistryConfig> EnsureVariableRegistryConfigs()
        {
            var sysAssets = HyphlowRuntimeSysAssets.S;
            var configsFound = Resources.LoadAll<VariableRegistryConfig>("");
            if (configsFound.Length > 0)
            {
                sysAssets.AddMultiVrcs(configsFound);
            }
            else
            {
                Debug.LogWarning($"Couldn't find any instances of {nameof(VariableRegistryConfig)} " +
                    $"in the Resources folder. Will create a default one.");
                var defaultConfig = SOUtils.EnsureSOExists<VariableRegistryConfig>(_pathToAtMyceliaResourceFolder,
                "VariableRegistryConfig");
                sysAssets.AddVrc(defaultConfig);
            }
                
            return sysAssets.VariableRegistryConfigs;
        }
    }
}