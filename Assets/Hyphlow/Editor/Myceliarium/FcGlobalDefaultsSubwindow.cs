using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public sealed class FcGlobalDefaultsSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml =>
            "Editor/Uxml/Myceliarium/FlowchartDefaultsSubmenu";

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