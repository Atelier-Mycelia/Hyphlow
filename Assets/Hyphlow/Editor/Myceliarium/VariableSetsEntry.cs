using AtMycelia.Myceliarium;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for creating and organizing Variable Sets on disk.
    /// Unlike the other Hyphlow entries, this doesn't stage changes for the Save button:
    /// Variable Sets already write straight to disk from their inspector, so asset
    /// operations here do the same, and nothing is ever left unsaved.
    /// </summary>
    public sealed class VariableSetsEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Variable Sets";
        public override bool IsTopLevel => false;

        private void ReloadRecords()
        {
            _records.Clear();
            _records.AddRange(VariableSetAssetOps.FindAll());
        }

        // The subwindow holds onto this same instance, so it gets refilled rather than replaced
        private readonly List<VariableSetRecord> _records = new List<VariableSetRecord>();

        protected override void PrepareLeftSidebarTab()
        {
            _tab = new VariableSetsTab();
            _tab.Init();
        }

        protected override void PrepareSubwindow()
        {
            ReloadRecords();
            _subwindow = new VariableSetsSubwindow(_records);
            _subwindow.Init();
        }

        private VariableSetsSubwindow SetsSubwindow => _subwindow as VariableSetsSubwindow;

        protected override void SetSubs(bool on)
        {
            base.SetSubs(on);

            if (on)
            {
                EditorApplication.projectChanged += OnProjectChanged;
            }
            else
            {
                EditorApplication.projectChanged -= OnProjectChanged;
            }

            var subwindow = SetsSubwindow;
            if (subwindow == null)
            {
                return;
            }

            if (on)
            {
                subwindow.CreateRequested += OnCreateRequested;
                subwindow.RenameRequested += OnRenameRequested;
                subwindow.DuplicateRequested += OnDuplicateRequested;
                subwindow.DeleteRequested += OnDeleteRequested;
                subwindow.MoveRequested += OnMoveRequested;
                subwindow.MoveAllRequested += OnMoveAllRequested;
            }
            else
            {
                subwindow.CreateRequested -= OnCreateRequested;
                subwindow.RenameRequested -= OnRenameRequested;
                subwindow.DuplicateRequested -= OnDuplicateRequested;
                subwindow.DeleteRequested -= OnDeleteRequested;
                subwindow.MoveRequested -= OnMoveRequested;
                subwindow.MoveAllRequested -= OnMoveAllRequested;
            }
        }

        /// <summary>
        /// Keeps the list in step with changes made outside the panel (Project window,
        /// version control, etc.).
        /// </summary>
        private void OnProjectChanged()
        {
            var subwindow = SetsSubwindow;
            bool isOnScreen = subwindow != null && subwindow.IsVisible && subwindow.panel != null;
            if (!isOnScreen)
            {
                return; // Select reloads anyway
            }

            ReloadRecords();
            subwindow.Refresh();
        }

        public override void Select()
        {
            ReloadRecords();
            base.Select();
        }

        private void OnCreateRequested(string name)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            VariableSet created = VariableSetAssetOps.Create(name, _records, out string error);
            if (created == null)
            {
                ShowError("Couldn't Create Variable Set", error);
                return;
            }

            ReloadAndSelect(created);
        }

        private static void ShowError(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private void ReloadAndSelect(VariableSet toSelect)
        {
            ReloadRecords();
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(toSelect));
            SetsSubwindow?.Refresh(guid);
        }

        private void ReloadAndRefresh()
        {
            ReloadRecords();
            SetsSubwindow?.Refresh();
        }

        private void OnRenameRequested(VariableSetRecord record, string newName)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (!VariableSetAssetOps.Rename(record, newName, _records, out string error))
            {
                ShowError("Couldn't Rename Variable Set", error);
            }

            ReloadAndRefresh();
        }

        private void OnDuplicateRequested(VariableSetRecord record)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            VariableSet copy = VariableSetAssetOps.Duplicate(record, _records, out string error);
            if (copy == null)
            {
                ShowError("Couldn't Duplicate Variable Set", error);
                ReloadAndRefresh();
                return;
            }

            ReloadAndSelect(copy);
        }

        private void OnDeleteRequested(VariableSetRecord record)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            int varCount = record.VariableCount;
            string varPhrase = varCount == 1 ? "its 1 variable" : $"its {varCount} variables";
            string message = $"\"{record.Name}\" and {varPhrase} will be moved to your system's " +
                "trash.\n\nFlowcharts and commands that use these variables will lose " +
                "those references.";

            bool confirmed = EditorUtility.DisplayDialog("Delete Variable Set?", message,
                "Delete", "Cancel");
            if (!confirmed)
            {
                return;
            }

            if (!VariableSetAssetOps.Delete(record, out string error))
            {
                ShowError("Couldn't Delete Variable Set", error);
            }

            ReloadAndRefresh();
        }

        private void OnMoveRequested(VariableSetRecord record)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            if (!VariableSetAssetOps.MoveToVsaFolder(record, out string error))
            {
                ShowError("Couldn't Move Variable Set", error);
            }

            ReloadAndRefresh();
        }

        private void OnMoveAllRequested()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            List<VariableSetRecord> toMove = _records.FindAll(IsElsewhere);
            static bool IsElsewhere(VariableSetRecord record)
            {
                return record.Location == VariableSetLocation.Elsewhere;
            }

            if (toMove.Count == 0 || !ConfirmMoveAll(toMove))
            {
                return;
            }

            // Not batched in Start/StopAssetEditing: two same-named sets would both get
            // the same unique path, since the first move wouldn't register until the end.
            var failures = new StringBuilder();
            for (int i = 0; i < toMove.Count; i++)
            {
                if (!VariableSetAssetOps.MoveToVsaFolder(toMove[i], out string error))
                {
                    failures.AppendLine($"• {toMove[i].Path}: {error}");
                }
            }

            if (failures.Length > 0)
            {
                ShowError("Some Variable Sets Weren't Moved", failures.ToString());
            }

            ReloadAndRefresh();
        }

        private static bool ConfirmMoveAll(IReadOnlyList<VariableSetRecord> toMove)
        {
            const int maxListed = 8;
            var message = new StringBuilder();
            string setPhrase = toMove.Count == 1 ? "1 Variable Set" : $"{toMove.Count} Variable Sets";
            message.AppendLine($"Move {setPhrase} into {VariableSetAssetOps.VsaFolder}?");
            message.AppendLine();

            for (int i = 0; i < toMove.Count && i < maxListed; i++)
            {
                message.AppendLine($"• {toMove[i].Path}");
            }

            if (toMove.Count > maxListed)
            {
                message.AppendLine($"…and {toMove.Count - maxListed} more");
            }

            message.AppendLine();
            message.Append("References to them are kept. Sets already in that folder or its " +
                "subfolders aren't affected.");

            return EditorUtility.DisplayDialog("Move Variable Sets?", message.ToString(),
                "Move", "Cancel");
        }
    }
}
