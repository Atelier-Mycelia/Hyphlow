using AtMycelia.Hyphlow.Sys;
using UnityEditor;
using UnityEngine;
using AtMycelia.AmaniTween;

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
            EnsureHyphlowRuntimeSysAssets();
            EnsureDefaultTweenAdapter();
            EnsureVariableRegistryConfig();
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
            }

            if (assets == null)
            {
                assets = SOUtils.EnsureSOExists<HyphlowRuntimeSysAssets>(_pathToRuntimeResourceFolder,
                    "HyphlowRuntimeSysAssets");
            }
            HyphlowRuntimeSysAssets.S = assets;
            return assets;
        }

        private static readonly string _pathToRuntimeResourceFolder = "Runtime"; // Relative to Resources

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

        public static VariableRegistryConfig EnsureVariableRegistryConfig()
        {
            VariableRegistryConfig config = HyphlowRuntimeSysAssets.S.VariableRegistryConfig;
            if (config == null)
            {
                config = SOUtils.EnsureSOExists<VariableRegistryConfig>(_pathToRuntimeResourceFolder,
                    "VariableRegistryConfig");
            }

            HyphlowRuntimeSysAssets.S.VariableRegistryConfig = config;
            return config;
        }
    }
}