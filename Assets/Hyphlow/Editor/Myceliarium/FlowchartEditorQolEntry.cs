using AtMycelia.Myceliarium;
using AtMycelia.Hyphlow.EditorExt;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for managing Flowchart Editor QoL assets.
    /// Uses a working-state list injected into the subwindow.
    /// </summary>
    public sealed class FlowchartEditorQolEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string MainDisplayName => "Editor QoL";
        public override bool IsTopLevel => false;

        internal IReadOnlyList<FlowchartEditorQol> WorkingState => _workingState;

        public override void Init(bool forceReinit = false)
        {
            LoadWorkingStateFromRealAssets();
            base.Init(forceReinit);
        }

        /// <summary>
        /// Loads real assets into working-state copies.
        /// </summary>
        private void LoadWorkingStateFromRealAssets()
        {
            _workingState.Clear();
            _realAssets.Clear();
            _workingToRealMap.Clear();

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FlowchartEditorQol)}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                FlowchartEditorQol real = AssetDatabase.LoadAssetAtPath<FlowchartEditorQol>(path);

                if (real != null)
                {
                    _realAssets[real.name] = real;
                    FlowchartEditorQol copy = UnityObj.Instantiate(real);
                    copy.name = real.name; // Keep the name consistent for display purposes.
                    _workingState.Add(copy);
                    _workingToRealMap[copy] = real;
                }
            }

            //_workingState.Sort(ByName);
        }

        public IReadOnlyDictionary<string, FlowchartEditorQol> RealAssets => _realAssets;
        private readonly Dictionary<string, FlowchartEditorQol> _realAssets =
            new Dictionary<string, FlowchartEditorQol>();

        // For letting the user edit copies of the QoL assets without modifying the
        // real ones until they click Save.
        private readonly List<FlowchartEditorQol> _workingState =
            new List<FlowchartEditorQol>();

        private readonly Dictionary<FlowchartEditorQol, FlowchartEditorQol> _workingToRealMap =
            new Dictionary<FlowchartEditorQol, FlowchartEditorQol>();

        protected override void PrepareLeftSidebarTab()
        {
            _tab = new FlowchartEditorQolTab();
            _tab.Init();
        }

        protected override void PrepareSubwindow()
        {
            _subwindow = new FlowchartEditorQolSubwindow(_workingState);
            _subwindow.Init();
        }

        protected override void ToggleSubs(bool on)
        {
            base.ToggleSubs(on);

            if (_subwindow is not FlowchartEditorQolSubwindow qolSubwindow)
            {
                return;
            }

            if (on)
            {
                qolSubwindow.CreateRequested += OnCreateRequested;
                qolSubwindow.RenameRequested += OnRenameRequested;
                qolSubwindow.DeleteRequested += OnDeleteRequested;
                qolSubwindow.SelectRequested += OnSelectRequested;
                ControlPanelSignals.SaveCompleted += OnSaveCompleted;
            }
            else
            {
                qolSubwindow.CreateRequested -= OnCreateRequested;
                qolSubwindow.RenameRequested -= OnRenameRequested;
                qolSubwindow.DeleteRequested -= OnDeleteRequested;
                qolSubwindow.SelectRequested -= OnSelectRequested;
                ControlPanelSignals.SaveCompleted -= OnSaveCompleted;
            }
        }

        private void OnCreateRequested(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                EditorUtility.DisplayDialog("Invalid Name",
                    "Please enter a valid asset name.", "OK");
                return;
            }

            var wState = ScriptableObject.CreateInstance<FlowchartEditorQol>();
            wState.name = name;
            _workingState.Add(wState);
            
            //_workingState.Sort(ByName); 
            // Todo: Implement a sort button in the subwindow instead of auto-sorting on create.

            RefreshSubwindow();
        }

        private void RefreshSubwindow()
        {
            if (_subwindow is FlowchartEditorQolSubwindow qolSubwindow)
            {
                qolSubwindow.Refresh();
            }
        }

        private void OnRenameRequested(int index, string newName)
        {
            bool isValidIndex = index >= 0 && index < _workingState.Count;
            if (!isValidIndex)
            {
                string logMessage = $"Invalid index {index} for renaming QoL asset. " +
                    $"Valid range is 0 to {_workingState.Count - 1}.";
                Debug.LogError(logMessage);
                return;
            }
            
            if (!string.IsNullOrEmpty(newName))
            {
                FlowchartEditorQol wState = _workingState[index];
                wState.name = newName;
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Name",
                    "Please enter a valid asset name.", "OK");
            }

            RefreshSubwindow();
        }

        private void OnDeleteRequested(int index)
        {
            bool isValidIndex = index >= 0 && index < _workingState.Count;
            if (!isValidIndex)
            {
                string logMessage = $"Invalid index {index} for deleting QoL asset. " +
                    $"Valid range is 0 to {_workingState.Count - 1}.";
                Debug.LogError(logMessage);
                return;
            }

            FlowchartEditorQol wState = _workingState[index];
            _workingState.RemoveAt(index);
            _workingToRealMap.Remove(wState);

            RefreshSubwindow();
        }

        private void OnSelectRequested(int index)
        {
            bool isValidIndex = index >= 0 && index < _workingState.Count;
            if (!isValidIndex)
            {
                return;
            }

            FlowchartEditorQol wState = _workingState[index];
            FlowchartEditorQol real = _workingToRealMap.TryGetValue(wState, out var mappedReal) ?
                mappedReal :
                null;

            if (real == null)
            {
                EditorUtility.DisplayDialog("No Real Asset Found",
                    $"No real asset found for working-state '{wState.name}'. " +
                    $"Need to click Save to make sure it is written on disk.",
                    "OK");
                return;
            }

            Selection.activeObject = real;
            EditorGUIUtility.PingObject(real);
        }

        private void OnSaveCompleted(IControlPanelEntry completedFor)
        {
            bool ignoreIt = completedFor != this;
            if (ignoreIt)
            {
                return;
            }

            Refresh();
        }

        public override void OnSelected()
        {
            base.OnSelected();

            LoadWorkingStateFromRealAssets();

            if (_subwindow is FlowchartEditorQolSubwindow qolSubwindow)
            {
                qolSubwindow.Refresh();
            }
        }

        /// <summary>
        /// JSON representation of working-state.
        /// </summary>
        public override string StringifiedState =>
            EditorJsonUtility.ToJson(_workingState, prettyPrint: true);

        /// <summary>
        /// Applies JSON to working-state, then refreshes the subwindow.
        /// </summary>
        public override void Apply(string stringifiedState, out bool success)
        {
            try
            {
                var qolsExtracted = JsonUtility.FromJson<List<FlowchartEditorQol>>(stringifiedState);

                _workingState.Clear();
                _workingToRealMap.Clear();

                if (qolsExtracted != null)
                {
                    _workingState.AddRange(qolsExtracted);
                }

                for (int i = 0; i < _workingState.Count; i++)
                {
                    FlowchartEditorQol wState = _workingState[i];
                    if (_realAssets.TryGetValue(wState.name, out FlowchartEditorQol real))
                    {
                        _workingToRealMap[wState] = real;
                    }
                }

                RefreshSubwindow();
                success = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to apply QoL working-state: {ex.Message}");
                success = false;
            }
        }

        public override void Dispose()
        {
            _workingState.Clear();
            _workingToRealMap.Clear();
            base.Dispose();
        }

        public void Refresh()
        {
            LoadWorkingStateFromRealAssets();
            RefreshSubwindow();
        }
    }
}
