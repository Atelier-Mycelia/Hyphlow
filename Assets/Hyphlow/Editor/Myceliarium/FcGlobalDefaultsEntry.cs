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
        public override string SortingName => "Flowchart Defaults";

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
        public FlowchartGlobalDefaults WorkingState => _workingState;

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

        protected override void SetSubs(bool on)
        {
            if (_subwindow == null)
            {
                return;
            }

            base.SetSubs(on);
        }


    }

}