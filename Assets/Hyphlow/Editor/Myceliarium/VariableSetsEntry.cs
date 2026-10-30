using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System.Collections.Generic;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for creating and organizing Variable Sets on disk.
    /// Everything done here is staged on <see cref="VariableSetRecord"/>s (edits go to
    /// detached copies of the sets) and only reaches the project when the user saves,
    /// through <see cref="VariableSetsSaver"/>.
    /// </summary>
    public sealed class VariableSetsEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Variable Sets";
        public override bool IsTopLevel => false;

        public override bool HasUnsavedChanges => AnyRecordIsDirty();

        private bool AnyRecordIsDirty()
        {
            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].IsDirty)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Computed from the records, so the base class's own change notification doesn't
        /// apply; this is what tells the panel when the answer may have changed.
        /// </summary>
        private void NotifyIfDirtyChanged()
        {
            bool dirty = AnyRecordIsDirty();
            if (dirty != _lastReportedDirty)
            {
                _lastReportedDirty = dirty;
                NotifyUnsavedChangesChanged();
            }
        }

        private bool _lastReportedDirty;

        internal int PendingDeleteCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _records.Count; i++)
                {
                    if (_records[i].PendingDelete && !_records[i].IsNew)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        // The subwindow holds onto this same instance, so it gets refilled rather than replaced
        private readonly List<VariableSetRecord> _records = new List<VariableSetRecord>();

        private VariableSetsSubwindow SetsSubwindow => _subwindow as VariableSetsSubwindow;

        internal IReadOnlyList<VariableSetRecord> Records => _records;

        // Swappable so tests don't block on modal dialogs
        internal System.Action<string, string> ErrorDialog { get; set; } =
            (title, message) => EditorUtility.DisplayDialog(title, message, "OK");

        /// <summary>
        /// Takes the title and message, and returns which button was picked (the same as
        /// DisplayDialogComplex: 0 for the first, 1 for the second, 2 for the third).
        /// </summary>
        internal System.Func<string, string, int> UnsavedChangesBeforePlayDialog { get; set; } =
            (title, message) => EditorUtility.DisplayDialogComplex(title, message,
                "Save and Play", "Cancel", "Discard and Play");

        #region Loading
        /// <summary>
        /// Throws away whatever's staged and starts over from what's on disk. The inspector
        /// shown in the subwindow has to let go of the detached copies first.
        /// </summary>
        private void ReloadRecords()
        {
            VariableSetsSubwindow subwindow = SetsSubwindow;
            VariableSet selectedAsset = subwindow?.SelectedRecord?.Real;
            subwindow?.ReleaseInspector();

            DisposeRecords();
            _records.AddRange(VariableSetAssetOps.FindAll());

            if (selectedAsset != null)
            {
                // The records are all new objects now, so selection goes by asset
                subwindow.SelectKey(_records.Find(record => record.Real == selectedAsset)?.Key);
            }

            NotifyIfDirtyChanged();
        }

        private void DisposeRecords()
        {
            for (int i = 0; i < _records.Count; i++)
            {
                _records[i].Dispose();
            }

            _records.Clear();
        }

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

        public override void Select()
        {
            // Reloading would throw out what's staged, which is what coming back to this
            // tab shouldn't do
            if (!HasUnsavedChanges)
            {
                ReloadRecords();
                SetsSubwindow?.Refresh();
            }

            base.Select();
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
                SetsSubwindow?.Refresh();
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
            if (_isCommitting)
            {
                return; // Our own saving; the copies are already in step
            }

            VariableSetRecord record = _records.Find(r => r.Real == set);
            bool isStale = record != null && record.HasWorking && !record.StateChanged;
            if (!isStale)
            {
                return;
            }

            SetsSubwindow?.ReleaseInspector();
            record.ResetWorking();
            SetsSubwindow?.Refresh();
        }

        private bool _isCommitting;

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
            subwindow.Refresh();
        }

        /// <summary>
        /// Project changes are frequent and mostly unrelated to us, and reloading rebuilds
        /// the inspector (losing the user's focus and scroll position in it), so this is
        /// checked first.
        /// </summary>
        private bool DiskMatchesRecords()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(VariableSet)}");
            if (guids.Length != _records.Count)
            {
                return false;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                VariableSetRecord record = _records.Find(r => r.Key == guids[i]);
                bool unchanged = record != null &&
                    record.Path == AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!unchanged)
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        protected override void SetSubs(bool on)
        {
            base.SetSubs(on);

            if (on)
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
                subwindow.KeepRequested += OnKeepRequested;
                subwindow.MoveToggled += OnMoveToggled;
                subwindow.MoveAllRequested += OnMoveAllRequested;
                subwindow.StateMayHaveChanged += NotifyIfDirtyChanged;
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
                subwindow.StateMayHaveChanged -= NotifyIfDirtyChanged;
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
            string error = VariableSetAssetOps.ValidateName(name, _records, null, folder);
            if (error != null)
            {
                ShowError("Couldn't Create Variable Set", error);
                return;
            }

            Add(VariableSetRecord.CreateNew(name, folder));
        }

        private void Add(VariableSetRecord record)
        {
            _records.Add(record);
            SetsSubwindow?.Refresh(record.Key);
            NotifyIfDirtyChanged();
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

            string error = VariableSetAssetOps.ValidateName(newName, _records, record, record.Folder);
            if (error != null)
            {
                ShowError("Couldn't Rename Variable Set", error);
            }
            else
            {
                record.Name = newName;
            }

            SetsSubwindow?.Refresh(record.Key);
            NotifyIfDirtyChanged();
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
            string name = VariableSetAssetOps.SuggestName(record.Name, _records, folder);

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
                _records.Remove(record);
                record.Dispose();
            }
            else
            {
                record.PendingDelete = true;
            }

            SetsSubwindow?.Refresh();
            NotifyIfDirtyChanged();
        }

        internal void OnKeepRequested(VariableSetRecord record)
        {
            string error = VariableSetAssetOps.ValidateName(record.Name, _records, record);
            if (error != null)
            {
                // Something else took its name while it was staged for deletion
                ShowError("Couldn't Keep Variable Set",
                    error + "\n\nRename one of them, then try again.");
                return;
            }

            record.PendingDelete = false;
            SetsSubwindow?.Refresh(record.Key);
            NotifyIfDirtyChanged();
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
            NotifyIfDirtyChanged();
        }

        internal void OnMoveAllRequested()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            for (int i = 0; i < _records.Count; i++)
            {
                VariableSetRecord record = _records[i];
                if (!record.IsNew && !record.PendingDelete &&
                    record.Location == VariableSetLocation.Elsewhere)
                {
                    record.PendingMove = true;
                }
            }

            SetsSubwindow?.Refresh();
            NotifyIfDirtyChanged();
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

            VariableSetCommit.Result result;
            _isCommitting = true;
            try
            {
                result = VariableSetCommit.Run(_records);
            }
            finally
            {
                _isCommitting = false;
            }

            if (!result.Succeeded)
            {
                ShowError("Some Variable Set Changes Weren't Saved",
                    result.Summary + "\nThey're still staged in the Control Panel, so you can " +
                    "fix them and save again.");
            }

            SetsSubwindow?.Refresh();
            NotifyIfDirtyChanged();
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
            base.Dispose(); // Lets go of the inspector first, which is looking at the copies
            DisposeRecords();
            _lastReportedDirty = false;
        }
    }
}
