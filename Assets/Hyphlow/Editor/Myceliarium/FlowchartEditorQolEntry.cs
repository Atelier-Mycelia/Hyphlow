using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for managing Flowchart Editor QoL assets.
    /// Now uses a working-state list injected into the subwindow.
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

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FlowchartEditorQol)}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                FlowchartEditorQol real = AssetDatabase.LoadAssetAtPath<FlowchartEditorQol>(path);

                if (real != null)
                {
                    _realAssets[real.name] = real;
                    var copy = UnityObj.Instantiate(real);
                    _workingState.Add(copy);
                }
            }

            _workingState.Sort(ByName);
        }

        public IReadOnlyDictionary<string, FlowchartEditorQol> RealAssets => _realAssets;
        private readonly Dictionary<string, FlowchartEditorQol> _realAssets = 
            new Dictionary<string, FlowchartEditorQol>();

        // For letting the user edit copies of the QoL assets without modifying the
        // real ones until they click Save.
        private readonly List<FlowchartEditorQol> _workingState =
            new List<FlowchartEditorQol>();

        private int ByName(FlowchartEditorQol firstState, FlowchartEditorQol secondState)
        {
            int result = string.Compare(firstState.name, secondState.name, StringComparison.Ordinal);
            return result;
        }

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

        public override void OnSelected()
        {
            base.OnSelected();

            LoadWorkingStateFromRealAssets();

            if (_subwindow is FlowchartEditorQolSubwindow qolSubwindow)
            {
                qolSubwindow.RefreshFromWorkingState();
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
                _workingState.AddRange(qolsExtracted);

                if (_subwindow is FlowchartEditorQolSubwindow qolSubwindow)
                {
                    qolSubwindow.RefreshFromWorkingState();
                }

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
            base.Dispose();
        }

    }

}
