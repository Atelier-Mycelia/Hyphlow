using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UITKLabel = UnityEngine.UIElements.Label;
using UnityObj = UnityEngine.Object;
using AtMycelia.Collections;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Virtualized, reorderable variable list view (Unity 2022.3 LTS + Unity 6).
    /// The ListView recycles shell elements; a <see cref="VariableRowBinder"/> decides which
    /// pooled row handler lives in each shell. This class holds no per-variable row cache.
    /// </summary>
    public class VariableListView : IVariableListView
    {
        public VariableListView(VariableListViewInitArgs initArgs)
        {
            _listDisplay = initArgs.List;
            _countDisplay = initArgs.CountLabel;

            RowVisualHandlerPool pool = initArgs.HandlerPool ?? new RowVisualHandlerPool(
                new RowVisualHandlerResolver(), RowVisualHandlerRegistry.VisualHandlerLookup);
            _binder = new VariableRowBinder(pool);

            if (_listDisplay == null)
            {
                string errorMessage = $"VariableListView was not given a valid ListView in its init args.";
                Debug.LogError(errorMessage);
                return;
            }

            InitListViewStructure();

            if (initArgs.VariableSource != null)
            {
                SetSource(initArgs.VariableSource);
            }
        }

        protected ListView _listDisplay;
        protected UITKLabel _countDisplay;
        protected readonly VariableRowBinder _binder;

        /// <summary>
        /// Whoever should hear about edits made in the rows (normally the VariableRowManager).
        /// </summary>
        public IRowEditHost EditHost
        {
            get => _binder.Host;
            set => _binder.Host = value;
        }

        public virtual void SetSource(IVariableSource source)
        {
            _source = source;
            _lastSourceUid = _source != null ? _source.UniqueId : string.Empty;
            SyncFromSource();
        }

        private IVariableSource _source;

        protected virtual void InitListViewStructure()
        {
            _listDisplay.itemsSource = _varsToDisplay;
            _listDisplay.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _listDisplay.reorderable = true;
            _listDisplay.selectionType = SelectionType.Single;
            _listDisplay.showAlternatingRowBackgrounds = AlternatingRowBackground.All;

            _listDisplay.makeItem = _binder.CreateShell;
            _listDisplay.bindItem = (shell, index) =>
            {
                bool indexInRange = index >= 0 && index < _varsToDisplay.Count;
                if (!indexInRange || _varsToDisplay[index] == null)
                {
                    _binder.Unbind(shell);
                    return;
                }

                _binder.Bind(shell, _varsToDisplay[index]);
            };

            // Unbinding keeps the handler and its visuals in the shell, ready for the next
            // bind. Handlers only go back to the pool when the shell is destroyed or needs a
            // handler of another type.
            _listDisplay.unbindItem = (shell, index) => _binder.Unbind(shell);
            _listDisplay.destroyItem = shell => _binder.Destroy(shell);

            _listDisplay.itemIndexChanged += OnItemReordered;
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;

            UpdateCount();
        }

        public virtual IReadOnlyList<IVariable> VarsToDisplay => _varsToDisplay;

        protected readonly List<IVariable> _varsToDisplay = new();
        // ^Meant to be separate from that held by the source or FC

        protected virtual void OnItemReordered(int from, int to)
        {
            if (from == to || _varsToDisplay.Count == 0) return;
            OrderChanged?.Invoke(_varsToDisplay);
        }

        public virtual void AddVariable(IVariable toAdd)
        {
            if (this._isDisposed)
            {
                string logMessage = $"Tried to add variable to disposed VariableListView.";
                Debug.LogWarning(logMessage);
                return;
            }

            if (toAdd == null)
            {
                string logMessage = $"Tried to add a null variable to VariableListView.";
                Debug.LogWarning(logMessage);
                return;
            }

            if (_varsToDisplay.ContainsReference(toAdd))
            {
                return;
            }

            _varsToDisplay.Add(toAdd);
            Refresh();
        }

        public virtual void RemoveVariable(IVariable variable)
        {
            if (variable == null) return;

            // Reference equality on purpose: variables may override Equals, and List.IndexOf
            // would then pick the wrong one
            int idx = _varsToDisplay.FindIndex(elem => ReferenceEquals(elem, variable));
            if (idx < 0) return;

            _varsToDisplay.RemoveAt(idx);
            Refresh();
        }

        public virtual void SetVariables(IEnumerable<IVariable> varsToSet)
        {
            _varsToDisplay.Clear();
            if (varsToSet != null)
            {
                foreach (var elem in varsToSet)
                    if (elem != null)
                        _varsToDisplay.Add(elem);
            }
            Refresh();
        }

        public virtual void Clear()
        {
            _varsToDisplay.Clear();
            Refresh();
        }

        /// <summary>
        /// Rebinds whatever rows are on screen.
        /// </summary>
        public virtual void Refresh()
        {
            ReleaseTestShells();
            _listDisplay?.RefreshItems();
            UpdateCount();
        }

        /// <summary>
        /// Updates just the row showing the variable (if it's on screen) instead of
        /// rebinding the whole list.
        /// </summary>
        public virtual void RefreshValueOf(IVariable variable)
        {
            if (_binder.TryGetHandler(variable, out IRowVisualHandler handler))
            {
                handler.RefreshValue();
            }
        }

        public bool TryGetHandler(IVariable variable, out IRowVisualHandler handler)
        {
            return _binder.TryGetHandler(variable, out handler);
        }

        public int RowCount => _varsToDisplay.Count;

        public virtual void UpdateCount()
        {
            if (_countDisplay != null)
            {
                _countDisplay.text = $"Count: {_varsToDisplay.Count}";
            }
        }

        public event Action<IList<IVariable>> OrderChanged;

        public virtual void Dispose()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;

            ReleaseTestShells();
            _varsToDisplay.Clear();

            DisposeListDisplay();
            void DisposeListDisplay()
            {
                if (_listDisplay != null)
                {
                    _listDisplay.makeItem = null;
                    _listDisplay.bindItem = null;
                    _listDisplay.unbindItem = null;
                    _listDisplay.destroyItem = null;
                    _listDisplay.itemIndexChanged -= OnItemReordered;
                    _listDisplay.Clear();
                    _listDisplay = null;
                }
            }

            ResetCountDisplay();
            _binder.Dispose();

            _source = null;
            _isDisposed = true;

            void ResetCountDisplay()
            {
                if (_countDisplay == null)
                {
                    return;
                }

                _countDisplay.text = "Count: 0";
                _countDisplay = null;
            }
        }

        protected bool _isDisposed;


        #region Undo/Redo Sync

        protected void HandleUndoRedoPerformed()
        {
            if (!AcquireSourceIfLost())
            {
                return;
            }

            SyncFromSource();
            UpdateCount();
        }

        // Returns true if the source was found (or not even lost in the first place),
        // false otherwise.
        protected bool AcquireSourceIfLost()
        {
            bool sourceIsAlive = _source != null && (_source is not UnityObj unityObj || unityObj != null);
            if (sourceIsAlive) return true;

            FindFlowchart(out bool found);
            void FindFlowchart(out bool found)
            {
                found = false;
                var fcFound = UnityObj.FindObjectsByType<Flowchart>(FindObjectsSortMode.None)
                    .Where((fc) => fc.UniqueId == _lastSourceUid).FirstOrDefault();
                if (fcFound != null)
                {
                    SetSource(fcFound);
                    found = true;
                }
            }

            if (found)
            {
                return true;
            }

            FindVsa(out found);
            void FindVsa(out bool found)
            {
                found = false;
                var vsasInProject = Resources.LoadAll<VariableSet>("");
                var vsaFound = vsasInProject.Where((vsa) => vsa.UniqueId == _lastSourceUid).FirstOrDefault();
                if (vsaFound != null)
                {
                    SetSource(vsaFound);
                    found = true;
                }
            }
            return found;
        }

        protected virtual void SyncFromSource()
        {
            var source = _source?.Variables;
            if (source == null)
                return;

            _varsToDisplay.Clear();
            var sourceToAdd = source.Where(IsValidVar);

            static bool IsValidVar(IVariable elem)
            {
                // As in neither null or a destroyed UnityObj
                return elem != null && (elem is not UnityObj unityObj || unityObj != null);
            }

            _varsToDisplay.AddRange(sourceToAdd);
            Refresh();
        }

        private string _lastSourceUid = string.Empty;

        #endregion

        #region For tests only
        /// <summary>
        /// A ListView outside a panel never binds anything, so tests call this to get a bound
        /// row for every variable. They're dropped again on the next Refresh.
        /// </summary>
        public virtual void ForceMaterializeAllRowsForTests()
        {
            if (_listDisplay == null)
                return;

            VisualElement container = _listDisplay.contentContainer;
            if (container == null)
            {
                if (_testMaterializedContainer == null)
                {
                    _testMaterializedContainer = new VisualElement { name = "__TestMaterializedRows" };
                    _listDisplay.hierarchy.Add(_testMaterializedContainer);
                }
                container = _testMaterializedContainer;
            }

            foreach (IVariable variable in _varsToDisplay)
            {
                if (variable == null || _binder.TryGetHandler(variable, out _))
                    continue;

                VisualElement shell = _binder.CreateShell();
                _binder.Bind(shell, variable);
                container.Add(shell);
                _testShells.Add(shell);
            }
        }

        private void ReleaseTestShells()
        {
            if (_testShells.Count == 0)
                return;

            foreach (VisualElement shell in _testShells)
            {
                _binder.Destroy(shell);
                shell.RemoveFromHierarchy();
            }
            _testShells.Clear();
        }

        private readonly List<VisualElement> _testShells = new List<VisualElement>();
        VisualElement _testMaterializedContainer;
        #endregion

    }

    public interface IVariableListView : IDisposable
    {
        void AddVariable(IVariable variable);
        void RemoveVariable(IVariable variable);
        void SetVariables(IEnumerable<IVariable> variables);
        void Clear();
        void Refresh();
        void RefreshValueOf(IVariable variable);
        bool TryGetHandler(IVariable variable, out IRowVisualHandler handler);
        int RowCount { get; }
        IRowEditHost EditHost { get; set; }
        event Action<IList<IVariable>> OrderChanged;
        IReadOnlyList<IVariable> VarsToDisplay { get; }
    }

    public class VariableListViewInitArgs
    {
        public ListView List { get; set; }
        public UITKLabel CountLabel { get; set; }
        public IVariableSource VariableSource { get; set; }

        /// <summary>
        /// Optional; a pool fed by the handler registry is created when omitted.
        /// </summary>
        public RowVisualHandlerPool HandlerPool { get; set; }
    }
}
