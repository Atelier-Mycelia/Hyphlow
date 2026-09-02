using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Commits working-state QoL assets to disk.
    /// Handles creation, deletion, renaming, and property updates.
    /// </summary>
    public sealed class FlowchartEditorQolSaver : ControlPanelEntrySaver,
        IAtMyceliaControlPanelEntrySaver
    {
        private const string AssetFolder = "Assets/Resources/AtMycelia/Hyphlow";

        public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
        {
            return toSaveFor is FlowchartEditorQolEntry;
        }

        protected override IEnumerator SaveProcess(IControlPanelEntry toSaveFor,
            Action onComplete)
        {
            yield return null;

            FlowchartEditorQolEntry entry = toSaveFor as FlowchartEditorQolEntry;
            IReadOnlyList<FlowchartEditorQol> working = entry.WorkingState;

            // Ensure folder exists
            if (!Directory.Exists(AssetFolder))
            {
                Directory.CreateDirectory(AssetFolder);
            }

            var realAssets = new Dictionary<string, FlowchartEditorQol>(entry.RealAssets);
            // ^Since those are what we want to mutate

            // Track which real assets are still needed
            HashSet<string> namesInWorkingState = new HashSet<string>();

            // Sync working-state → real assets
            foreach (var wState in working)
            {
                namesInWorkingState.Add(wState.name);

                bool nameAlreadyAmongRealAssets = realAssets.TryGetValue(wState.name, out var real);
                if (!nameAlreadyAmongRealAssets)
                {
                    #region Create and then register new asset
                    real = ScriptableObject.CreateInstance<FlowchartEditorQol>();
                    string path = $"{AssetFolder}/{wState.name}.asset";
                    AssetDatabase.CreateAsset(real, path);
                    realAssets[wState.name] = real;
                    #endregion
                }

                ApplyWorkingStateToReal(wState, real);
            }

            #region DELETE assets missing from working state
            foreach (var kvp in realAssets)
            {
                string name = kvp.Key;
                FlowchartEditorQol real = kvp.Value;

                if (!namesInWorkingState.Contains(name))
                {
                    string path = AssetDatabase.GetAssetPath(real);
                    AssetDatabase.DeleteAsset(path);
                }
            }
            #endregion

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            onComplete?.Invoke();
        }

        /// <summary>
        /// Keyed by name.
        /// </summary>
        private IDictionary<string, FlowchartEditorQol> LoadRealAssets()
        {
            var result = new Dictionary<string, FlowchartEditorQol>();

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FlowchartEditorQol)}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                FlowchartEditorQol real = AssetDatabase.LoadAssetAtPath<FlowchartEditorQol>(path);

                if (real != null)
                {
                    result[real.name] = real;
                }
            }

            return result;
        }

        private void ApplyWorkingStateToReal(FlowchartEditorQol wState, FlowchartEditorQol real)
        {
            RenameAsNeeded(wState, real);
            real.ClearCommandsToHide();
            real.AddMultiCommandsToHide(wState.CommandsToHide as IList<string>);
            EditorUtility.SetDirty(real);
        }

        private void RenameAsNeeded(FlowchartEditorQol wState, FlowchartEditorQol real)
        {
            bool shouldRename = real.name != wState.name;
            if (shouldRename)
            {
                string path = AssetDatabase.GetAssetPath(real);
                string errorMessage = AssetDatabase.RenameAsset(path, wState.name);

                bool renameAttemptFailed = !string.IsNullOrEmpty(errorMessage);
                if (renameAttemptFailed)
                {
                    string logMessage = $"Failed to rename QoL asset '{real.name}' → " +
                        $"'{wState.name}': {errorMessage}";
                    Debug.LogError(logMessage);
                }
            }
        }
    }
}
