using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public sealed class FcGlobalDefaultsSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml =>
            "Editor/Uxml/Myceliarium/FlowchartDefaultsSubmenu";

        /// <summary>
        /// The fields are bound to the working copy, so they apply the user's edits
        /// themselves; this only says that the user may be done with one.
        /// </summary>
        internal event Action StateMayHaveChanged = delegate { };

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

        protected override void ConfigureVisualElements()
        {
            // No need for further config
        }

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            _interactionWatcher?.Dispose();
            _interactionWatcher = null;
            if (wantsSubsActive)
            {
                _interactionWatcher = new InteractionWatcher(Root, RaiseStateMayHaveChanged);
            }
        }

        private InteractionWatcher _interactionWatcher;

        private void RaiseStateMayHaveChanged()
        {
            StateMayHaveChanged();
        }

        /// <summary>
        /// Shows the working copy the user is editing. Binding it is what makes the
        /// fields write to it.
        /// </summary>
        internal void ShowState(FlowchartGlobalDefaults workingState)
        {
            Unbind();
            _workingState = workingState;
            Bind();
        }

        private FlowchartGlobalDefaults _workingState;

        public override void Bind()
        {
            if (_workingState == null)
            {
                return;
            }

            var so = new SerializedObject(_workingState);

            _blockScopeField.BindProperty(so.FindProperty("_newBlockScope"));
            _firstBlockNameField.BindProperty(so.FindProperty("_firstBlockName"));
            _newBlockNameField.BindProperty(so.FindProperty("_newBlockName"));
            _firstBlockEvHanTypeField.BindProperty(so.FindProperty("_firstBlockEventHandlerTypeName"));
            _blockSizeField.BindProperty(so.FindProperty("_blockSize"));
            _stepPauseField.BindProperty(so.FindProperty("_stepPause"));

            // Without notifying, or the edit trackers would take it for something the user did
            _configAssetField.SetValueWithoutNotify(FlowchartGlobalDefaults.S);
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

        public override void Dispose()
        {
            _interactionWatcher?.Dispose();
            _interactionWatcher = null;
            base.Dispose();
        }
    }
}
