using AtMycelia.Hyphlow.Sys;
using UnityEditor;

namespace AtMycelia.Hyphlow.EditorUtils
{
    public sealed class VariableRegistryConfigPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets == null || importedAssets.Length == 0)
            {
                return;
            }

            HyphlowRuntimeSysAssets sysAssets = DefaultAssetMaintenance.EnsureHyphlowRuntimeSysAssets();
            if (sysAssets == null)
            {
                return;
            }

            for (int i = 0; i < importedAssets.Length; i++)
            {
                string assetPath = importedAssets[i];
                VariableRegistryConfig config = AssetDatabase.LoadAssetAtPath<VariableRegistryConfig>(assetPath);
                if (config == null)
                {
                    continue;
                }

                if (IsConfigRegistered(sysAssets, config))
                {
                    continue;
                }

                sysAssets.AddVrc(config);
                EditorUtility.SetDirty(sysAssets);
            }

            AssetDatabase.SaveAssetIfDirty(sysAssets);
        }

        private static bool IsConfigRegistered(HyphlowRuntimeSysAssets sysAssets, VariableRegistryConfig config)
        {
            var configs = sysAssets.VariableRegistryConfigs;
            if (configs == null)
            {
                return false;
            }

            for (int i = 0; i < configs.Count; i++)
            {
                if (configs[i] == config)
                {
                    return true;
                }
            }

            return false;
        }
    }
}