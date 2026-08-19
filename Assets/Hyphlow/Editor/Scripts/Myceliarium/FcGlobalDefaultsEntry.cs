using AtMycelia.Hyphlow.EditorExt;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Myceliarium
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

        protected override string PathToTabButtonUXML => 
            "Editor/UIToolkitTemplates/Myceliarium/FlowchartDefaultsTab";

        protected override string PathToSubwindowUXML =>
            "Editor/UIToolkitTemplates/Myceliarium/FlowchartDefaultsSubmenu";
        #endregion

        public override void Init(bool forceReinit = false)
        {
            LoadTempSettingsFromAsset();
            base.Init(forceReinit);
        }

        protected override void PrepareLeftSidebarTab()
        {
            _tab = new FcGlobalDefaultsTab();
            _tab.Init();
        }

        protected override void PrepareSubwindow()
        {
            base.PrepareSubwindow();
            RegisterVisualElements();
            BindVisualElementsToWorkingState();
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
            _workingState = UnityObj.Instantiate(globalDefaults);
        }

        private FlowchartGlobalDefaults _workingState;

        private void RegisterVisualElements()
        {
            _blockScopeField = _subwindow.Q<EnumField>("NewBlockScope");
            _firstBlockNameField = _subwindow.Q<TextField>("FirstBlockName");
            _newBlockNameField = _subwindow.Q<TextField>("NewBlockName");
            _blockSizeField = _subwindow.Q<Vector2Field>("BlockSize");
            _stepPauseField = _subwindow.Q<Slider>("StepPause");
            _firstBlockEvHanTypeField = _subwindow.Q<TextField>("FirstBlockEventHandlerType");
            _configAssetField = _subwindow.Q<ObjectField>("ConfigAsset");
        }

        private EnumField _blockScopeField;
        private TextField _firstBlockNameField, _newBlockNameField, _firstBlockEvHanTypeField;
        private Vector2Field _blockSizeField;
        private Slider _stepPauseField;
        private ObjectField _configAssetField;

        private void BindVisualElementsToWorkingState()
        {
            _blockScopeField.BindProperty(
                new SerializedObject(_workingState).FindProperty("_newBlockScope"));
            _firstBlockNameField.BindProperty(
                new SerializedObject(_workingState).FindProperty("_firstBlockName"));
            _newBlockNameField.BindProperty(
                new SerializedObject(_workingState).FindProperty("_newBlockName"));
            _firstBlockEvHanTypeField.BindProperty(
                new SerializedObject(_workingState).FindProperty("_firstBlockEventHandlerTypeName"));
            _blockSizeField.BindProperty(
                new SerializedObject(_workingState).FindProperty("_blockSize"));
            _stepPauseField.BindProperty(
                new SerializedObject(_workingState).FindProperty("_stepPause"));

            _configAssetField.value = FlowchartGlobalDefaults.S;
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
                UnbindVisualElements();
                BindVisualElementsToWorkingState();
                success = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to apply stringified state to " +
                    $"{nameof(FcGlobalDefaultsEntry)}: {ex.Message}");
                success = false;
            }
        }

        private void UnbindVisualElements()
        {
            _blockScopeField.Unbind();
            _firstBlockNameField.Unbind();
            _newBlockNameField.Unbind();
            _firstBlockEvHanTypeField.Unbind();
            _blockSizeField.Unbind();
            _stepPauseField.Unbind();
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_workingState != null)
            {
                UnityObj.DestroyImmediate(_workingState);
                _workingState = null;
            }
        }

        public override string StringifiedState =>
            JsonUtility.ToJson(_workingState, prettyPrint: false);
    }

    public class FcGlobalDefaultsTab : ControlPanelTab
    {
        public override string DisplayName => "FC Global Defaults";
        public override string PathToUxml => "Editor/UIToolkitTemplates/Myceliarium/" +
            "FlowchartDefaultsTab";
    }
}