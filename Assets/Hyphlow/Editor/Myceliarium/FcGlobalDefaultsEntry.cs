using AtMycelia.Hyphlow.EditorExt;
using System;
using UnityEngine;
using AtMycelia.Myceliarium;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Handles the UI logic for the Flowchart Global Defaults control panel entry.
    /// Manages the tab button and submenu for configuring global Flowchart settings.
    /// Later on, this will become a sub-entry for the Hyphlow Entry.
    /// </summary>
    public sealed class FcGlobalDefaultsEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        #region Display Configuration
        public override string MainDisplayName => "Flowchart Defaults";

        #endregion

        public override void Init(bool forceReinit = false)
        {
            LoadTempSettingsFromAsset();
            base.Init(forceReinit);
        }

        private void LoadTempSettingsFromAsset()
        {
            var globalDefaults = FlowchartGlobalDefaults.S;
            if (globalDefaults == null)
            {
                Debug.LogError("FlowchartGlobalDefaults asset not found. Please ensure it" +
                    "exists in the Resources folder.");
                return;
            }

            if (_workingState == null)
            {
                _workingState = ScriptableObject.CreateInstance<FlowchartGlobalDefaults>();
            }
            
            globalDefaults.ApplyStateTo(_workingState);
        }

        private FlowchartGlobalDefaults _workingState;

        protected override void PrepareLeftSidebarTab()
        {
            _tab = new FcGlobalDefaultsTab();
            _tab.Init();
        }

        protected override void PrepareSubwindow()
        {
            _subwindow ??= new FcGlobalDefaultsSubwindow(_workingState);
            _subwindow.Init();
        }

        protected override void ToggleSubs(bool on)
        {
            if (_subwindow == null)
            {
                return;
            }

            base.ToggleSubs(on);
        }

        public override void Apply(string stringifiedState, out bool success)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(stringifiedState, _workingState);
                _subwindow.Unbind();
                _subwindow.Bind();
                success = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to apply stringified state to " +
                    $"{nameof(FcGlobalDefaultsEntry)}: {ex.Message}");
                success = false;
            }
        }

        public override string StringifiedState =>
            JsonUtility.ToJson(_workingState, prettyPrint: true);
    }

}