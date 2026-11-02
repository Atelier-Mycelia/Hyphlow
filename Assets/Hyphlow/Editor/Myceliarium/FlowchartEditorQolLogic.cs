using AtMycelia.Hyphlow.EditorExt;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Owns the working copies of the Flowchart Editor QoL assets, so the real ones stay
    /// untouched until the user saves. Creating, renaming and deleting work on the copies
    /// too, with the saver turning the difference into asset operations.
    /// </summary>
    internal sealed class FlowchartEditorQolLogic : JsonSnapshotLogic
    {
        // The subwindow's list shows this very instance, so it's refilled rather than replaced
        public List<FlowchartEditorQol> Working { get; } = new List<FlowchartEditorQol>();

        /// <summary>
        /// The assets on disk, by name. Brought up to date after every save, since the
        /// saver may have created or deleted some.
        /// </summary>
        public IReadOnlyDictionary<string, FlowchartEditorQol> RealAssets => _realAssets;
        private readonly Dictionary<string, FlowchartEditorQol> _realAssets =
            new Dictionary<string, FlowchartEditorQol>();

        protected override void CopyFrom(object loadResult)
        {
            DestroyWorkingCopies();

            var found = loadResult as IReadOnlyList<FlowchartEditorQol> ??
                Array.Empty<FlowchartEditorQol>();
            RefreshRealAssets(found);

            for (int i = 0; i < found.Count; i++)
            {
                FlowchartEditorQol copy = UnityObj.Instantiate(found[i]);
                copy.name = found[i].name; // Instantiate adds a "(Clone)" suffix
                Working.Add(copy);
            }
        }

        private void RefreshRealAssets(IReadOnlyList<FlowchartEditorQol> found)
        {
            _realAssets.Clear();
            for (int i = 0; i < found.Count; i++)
            {
                _realAssets[found[i].name] = found[i];
            }
        }

        public override void MarkSaved()
        {
            base.MarkSaved();
            RefreshRealAssets(FlowchartEditorQolLoader.FindAll());
        }

        #region Editing
        public void Create(string name)
        {
            var created = ScriptableObject.CreateInstance<FlowchartEditorQol>();
            created.name = name;
            Working.Add(created);

            //_workingState.Sort(ByName);
            // Todo: Implement a sort button in the subwindow instead of auto-sorting on create.
            RecordPendingChange();
        }

        public void Rename(int index, string newName)
        {
            Working[index].name = newName;
            RecordPendingChange();
        }

        public void Delete(int index)
        {
            FlowchartEditorQol removed = Working[index];
            Working.RemoveAt(index);
            UnityObj.DestroyImmediate(removed);
            RecordPendingChange();
        }

        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < Working.Count;
        }

        /// <summary>
        /// The asset on disk that the working copy at the index stands for, if there is one.
        /// </summary>
        public FlowchartEditorQol FindRealFor(int index)
        {
            return _realAssets.TryGetValue(Working[index].name, out FlowchartEditorQol real) ?
                real :
                null;
        }
        #endregion

        #region Snapshots
        [Serializable]
        private sealed class Snapshot
        {
            public List<Item> items = new List<Item>();
        }

        [Serializable]
        private sealed class Item
        {
            public string name;
            public string json;
        }

        protected override string CaptureSnapshot()
        {
            var snapshot = new Snapshot();
            for (int i = 0; i < Working.Count; i++)
            {
                snapshot.items.Add(new Item
                {
                    name = Working[i].name,
                    json = JsonUtility.ToJson(Working[i])
                });
            }

            return JsonUtility.ToJson(snapshot);
        }

        protected override void RestoreSnapshot(string snapshot)
        {
            DestroyWorkingCopies();

            Snapshot restored = JsonUtility.FromJson<Snapshot>(snapshot);
            for (int i = 0; i < restored.items.Count; i++)
            {
                var copy = ScriptableObject.CreateInstance<FlowchartEditorQol>();
                JsonUtility.FromJsonOverwrite(restored.items[i].json, copy);
                copy.name = restored.items[i].name;
                Working.Add(copy);
            }
        }
        #endregion

        private void DestroyWorkingCopies()
        {
            for (int i = 0; i < Working.Count; i++)
            {
                if (Working[i] != null)
                {
                    UnityObj.DestroyImmediate(Working[i]);
                }
            }

            Working.Clear();
        }

        public override void Dispose()
        {
            base.Dispose();
            DestroyWorkingCopies();
            _realAssets.Clear();
        }
    }
}
