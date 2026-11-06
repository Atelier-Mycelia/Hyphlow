using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Owns the <see cref="VariableSetRecord"/>s that everything done in the Variable Sets
    /// entry is staged on. Edits go to detached copies of the sets, and only reach the
    /// project when <see cref="Commit"/> runs. Whether there's anything unsaved comes from
    /// the records (not from the history), since they each know what's staged on them.
    /// </summary>
    internal sealed class VariableSetsLogic : ControlPanelEntryLogic
    {
        // The subwindow holds onto this same instance, so it gets refilled rather than replaced
        private readonly List<VariableSetRecord> _records = new List<VariableSetRecord>();

        public IReadOnlyList<VariableSetRecord> Records => _records;

        /// <summary>
        /// True while <see cref="Commit"/> is running, to tell our own saving apart from
        /// changes that come from somewhere else.
        /// </summary>
        public bool IsCommitting { get; private set; }

        protected override void AdoptState(object loadResult)
        {
            DisposeRecords();
            if (loadResult is IEnumerable<VariableSetRecord> loaded)
            {
                _records.AddRange(loaded);
            }

            NotifyIfDirtyChanged();
        }

        #region Unsaved changes
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
        /// The answer is computed from the records, so the history's own change
        /// notification doesn't apply; this is what tells the panel when it may have changed.
        /// </summary>
        public void NotifyIfDirtyChanged()
        {
            bool dirty = AnyRecordIsDirty();
            if (dirty != _lastReportedDirty)
            {
                _lastReportedDirty = dirty;
                if (_owner != null)
                {
                    ControlPanelSignals.UnsavedChangesChanged(_owner);
                }
            }
        }

        private bool _lastReportedDirty;

        public override void MarkSaved()
        {
            base.MarkSaved();
            NotifyIfDirtyChanged();
        }
        #endregion

        #region Undo
        // Only edits to a set's variables and settings go on the history. Creating,
        // renaming, deleting and moving sets are staged on the records and show up as such
        // in the list, so Cancel is what takes those back.

        /// <summary>
        /// Raised after undoing or redoing changed a record's working copy, so the UI
        /// can show it.
        /// </summary>
        public event Action<VariableSetRecord> RecordRestoredByHistory = delegate { };

        /// <summary>
        /// To be called when the user may have edited a working copy (they're edited
        /// through the inspector, which doesn't announce it). Does nothing for copies that
        /// weren't, so it's fine to call it too often.
        /// </summary>
        public void RecordPendingEdits()
        {
            for (int i = 0; i < _records.Count; i++)
            {
                VariableSetRecord record = _records[i];
                if (!record.TryCaptureEdit(out string before, out string after))
                {
                    continue;
                }

                History.Record(new DelegateCommand(
                    execute: () => Restore(record, after),
                    undo: () => Restore(record, before)));
            }
        }

        private void Restore(VariableSetRecord record, string state)
        {
            bool stillThere = _records.Contains(record) && record.HasWorking;
            if (!stillThere)
            {
                return;
            }

            record.RestoreEdit(state);
            RecordRestoredByHistory(record);
            NotifyIfDirtyChanged();
        }

        public override void CommitPendingEdits()
        {
            base.CommitPendingEdits();
            RecordPendingEdits(); // So an undo right after an edit has that edit to undo
        }
        #endregion

        public int PendingDeleteCount
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

        public void Add(VariableSetRecord record)
        {
            _records.Add(record);
            NotifyIfDirtyChanged();
        }

        /// <summary>
        /// Whoever shows the record's detached copy should let go of it first.
        /// </summary>
        public void Remove(VariableSetRecord record)
        {
            _records.Remove(record);
            record.Dispose();
            NotifyIfDirtyChanged();
        }

        /// <summary>
        /// Carries out everything that's staged. What didn't go through stays staged.
        /// Whoever shows the records' detached copies should let go of them first, since
        /// deleted records dispose theirs.
        /// </summary>
        public VariableSetCommit.Result Commit()
        {
            IsCommitting = true;
            try
            {
                return VariableSetCommit.Run(_records);
            }
            finally
            {
                IsCommitting = false;
                NotifyIfDirtyChanged();
            }
        }

        private void DisposeRecords()
        {
            for (int i = 0; i < _records.Count; i++)
            {
                _records[i].Dispose();
            }

            _records.Clear();
        }

        public override void Dispose()
        {
            base.Dispose();
            DisposeRecords();
            _lastReportedDirty = false;
        }
    }
}
