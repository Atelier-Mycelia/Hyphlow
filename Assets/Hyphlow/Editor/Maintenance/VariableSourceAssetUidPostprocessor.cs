using UnityEditor;

namespace AtMycelia.Hyphlow.EditorExt
{
    public sealed class VariableSourceAssetUidPostprocessor : AssetPostprocessor
    {
        private static bool _isProcessing;

        private static void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (_isProcessing || importedAssets == null || importedAssets.Length == 0)
            {
                return;
            }

            _isProcessing = true;

            try
            {
                for (int i = 0; i < importedAssets.Length; i++)
                {
                    string importedPath = importedAssets[i];
                    VariableSourceAsset importedSource = AssetDatabase.LoadAssetAtPath<VariableSourceAsset>
                        (importedPath);
                    if (importedSource == null || string.IsNullOrEmpty(importedSource.UniqueId))
                    {
                        continue;
                    }

                    if (!HasDuplicateUid(importedSource, importedPath))
                    {
                        continue;
                    }

                    importedSource.ForceResetUid();
                    EditorUtility.SetDirty(importedSource);
                    AssetDatabase.SaveAssetIfDirty(importedSource);
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private static bool HasDuplicateUid(VariableSourceAsset sourceToCheck, string sourcePath)
        {
            string[] guidList = AssetDatabase.FindAssets($"t:{nameof(VariableSourceAsset)}");
            for (int i = 0; i < guidList.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guidList[i]);
                if (path == sourcePath)
                {
                    continue;
                }

                VariableSourceAsset other = AssetDatabase.LoadAssetAtPath<VariableSourceAsset>(path);
                if (other == null)
                {
                    continue;
                }

                if (other.UniqueId == sourceToCheck.UniqueId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}