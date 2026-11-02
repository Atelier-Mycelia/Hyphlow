using AtMycelia.Myceliarium;
using System;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// For entries whose controls are bound straight to a working copy of some assets,
    /// so the subwindow changes the state itself instead of reporting each edit as a
    /// <see cref="FieldEdit"/>. Undo and redo work on snapshots of the whole state: when
    /// the UI says something may have been edited, the state is captured again, and if it
    /// differs from the last capture, the pair becomes one command on the history.
    /// </summary>
    internal abstract class JsonSnapshotLogic : ControlPanelEntryLogic
    {
        /// <summary>
        /// Raised after undoing or redoing replaced the state, so the UI can show it.
        /// </summary>
        public event Action StateRestoredByHistory = delegate { };

        protected abstract void CopyFrom(object loadResult);
        protected abstract string CaptureSnapshot();
        protected abstract void RestoreSnapshot(string snapshot);

        protected sealed override void AdoptState(object loadResult)
        {
            CopyFrom(loadResult);
            _lastSnapshot = CaptureSnapshot();
        }

        private string _lastSnapshot = string.Empty;

        /// <summary>
        /// To be called when the user may have edited the state. Does nothing if they
        /// didn't, so it's fine to call it too often.
        /// </summary>
        public void RecordPendingChange()
        {
            if (!IsLoaded)
            {
                return;
            }

            string before = _lastSnapshot;
            string after = CaptureSnapshot();
            if (after == before)
            {
                return;
            }

            _lastSnapshot = after;
            History.Record(new DelegateCommand(
                execute: () => Restore(after),
                undo: () => Restore(before)));
        }

        private void Restore(string snapshot)
        {
            RestoreSnapshot(snapshot);
            _lastSnapshot = snapshot;
            StateRestoredByHistory();
        }

        public override void CommitPendingEdits()
        {
            base.CommitPendingEdits();
            RecordPendingChange(); // So an undo right after an edit has that edit to undo
        }
    }
}
