using AtMycelia.Hyphlow.EditorExt;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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

    public class FcGlobalDefaultsTab : ControlPanelTab
    {
        public override string DisplayName => "FC Global Defaults";
        public override string PathToUxml => "Editor/UIToolkitTemplates/Myceliarium/" +
            "FlowchartDefaultsTab";
    }

    public sealed class FcGlobalDefaultsSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml =>
            "Editor/UIToolkitTemplates/Myceliarium/FlowchartDefaultsSubmenu";

        public FcGlobalDefaultsSubwindow(FlowchartGlobalDefaults workingState)
        {
            _workingState = workingState;
        }

        private FlowchartGlobalDefaults _workingState;

        protected override void RegisterVisualElements()
        {
            _blockScopeField = Root.Q<EnumField>("NewBlockScope");
            _firstBlockNameField = Root.Q<TextField>("FirstBlockName");
            _newBlockNameField = Root.Q<TextField>("NewBlockName");
            _firstBlockEvHanTypeField = Root.Q<TextField>("FirstBlockEventHandlerType");
            _blockSizeField = Root.Q<Vector2Field>("BlockSize");
            _stepPauseField = Root.Q<Slider>("StepPause");
            _configAssetField = Root.Q<ObjectField>("ConfigAsset");
        }

        private EnumField _blockScopeField;
        private TextField _firstBlockNameField;
        private TextField _newBlockNameField;
        private TextField _firstBlockEvHanTypeField;
        private Vector2Field _blockSizeField;
        private Slider _stepPauseField;
        private ObjectField _configAssetField;

        public override void Init()
        {
            base.Init();
            Unbind();
            Bind();
        }

        public override void Bind()
        {
            var so = new SerializedObject(_workingState);

            _blockScopeField.BindProperty(so.FindProperty("_newBlockScope"));
            _firstBlockNameField.BindProperty(so.FindProperty("_firstBlockName"));
            _newBlockNameField.BindProperty(so.FindProperty("_newBlockName"));
            _firstBlockEvHanTypeField.BindProperty(so.FindProperty("_firstBlockEventHandlerTypeName"));
            _blockSizeField.BindProperty(so.FindProperty("_blockSize"));
            _stepPauseField.BindProperty(so.FindProperty("_stepPause"));

            _configAssetField.value = FlowchartGlobalDefaults.S;
        }

        public override void Unbind()
        {
            _blockScopeField.Unbind();
            _firstBlockNameField.Unbind();
            _newBlockNameField.Unbind();
            _firstBlockEvHanTypeField.Unbind();
            _blockSizeField.Unbind();
            _stepPauseField.Unbind();
        }
    }

}