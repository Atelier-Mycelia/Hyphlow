using UnityEditor;
using Debug = UnityEngine.Debug;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// For ensuring that certain default assets are present in the project.
    /// </summary>
    public static class DefaultAssetMaintenance
    {
        /// <summary>
        /// Called by its module in HyphlowMaintenanceModules.cs via the Common lib's MaintenanceRunner; don't call it from lifecycle hooks.
        /// </summary>
        public static void Run()
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