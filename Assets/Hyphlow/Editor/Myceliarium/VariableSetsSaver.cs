using AtMycelia.Myceliarium;
using System;
using System.Collections;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Commits what's been staged in the Variable Sets entry. The work itself is
    /// VariableSetCommit's; this is the part that's specific to the Save button.
    /// </summary>
    public sealed class VariableSetsSaver : ControlPanelEntrySaver, IAtMyceliaControlPanelEntrySaver
    {
        public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
        {
            return toSaveFor is VariableSetsEntry;
        }

        protected override IEnumerator SaveProcess(IControlPanelEntry toSaveFor, Action onComplete)
        {
            yield return null; // So delayed fields have the time to commit their values

            var entry = (VariableSetsEntry)toSaveFor;
            if (!entry.HasUnsavedChanges)
            {
                onComplete?.Invoke();
                yield break;
            }

            if (EditorApplication.isPlaying)
            {
                // Whatever's written to a set now gets reverted when Play Mode ends
                EditorUtility.DisplayDialog("Can't Save in Play Mode",
                    "Variable Sets can't be changed while Play Mode is running. Your changes " +
                    "are still here; exit Play Mode, then save.", "OK");
                ControlPanelSignals.SaveFailed(entry);
                onComplete?.Invoke();
                yield break;
            }

            if (!ConfirmDeletes(entry))
            {
                onComplete?.Invoke();
                yield break;
            }

            if (entry.Commit())
            {
                ControlPanelSignals.SaveCompleted(entry);
            }
            else
            {
                ControlPanelSignals.SaveFailed(entry);
            }

            onComplete?.Invoke();
        }

        private static bool ConfirmDeletes(VariableSetsEntry entry)
        {
            int deleteCount = entry.PendingDeleteCount;
            if (deleteCount == 0)
            {
                return true;
            }

            string setPhrase = deleteCount == 1 ? "1 Variable Set" : $"{deleteCount} Variable Sets";
            string message = $"Saving moves {setPhrase} to your system's trash, along with the " +
                "variables inside.\n\nFlowcharts and commands that use those variables will lose " +
                "their references. You can still recover them from the trash.";
            return EditorUtility.DisplayDialog("Delete Variable Sets?", message, "Save and Delete",
                "Cancel");
        }
    }
}
