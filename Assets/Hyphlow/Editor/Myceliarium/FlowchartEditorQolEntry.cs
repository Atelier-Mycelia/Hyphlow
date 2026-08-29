using AtMycelia.Myceliarium;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for managing Flowchart Editor QoL assets.
    /// Allows creating, editing, and deleting QoL configuration assets.
    /// </summary>
    public sealed class FlowchartEditorQolEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string MainDisplayName => "Editor QoL";
        public override bool IsTopLevel => false;
        public override string StringifiedState => string.Empty;

        protected override void PrepareLeftSidebarTab()
        {
            _tab = new FlowchartEditorQolTab();
            _tab.Init();
        }

        protected override void PrepareSubwindow()
        {
            _subwindow = new FlowchartEditorQolSubwindow();
            _subwindow.Init();
        }

        public override void OnSelected()
        {
            base.OnSelected();

            // Refresh the list when this entry is selected
            if (_subwindow is FlowchartEditorQolSubwindow qolSubwindow)
            {
                qolSubwindow.LoadAllQolAssets();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
