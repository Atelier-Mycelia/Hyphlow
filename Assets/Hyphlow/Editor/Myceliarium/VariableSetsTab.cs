using AtMycelia.Myceliarium;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Tab for the Variable Sets manager in the Control Panel.
    /// </summary>
    public sealed class VariableSetsTab : ControlPanelTab
    {
        public override string DisplayName => "Variable Sets";

        public override string PathToUxml =>
            "Editor/Uxml/Myceliarium/VariableSetsTab";
    }
}
