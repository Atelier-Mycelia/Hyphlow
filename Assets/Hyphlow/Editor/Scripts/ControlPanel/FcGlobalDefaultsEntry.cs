using AtMycelia.Hyphlow.EditorExt;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Handles the UI logic for the Flowchart Global Defaults control panel entry.
    /// Manages the tab button and submenu for configuring global Flowchart settings.
    /// </summary>
    public sealed class FcGlobalDefaultsEntry : ControlPanelEntry
    {
        #region Display Configuration
        public override string MainDisplayName => "Flowchart Defaults";

        protected override string PathToTabButtonUXML => 
            "Editor/UIToolkitTemplates/ControlPanel/FlowchartDefaultsTab";

        protected override string PathToSubwindowUXML => 
            "Editor/UIToolkitTemplates/ControlPanel/FlowchartDefaultsSubmenu";
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
            _tempGlobals = UnityObj.Instantiate(globalDefaults);
        }

        private FlowchartGlobalDefaults _tempGlobals;

        protected override void ToggleSubs(bool on)
        {
            if (_subwindow == null)
            {
                return;
            }

            _subwindow.style.display = on ?
                DisplayStyle.Flex :
                DisplayStyle.None;
        }

        public override void Apply(string stringifiedState, out bool success)
        {
            try
            {
                JsonUtility.FromJsonOverwrite(stringifiedState, _tempGlobals);
                success = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to apply stringified state to " +
                    $"{nameof(FcGlobalDefaultsEntry)}: {ex.Message}");
                success = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_tempGlobals != null)
            {
                UnityObj.DestroyImmediate(_tempGlobals);
                _tempGlobals = null;
            }
        }

        public override string StringifiedState =>
            JsonUtility.ToJson(_tempGlobals, prettyPrint: false);
    }
}