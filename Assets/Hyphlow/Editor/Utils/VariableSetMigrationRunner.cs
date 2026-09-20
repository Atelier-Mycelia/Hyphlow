#if UNITY_EDITOR
using UnityEditor;

namespace AtMycelia.Hyphlow.EditorExt
{
    [InitializeOnLoad]
    public static class VariableSetMigrationRunner
    {
        static VariableSetMigrationRunner()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private static void OnAfterAssemblyReload()
        {
            EditorApplication.delayCall += MigrateAllVariableSourceAssets;
        }

        private static void MigrateAllVariableSourceAssets()
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