using UnityEditor;
using Debug = UnityEngine.Debug;

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
        }

        public static FlowchartGlobalDefaults EnsureFlowchartGlobalDefaults()
        {
            DefaultFlowchartConfigMaintenance.EnsureFcGlobalDefaults();
            return FlowchartGlobalDefaults.S;
        }

        private static readonly string _pathToRuntimeResourceFolder = "AtMycelia/Runtime"; // Relative to Resources
        // Relative to Resources under Assets/

    }
}