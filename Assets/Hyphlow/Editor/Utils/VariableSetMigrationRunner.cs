#if UNITY_EDITOR
using UnityEditor;

namespace AtMycelia.Hyphlow.EditorExt
{
    public static class VariableSetMigrationRunner
    {
        /// <summary>
        /// Called by its module in HyphlowMaintenanceModules.cs via the Common lib's MaintenanceRunner; don't call it from lifecycle hooks.
        /// </summary>
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:VariableSourceAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                VariableSet asset = AssetDatabase.LoadAssetAtPath<VariableSet>(path);
                if (asset != null)
                {
                }
            }
        }
    }
}
#endif