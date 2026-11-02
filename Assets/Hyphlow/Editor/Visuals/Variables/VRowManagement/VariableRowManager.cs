using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Sits between a variable source and the list view showing it: keeps the list in step
    /// with the source, and is the only place that applies edits made in the rows back onto
    /// the variables (with Undo and dirtying).
    /// </summary>
    public class VariableRowManager : IRowEditHost, IDisposable
    {
        /// <summary>
        /// Builds the list view and manager for a panel made from the VariableDisplayEditor
        /// UXML (it needs elements named rowList, varCountLabel and addVarButton).
        /// </summary>
        public static VariableRowManager CreateFor(VisualElement root, IReorderableVariableSource source)
        {
            if (root == null || source == null)
            {
                Debug.LogError("VariableRowManager.CreateFor needs both a root element and a variable source.");
                return null;
            }

            var listViewArgs = new VariableListViewInitArgs
            {
                List = root.Q<ListView>("rowList"),
                CountLabel = root.Q<UitkLabel>("varCountLabel"),
                VariableSource = source,
            };
            var view = new VariableListView(listViewArgs);

            var manager = new VariableRowManager();
            manager.Init(new VRowManagerInitArgs
            {
                Root = root,
                AddButton = root.Q<Button>("addVarButton"),
                VariableSource = source,
                VariableListView = view,
            });
            return manager;
        }

        public virtual void Init(VRowManagerInitArgs initArgs)
        {
            // In case we're being re-initialized; unsubscribe from what we had before
            SetSubs(false);
            _isDisposed = false;

            if (!ValidateArgs(initArgs))
            {
                Debug.LogError("Failed to initialize VariableRowManager.");
                return;
            }

            _listView = initArgs.VariableListView;
            _listView.EditHost = this;
            Root = initArgs.Root;
            _addButton = initArgs.AddButton;
            _variableSource = initArgs.VariableSource;
            _varManagerComponent = _variableSource as VariableManagerComponent;

            SetSubs(true);
            Refresh();
        }

        private bool ValidateArgs(VRowManagerInitArgs initArgs)
        {
            if (initArgs == null)
            {
                Debug.LogError("VariableRowManager was given a null args object.");
                return false;
            }

            int errorLogs = 0;
            if (initArgs.VariableSource == null)
            {
                Debug.LogError("VariableRowManager was not given a Flowchart to work with.");
                errorLogs++;
            }

            if (initArgs.VariableListView == null)
            {
                Debug.LogError($"VariableRowManager was not given a list view to work with.");
                errorLogs++;
            }

            if (initArgs.Root == null)
            {
                Debug.LogError("VariableRowManager was not given a root to work with.");
                errorLogs++;
            }

            if (initArgs.AddButton == null)
            {
                Debug.LogError("VariableRowManager was not given an add button to work with");
                errorLogs++;
            }

            return errorLogs == 0;
        }

        protected bool _isDisposed;
        protected IReorderableVariableSource _variableSource;
        protected VariableManagerComponent VarManagerComp => _variableSource as VariableManagerComponent;
        protected VariableManagerComponent _varManagerComponent;
        protected IVariableListView _listView;
        protected Button _addButton;

        public VisualElement Root { get; protected set; }

        #region Event Wiring
        protected virtual void SetSubs(bool on)
        {
            if (on == _subsActive || _variableSource == null || _listView == null || _addButton == null)
            {
                return;
            }

            if (on)
            {
                VariableSignals.PostValueChange += OnVariableValueChanged;
                _variableSource.VariableAdded += OnVariableAdded;
                _variableSource.VariableRemoved += OnVariableRemoved;
                _listView.OrderChanged += OnOrderChanged;
                _addButton.clicked += OnAddButtonClicked;
            }
            else
            {
                VariableSignals.PostValueChange -= OnVariableValueChanged;
                _variableSource.VariableAdded -= OnVariableAdded;
                _variableSource.VariableRemoved -= OnVariableRemoved;
                _listView.OrderChanged -= OnOrderChanged;
                _addButton.clicked -= OnAddButtonClicked;
            }

            _subsActive = on;
        }

        protected bool _subsActive = false;

        private void OnVariableValueChanged(IVariable variable, object newValue)
        {
            // Only the row showing this variable (if any) needs to catch up
            _listView?.RefreshValueOf(variable);
        }

        protected virtual void OnVariableAdded(IVariable added)
        {
            if (_isDisposed || added == null) return;
            _listView?.AddVariable(added);
        }

        protected virtual void OnVariableRemoved(IVariable removed)
        {
            if (_isDisposed || removed == null) return;
            _listView?.RemoveVariable(removed);
        }

        protected virtual void OnOrderChanged(System.Collections.Generic.IList<IVariable> newlyOrderedVars)
        {
            _variableSource.ReorderVariables(newlyOrderedVars);
        }

        protected virtual void OnAddButtonClicked()
        {
            Rect rect = _addButton.worldBound;
            // In the future, due to how Flowchart implements IVariableSource, we might
            // want just a single DoAddVariable method that takes IVariableSource.
            if (VarManagerComp != null)
            {
                VariableSelectPopupWindowContent.DoAddVariable(rect, "", VarManagerComp);
            }
            else if (_variableSource is IReorderableMuscariableSource muscaSource)
            {
                VariableSelectPopupWindowContent.DoAddVariable(rect, "", muscaSource);
            }
        }

        protected bool HasLiveVariableSourceReference()
        {
            if (_variableSource == null)
            {
                return false;
            }

            if (_variableSource is UnityObj unityObj)
            {
                return unityObj != null;
            }

            return true;
        }

        protected void EnsureVariableSource(IVariableSource ownerCandidate)
        {
            if (_isDisposed || HasLiveVariableSourceReference())
            {
                return;
            }

            if (TryRebindTo(ownerCandidate))
            {
                return;
            }

            var fcFromTracker = EditorSelectionTracker.ActiveFlowchart;
            TryRebindTo(fcFromTracker);
        }

        protected bool TryRebindTo(IVariableSource candidate)
        {
            if (candidate is not IReorderableVariableSource reorderable)
            {
                return false;
            }

            return TryRebindTo(reorderable);
        }

        protected bool TryRebindTo(IReorderableVariableSource newSource)
        {
            if (newSource == null || _isDisposed)
            {
                return false;
            }

            if (ReferenceEquals(_variableSource, newSource) && HasLiveVariableSourceReference())
            {
                return true;
            }

            SetSubs(false);
            _variableSource = newSource;
            SetSubs(true);
            Refresh();
            return HasLiveVariableSourceReference();
        }

        protected UnityObj ResolveRecordTarget(IVariable variable)
        {
            if (variable == null)
            {
                return null;
            }

            UnityObj direct = variable as UnityObj;
            if (direct != null)
            {
                return direct;
            }

            if (variable.Owner is Flowchart fc)
            {
                if (_varManagerComponent == null)
                {
                    _varManagerComponent = fc.GetComponent<VariableManagerComponent>();
                }
                return _varManagerComponent;
            }

            if (variable.Owner is UnityObj ownerObj && ownerObj != null)
            {
                return ownerObj;
            }

            if (_variableSource is UnityObj sourceObj && sourceObj != null)
            {
                return sourceObj;
            }

            return null;
        }
        #endregion

        #region IRowEditHost
        // Rows report straight to the manager of the list they sit in, so there's no need to
        // work out whether an edit belongs to us. We only make sure the source is still usable.

        public virtual void RemoveRequested(IVariable variable)
        {
            if (!ReadyToEdit(variable))
            {
                return;
            }

            IVariableSource owner = variable.Owner ?? _variableSource;

            if (owner is UnityObj ownerObj && ownerObj != null)
            {
                string typeName = variable.ContentType.Name;
                if (typeName.Equals("Single"))
                {
                    typeName = "Float";
                }

                RecordUndo(ownerObj, $"Remove {typeName} Variable");
            }

            owner.RemoveVariable(variable);

            if (variable is UnityObj legacyVar && legacyVar != null)
            {
                UnityObj.DestroyImmediate(legacyVar);
            }
        }

        public virtual void KeyEdited(IVariable variable, string newKey)
        {
            if (!ReadyToEdit(variable))
            {
                return;
            }

            if (variable.Key != newKey)
            {
                RecordAndApplyChange(variable, "Key", (varToChange) => varToChange.Key = newKey);
            }
        }

        public virtual void ScopeEdited(IVariable variable, AccessScope newScope)
        {
            if (!ReadyToEdit(variable))
            {
                return;
            }

            if (variable.Scope != newScope)
            {
                RecordAndApplyChange(variable, "Scope", (varToChange) => varToChange.Scope = newScope);
            }
        }

        public virtual void ValueEdited(IVariable variable, object newValue)
        {
            if (!ReadyToEdit(variable))
            {
                return;
            }

            if (!Equals(variable.BoxedValue, newValue))
            {
                RecordAndApplyChange(variable, "Value", (varToChange) => varToChange.BoxedValue = newValue);
            }
        }

        protected virtual bool ReadyToEdit(IVariable variable)
        {
            if (_isDisposed || variable == null)
            {
                return false;
            }

            if (!HasLiveVariableSourceReference())
            {
                EnsureVariableSource(variable.Owner);
            }

            if (variable.Owner == null && HasLiveVariableSourceReference())
            {
                variable.Owner = _variableSource;
            }

            if (_variableSource is Flowchart fc && _varManagerComponent == null)
            {
                _varManagerComponent = fc.GetComponent<VariableManagerComponent>();
            }

            return _variableSource != null;
        }

        private static void MarkDirty(UnityObj target)
        {
            if (target == null || Application.isPlaying)
            {
                return;
            }

            EditorUtility.SetDirty(target);

            if (target is Component component && component.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }

            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        /// <summary>
        /// Detached sets are staged on by the Control Panel, which has its own history. If
        /// edits to them also went on Unity's undo stack, Edit > Undo would change a set
        /// behind the panel's back.
        /// </summary>
        protected virtual bool RecordsUnityUndo => !(_variableSource is VariableSet set && set.IsDetached);

        private void RecordUndo(UnityObj target, string label)
        {
            if (RecordsUnityUndo)
            {
                Undo.RecordObject(target, label);
            }
        }

        protected void RecordAndApplyChange(IVariable variable, string description, Action<IVariable> applyChange)
        {
            if (variable == null || applyChange == null)
            {
                return;
            }

            EnsureVariableSource(variable.Owner);

            string varType = variable.GetType().Name;
            UnityObj toRecord = ResolveRecordTarget(variable);
            if (toRecord == null)
            {
                Debug.LogError($"VariableRowManager could not resolve a UnityEngine.Object " +
                    $"to record for {varType} {description}.");
                return;
            }

            RecordUndo(toRecord, $"Change to {varType} {description}");

            applyChange(variable);

            if (!HasLiveVariableSourceReference())
            {
                EnsureVariableSource(variable.Owner);
            }

            MarkDirty(toRecord);

            if (_variableSource is ScriptableObject so)
            {
                so.MarkDirtyAndSave();
            }
        }
        #endregion

        #region Refresh APIs
        public void Refresh()
        {
            if (_isDisposed || _variableSource == null || _listView == null)
                return;

            _listView.SetVariables(_variableSource.Variables);
        }
        #endregion

        #region Dispose
        public virtual void Dispose()
        {
            if (_isDisposed) return;

            SetSubs(false);
            ReleaseRowsFromList();

            if (_listView != null)
            {
                _listView.EditHost = null;
                _listView.Dispose();
            }

            _listView = null;
            _variableSource = null;
            Root = null;
            _isDisposed = true;
        }

        public virtual void ReleaseRowsFromList()
        {
            _listView?.Clear();
        }

        #endregion
    }
}
