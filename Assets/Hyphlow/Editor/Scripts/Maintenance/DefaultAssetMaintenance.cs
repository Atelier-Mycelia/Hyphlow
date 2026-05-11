using AtMycelia.Hyphlow.Sys;
using UnityEditor;
using UnityEngine;
using AtMycelia.AmaniTween;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

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
                string[] guids = AssetDatabase.FindAssets($"t:{nameof(HyphlowRuntimeSysAssets)}");
                if (guids.Length > 0)
                {
                    if (guids.Length > 1)
                    {
                        Debug.LogWarning($"Multiple {nameof(HyphlowRuntimeSysAssets)} assets found. Using the first.");
                    }

                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    assets = AssetDatabase.LoadAssetAtPath<HyphlowRuntimeSysAssets>(path);
                }
                else
                {
                    Debug.LogWarning($"Couldn't find an instance of {nameof(HyphlowRuntimeSysAssets)} " +
                        $"in the Assets folder. Will create one.");
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
                string[] guids = AssetDatabase.FindAssets($"t:{nameof(DefaultTweenAdapter)}");
                if (guids.Length > 0)
                {
                    if (guids.Length > 1)
                    {
                        Debug.LogWarning($"Multiple {nameof(DefaultTweenAdapter)} assets found. Using the first.");
                    }

                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    adaptor = AssetDatabase.LoadAssetAtPath<DefaultTweenAdapter>(path);
                }

                if (adaptor == null)
                {
                    adaptor = SOUtils.EnsureSOExists<DefaultTweenAdapter>(_pathToRuntimeResourceFolder,
                        "DefaultTweenAdapter");
                }
            }

            HyphlowRuntimeSysAssets.S.TweenAdapter = adaptor;
            return adaptor;
        }

        public static IReadOnlyList<VariableRegistryConfig> EnsureVariableRegistryConfigs()
        {
            var sysAssets = HyphlowRuntimeSysAssets.S;

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