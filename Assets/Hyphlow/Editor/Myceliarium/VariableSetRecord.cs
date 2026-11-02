using System;
using UnityEditor;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    internal enum VariableSetLocation
    {
        VsaFolder,
        VsaSubfolder,
        /// <summary>
        /// Anywhere in Assets outside the VSA folder. These are the ones we offer to move.
        /// </summary>
        Elsewhere,//
        ReadOnly,
    }

    /// <summary>
    /// One Variable Set as the Control Panel sees it: what's on disk, plus whatever's been
    /// staged for it since. Nothing here touches the project until VariableSetCommit runs,
    /// so edits are made to <see cref="Working"/> (a detached copy) rather than the asset.
    /// </summary>
    internal sealed class VariableSetRecord : IDisposable
    {
        private VariableSetRecord() { }

        /// <summary>
        /// A set that's already on disk. The detached copy is made when first needed.
        /// </summary>
        internal static VariableSetRecord FromAsset(VariableSet asset, string guid, string path)
        {
            return new VariableSetRecord
            {
                Key = guid,
                Real = asset,
                Path = path,
                Location = VariableSetAssetOps.Classify(path),
                OriginalName = asset.name,
                Name = asset.name,
            };
        }

        /// <summary>
        /// A set that doesn't exist on disk yet; it's created when the record is committed.
        /// </summary>
        internal static VariableSetRecord CreateNew(string name, string targetFolder)
        {
            var record = new VariableSetRecord
            {
                Key = Guid.NewGuid().ToString("N"),
                TargetFolder = targetFolder,
                Name = name,
                Working = VariableSet.CreateDetached(),
            };
            record._editedJson = JsonOf(record.Working);
            return record;
        }

        /// <summary>
        /// A staged duplicate, starting out with the source's staged state (not its on-disk one).
        /// </summary>
        internal static VariableSetRecord CreateCopyOf(VariableSetRecord source, string name,
            string targetFolder)
        {
            var record = new VariableSetRecord
            {
                Key = Guid.NewGuid().ToString("N"),
                TargetFolder = targetFolder,
                Name = name,
                Working = source.EnsureWorking().CreateDetachedCopy(),
            };
            record._editedJson = JsonOf(record.Working);
            return record;
        }

        /// <summary>
        /// Identifies the record in the UI. Keeps its value when a staged set gets
        /// created, so selections survive saving.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The asset on disk. Null until a new set is committed.
        /// </summary>
        public VariableSet Real { get; private set; }

        /// <summary>
        /// Null for sets that aren't on disk yet.
        /// </summary>
        public string Path { get; private set; }
        public VariableSetLocation Location { get; private set; }

        /// <summary>
        /// Where a set that isn't on disk yet will be created.
        /// </summary>
        public string TargetFolder { get; private set; }

        public bool IsNew => Real == null;
        public bool IsReadOnly => Location == VariableSetLocation.ReadOnly && !IsNew;

        public string OriginalName { get; private set; }

        /// <summary>
        /// The name it'll have once saved. Changing it only stages the rename.
        /// </summary>
        public string Name { get; set; }

        public bool PendingDelete { get; set; }

        /// <summary>
        /// Only means something for sets outside the VSA folder.
        /// </summary>
        public bool PendingMove { get; set; }

        public bool IsRenamed => !IsNew && Name != OriginalName;

        /// <summary>
        /// The folder the set is in, or will be in once created.
        /// </summary>
        public string Folder => IsNew ? TargetFolder : VariableSetAssetOps.FolderOf(Path);

        /// <summary>
        /// The set being edited: a detached copy, so the asset stays untouched until saving.
        /// Null until needed for sets on disk, which is what keeps loading the list cheap.
        /// </summary>
        public VariableSet Working { get; private set; }

        public bool HasWorking => Working != null;

        public VariableSet EnsureWorking()
        {
            if (Working == null)
            {
                Working = Real.CreateDetachedCopy();
                _baselineJson = JsonOf(Working);
                _editedJson = _baselineJson;
                _stateChanged = false;
            }

            return Working;
        }

        private string _baselineJson;
        private bool _stateChanged;

        // The working copy as of the last edit the Control Panel's history knows about
        private string _editedJson;

        /// <summary>
        /// Whether the working copy was edited since this last said so (or since it was
        /// made). If so, hands back its state from before and after, for the history.
        /// </summary>
        internal bool TryCaptureEdit(out string before, out string after)
        {
            before = _editedJson;
            after = null;
            if (Working == null || before == null)
            {
                return false;
            }

            after = JsonOf(Working);
            if (after == before)
            {
                return false;
            }

            _editedJson = after;
            return true;
        }

        /// <summary>
        /// Puts the working copy back to a state from <see cref="TryCaptureEdit"/>. Its
        /// variable instances are kept where they still exist. Whatever's showing the copy
        /// should refresh afterwards.
        /// </summary>
        internal void RestoreEdit(string json)
        {
            VariableSet snapshot = VariableSet.CreateDetached();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, snapshot);
                Working.ApplyStateFrom(snapshot);
            }
            finally
            {
                UnityObj.DestroyImmediate(snapshot);
            }

            _editedJson = json;
            RefreshStateChanged();
        }

        /// <summary>
        /// Throws away the working copy, so the next EnsureWorking makes a new one from the
        /// asset as it is now. For when the asset was changed behind the record's back (say,
        /// restored after Play Mode) and there's nothing staged on the copy worth keeping.
        /// Whatever's showing the copy has to let go of it first.
        /// </summary>
        internal void ResetWorking()
        {
            if (IsNew)
            {
                return; // The copy is all there is of it
            }

            if (Working != null)
            {
                UnityObj.DestroyImmediate(Working);
            }

            Working = null;
            _baselineJson = null;
            _editedJson = null;
            _stateChanged = false;
        }

        /// <summary>
        /// Re-checks whether the working copy's variables or settings differ from what's on
        /// disk. Edits to it don't announce themselves, so whoever's showing it calls this.
        /// Returns whether the answer changed.
        /// </summary>
        public bool RefreshStateChanged()
        {
            bool changed = Working != null && _baselineJson != null &&
                JsonOf(Working) != _baselineJson;
            bool answerChanged = changed != _stateChanged;
            _stateChanged = changed;
            return answerChanged;
        }

        public bool StateChanged => _stateChanged;

        public bool IsDirty => IsNew || PendingDelete || IsRenamed || _stateChanged ||
            (PendingMove && Location == VariableSetLocation.Elsewhere);

        public int VariableCount => (Working != null ? Working : Real)?.Variables.Count ?? 0;

        private static string JsonOf(VariableSet set)
        {
            return EditorJsonUtility.ToJson(set);
        }

        #region Called by VariableSetCommit, as steps succeed
        internal void OnCreated(VariableSet created)
        {
            Real = created;
            OriginalName = created.name;
            Name = created.name;
            TargetFolder = null;
            SyncWithAsset();
            _baselineJson = JsonOf(Working);
            _stateChanged = false;
        }

        internal void OnRenamed()
        {
            OriginalName = Real.name;
            SyncWithAsset();
        }

        internal void OnMoved()
        {
            PendingMove = false;
            SyncWithAsset();
        }

        internal void OnStateCommitted()
        {
            _baselineJson = JsonOf(Working);
            _stateChanged = false;
        }

        private void SyncWithAsset()
        {
            Path = AssetDatabase.GetAssetPath(Real);
            Location = VariableSetAssetOps.Classify(Path);
        }
        #endregion

        public void Dispose()
        {
            if (Working != null)
            {
                UnityObj.DestroyImmediate(Working);
                Working = null;
            }
        }
    }
}
