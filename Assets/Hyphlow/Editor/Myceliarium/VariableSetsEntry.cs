using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for creating and organizing Variable Sets on disk.
    /// Everything done here is staged on <see cref="VariableSetRecord"/>s (edits go to
    /// detached copies of the sets), which <see cref="VariableSetsLogic"/> owns, and only
    /// reaches the project when the user saves, through <see cref="VariableSetsSaver"/>.
    /// </summary>
    public sealed class VariableSetsEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Variable Sets";

        // The panel can be closed with sets staged, and the sets on disk can change
        // meanwhile, so what's shown has to be there from the start rather than from the
        // first click on the tab.
        public override bool ShouldPreloadOnInit => true;

        protected override Type SidebarTabType => typeof(VariableSetsTab);
        protected override Type SubwindowType => typeof(VariableSetsSubwindow);
        protected override Type LoaderType => typeof(VariableSetsLoader);
        protected override Type LogicType => typeof(VariableSetsLogic);

        private VariableSetsSubwindow SetsSubwindow => _subwindow as VariableSetsSubwindow;
        private VariableSetsLogic SetsLogic => Logic as VariableSetsLogic;

        internal IReadOnlyList<VariableSetRecord> Records => SetsLogic.Records;

        internal int PendingDeleteCount => SetsLogic.PendingDeleteCount;

        // Swappable so tests don't block on modal dialogs
        internal Action<string, string> ErrorDialog { get; set; } =
            (title, message) => EditorUtility.DisplayDialog(title, message, "OK");

        /// <summary>
        /// Takes the title and message, and returns which button was picked (the same as
        /// DisplayDialogComplex: 0 for the first, 1 for the second, 2 for the third).
        /// </summary>
        internal Func<string, string, int> UnsavedChangesBeforePlayDialog { get; set; } =
            (title, message) => EditorUtility.DisplayDialogComplex(title, message,
                "Save and Play", "Cancel", "Discard and Play");

        protected override void PrepareLogic()
        {
            base.PrepareLogic();

            // The subwindow was made before the logic, so it's only now there's a list to show
            SetsSubwindow?.UseRecords(SetsLogic.Records);
        }

        #region Loading
        /// <summary>
        /// Throws away whatever's staged and starts over from what's on disk, for when the
        /// tab isn't being selected (which has the base class do the same).
        /// </summary>
        private void ReloadRecords()
        {
            Loader.Load(this, OnLoadingDone);
        }

        protected override void OnLoadingDone()
        {
            // Not the base's, which shows the subwindow even if this isn't the selected
            // entry (loading also happens on init, the panel opening and project changes).
            AdoptLoadedRecords();
            if (_tab != null && _tab.IsSelected)
            {
                _subwindow?.Show();
            }
        }

        private void AdoptLoadedRecords()
        {
            VariableSetsSubwindow subwindow = SetsSubwindow;
            VariableSet selectedAsset = subwindow?.SelectedRecord?.Real;
            // The inspector is showing the old records' detached copies, which loading disposes
            subwindow?.ReleaseInspector();

            Logic.Load(Loader.GetLoadResult<object>());

            if (selectedAsset != null)
            {
                // The records are all new objects now, so selection goes by asset
                subwindow.SelectKey(FindRecordFor(selectedAsset)?.Key);
            }

            subwindow?.Refresh();
        }

        private VariableSetRecord FindRecordFor(VariableSet set)
        {
            IReadOnlyList<VariableSetRecord> records = SetsLogic.Records;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].Real == set)
                {
                    return records[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Entries outlive the window, so reopening it doesn't initialize or select this
        /// again, and project changes made while it was closed went unnoticed (the
        /// handler below only reacts while the subwindow is on screen). What's shown may
        /// be out of date by now. Staged changes are what the user left unfinished, so
        /// they're kept.
        /// </summary>
        protected override void OnControlPanelOpened(IControlPanel panel)
        {
            base.OnControlPanelOpened(panel);

            if (!HasUnsavedChanges)
            {
                ReloadRecords();
            }
        }

        /// <summary>
        /// A set's state was applied from somewhere else, which is what restoring the sets
        /// after Play Mode does. A working copy made before that (from the values Play Mode
        /// left) is out of date, and what the inspector shows comes from it. Copies that
        /// have staged edits are left alone, since those are the user's to keep or discard.
        /// </summary>
        private void OnSetStateApplied(VariableSet set)
        {
            if (SetsLogic.IsCommitting)
            {
                return; // Our own saving; the copies are already in step
            }

            VariableSetRecord record = FindRecordFor(set);
            bool isStale = record != null && record.HasWorking && !record.StateChanged;
            if (!isStale)
            {
                return;
            }

            SetsSubwindow?.ReleaseInspector();
            record.ResetWorking();
            SetsSubwindow?.Refresh();
        }

        /// <summary>
        /// Keeps the list in step with changes made outside the panel (Project window,
        /// version control, etc.), as long as that wouldn't cost the user their staged changes.
        /// </summary>
        private void OnProjectChanged()
        {
            VariableSetsSubwindow subwindow = SetsSubwindow;
            bool isOnScreen = subwindow != null && subwindow.IsVisible && subwindow.panel != null;
            if (!isOnScreen || HasUnsavedChanges || DiskMatchesRecords())
            {
                return;
            }

            ReloadRecords();
        }

        /// <summary>
        /// Project changes are frequent and mostly unrelated to us, and reloading rebuilds
        /// the inspector (losing the user's focus and scroll position in it), so this is
        /// checked first.
        /// </summary>
        private bool DiskMatchesRecords()
        {
            IReadOnlyList<VariableSetRecord> records = SetsLogic.Records;
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(VariableSet)}");
            if (guids.Length != records.Count)
            {
                return false;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                VariableSetRecord record = FindRecordWithKey(guids[i]);
                bool unchanged = record != null &&
                    record.Path == AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!unchanged)
                {
                    return false;
                }
            }

            return true;
        }

        private VariableSetRecord FindRecordWithKey(string key)
        {
            IReadOnlyList<VariableSetRecord> records = SetsLogic.Records;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].Key == key)
                {
                    return records[i];
                }
            }

            return null;
        }
        #endregion

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            if (wantsSubsActive)
            {
                EditorApplication.projectChanged += OnProjectChanged;
                VariableSetPlayModeSnapshots.BeforeCapture += OnBeforePlayModeSnapshot;
                VarSetSignals.StateApplied += OnSetStateApplied;
            }
            else
            {
                EditorApplication.projectChanged -= OnProjectChanged;
                VariableSetPlayModeSnapshots.BeforeCapture -= OnBeforePlayModeSnapshot;
                VarSetSignals.StateApplied -= OnSetStateApplied;
            }

            var subwindow = SetsSubwindow;
            var logic = SetsLogic;
            if (subwindow == null || logic == null)
            {
                return;
            }

            if (wantsSubsActive)
            {
                subwindow.CreateRequested += OnCreateRequested;
                subwindow.RenameRequested += OnRenameRequested;
                subwindow.DuplicateRequested += OnDuplicateRequested;
                subwindow.DeleteRequested += OnDeleteRequested;
                subwindow.KeepRequested += OnKeepRequested;
                subwindow.MoveToggled += OnMoveToggled;
                subwindow.MoveAllRequested += OnMoveAllRequested;
                subwindow.StateMayHaveChanged += logic.NotifyIfDirtyChanged;
            }
            else
            {
                subwindow.CreateRequested -= OnCreateRequested;
                subwindow.RenameRequested -= OnRenameRequested;
                subwindow.DuplicateRequested -= OnDuplicateRequested;
                subwindow.DeleteRequested -= OnDeleteRequested;
                subwindow.KeepRequested -= OnKeepRequested;
                subwindow.MoveToggled -= OnMoveToggled;
                subwindow.MoveAllRequested -= OnMoveAllRequested;
                subwindow.StateMayHaveChanged -= logic.NotifyIfDirtyChanged;
            }
        }

        #region Staging
        // Play Mode makes the panel read-only, but these guard against it anyway, since
        // changes to Variable Sets made then get reverted. Internal rather than private
        // so tests can drive them without going through the subwindow's buttons.

        internal void OnCreateRequested(string name)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            string folder = VariableSetAssetOps.VsaFolder;
            string error = VariableSetAssetOps.ValidateName(name, Records, null, folder);
            if (error != null)
            {
                ShowError("Couldn't Create Variable Set", error);
                return;
            }

            Add(VariableSetRecord.CreateNew(name, folder));
        }

        private void Add(VariableSetRecord record)
        {
            SetsLogic.Add(record);
            SetsSubwindow?.Refresh(record.Key);
        }

        private void ShowError(string title, string message)
        {
            ErrorDialog(title, message);
        }

        internal void OnRenameRequested(VariableSetRecord record, string newName)
        {
            if (EditorApplication.isPlaying || record.IsReadOnly)
            {
                return;
            }

            string error = VariableSetAssetOps.ValidateName(newName, Records, record, record.Folder);
            if (error != null)
            {
                ShowError("Couldn't Rename Variable Set", error);
            }
            else
            {
                record.Name = newName;
            }

            SetsSubwindow?.Refresh(record.Key);
            SetsLogic.NotifyIfDirtyChanged();
        }

        internal void OnDuplicateRequested(VariableSetRecord record)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            // Next to the original when it's in the VSA folder (or a subfolder of it).
            // Otherwise, in the VSA folder, same as any other new set.
            bool keepNextToOriginal = record.IsNew ||
                record.Location == VariableSetLocation.VsaFolder ||
                record.Location == VariableSetLocation.VsaSubfolder;
            string folder = keepNextToOriginal ? record.Folder : VariableSetAssetOps.VsaFolder;
            string name = VariableSetAssetOps.SuggestName(record.Name, Records, folder);

            Add(VariableSetRecord.CreateCopyOf(record, name, folder));
        }

        internal void OnDeleteRequested(VariableSetRecord record)
        {
            if (EditorApplication.isPlaying || record.IsReadOnly)
            {
                return;
            }

            if (record.IsNew)
            {
                // Nothing on disk to take care of at save time, so there's nothing to stage
                SetsSubwindow?.ReleaseInspector();
                SetsLogic.Remove(record);
            }
            else
            {
                record.PendingDelete = true;
            }

            SetsSubwindow?.Refresh();
            SetsLogic.NotifyIfDirtyChanged();
        }

        internal void OnKeepRequested(VariableSetRecord record)
        {
            string error = VariableSetAssetOps.ValidateName(record.Name, Records, record);
            if (error != null)
            {
                // Something else took its name while it was staged for deletion
                ShowError("Couldn't Keep Variable Set",
                    error + "\n\nRename one of them, then try again.");
                return;
            }

            record.PendingDelete = false;
            SetsSubwindow?.Refresh(record.Key);
            SetsLogic.NotifyIfDirtyChanged();
        }

        internal void OnMoveToggled(VariableSetRecord record)
        {
            bool canMove = !EditorApplication.isPlaying && !record.IsNew && !record.PendingDelete &&
                record.Location == VariableSetLocation.Elsewhere;
            if (!canMove)
            {
                return;
            }

            record.PendingMove = !record.PendingMove;
            SetsSubwindow?.Refresh(record.Key);
            SetsLogic.NotifyIfDirtyChanged();
        }

        internal void OnMoveAllRequested()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            IReadOnlyList<VariableSetRecord> records = SetsLogic.Records;
            for (int i = 0; i < records.Count; i++)
            {
                VariableSetRecord record = records[i];
                if (!record.IsNew && !record.PendingDelete &&
                    record.Location == VariableSetLocation.Elsewhere)
                {
                    record.PendingMove = true;
                }
            }

            SetsSubwindow?.Refresh();
            SetsLogic.NotifyIfDirtyChanged();
        }
        #endregion

        #region Saving
        /// <summary>
        /// Carries out everything that's staged. Returns whether all of it went through;
        /// what didn't stays staged.
        /// </summary>
        internal bool Commit()
        {
            // The inspector is showing the detached copies, and deleted records dispose theirs
            SetsSubwindow?.ReleaseInspector();

            VariableSetCommit.Result result = SetsLogic.Commit();
            if (!result.Succeeded)
            {
                ShowError("Some Variable Set Changes Weren't Saved",
                    result.Summary + "\nThey're still staged in the Control Panel, so you can " +
                    "fix them and save again.");
            }

            SetsSubwindow?.Refresh();
            return result.Succeeded;
        }

        /// <summary>
        /// Entering Play Mode reloads scripts, which throws away everything staged here
        /// (it only exists in memory). Rather than let that happen silently, this asks.
        /// It's run by the snapshot module before it backs the sets up, so that what gets
        /// saved here is part of the backup.
        /// </summary>
        internal void OnBeforePlayModeSnapshot()
        {
            if (!HasUnsavedChanges)
            {
                return;
            }

            int deleteCount = PendingDeleteCount;
            string deleteNote = deleteCount > 0 ?
                $"\n\nSaving will also move {deleteCount} Variable Set(s) to the trash." :
                string.Empty;

            const int save = 0, cancel = 1; // Anything else is Discard, which needs nothing done
            int choice = UnsavedChangesBeforePlayDialog("Unsaved Variable Set Changes",
                "The Control Panel has Variable Set changes that haven't been saved, and " +
                "entering Play Mode would discard them." + deleteNote);

            bool stayInEditMode = choice == cancel || (choice == save && !Commit());
            if (stayInEditMode)
            {
                EditorApplication.isPlaying = false;
            }
        }
        #endregion

        public override void Dispose()
        {
            // It's looking at the records' detached copies, which the logic disposes
            SetsSubwindow?.ReleaseInspector();
            base.Dispose();
        }
    }
}
