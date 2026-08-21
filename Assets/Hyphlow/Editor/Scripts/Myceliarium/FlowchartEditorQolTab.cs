using AtMycelia.Myceliarium;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Tab for the Flowchart Editor QoL manager in the Control Panel.
    /// </summary>
    public sealed class FlowchartEditorQolTab : ControlPanelTab
    {
        public override string DisplayName => "Editor QoL";

        public override string PathToUxml => 
            "Editor/UIToolkitTemplates/Myceliarium/FlowchartEditorQolTab";

    }
}
