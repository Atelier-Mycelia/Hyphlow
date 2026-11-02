using AtMycelia.Myceliarium;
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

        // Each way of not saving everything is reported with ReportFailure rather than by
        // throwing, since the user has been told why by a dialog and a logged exception
        // would only add noise.
        protected override IEnumerator SaveProcess(EntrySaveRequest request)
        {
            var entry = (VariableSetsEntry)request.Entry;
            if (!entry.HasUnsavedChanges)
            {
                yield break;
            }

            if (EditorApplication.isPlaying)
            {
                // Whatever's written to a set now gets reverted when Play Mode ends
                EditorUtility.DisplayDialog("Can't Save in Play Mode",
                    "Variable Sets can't be changed while Play Mode is running. Your changes " +
                    "are still here; exit Play Mode, then save.", "OK");
                request.ReportFailure();
                yield break;
            }

            if (!ConfirmDeletes(entry) || !entry.Commit())
            {
                request.ReportFailure();
            }
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
