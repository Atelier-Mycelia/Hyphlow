using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Hyphlow's top-level entry in the Control Panel. It has no subwindow of its own
    /// (no SubwindowType), only its subentries nested under its tab.
    /// </summary>
    public class HyphlowEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Hyphlow";
        public override bool IsTopLevel => true;

        protected override Type SidebarTabType => typeof(HyphlowRootTab);

        protected override IEnumerable<IControlPanelEntry> CreateSubentries()
        {
            return new IControlPanelEntry[]
            {
                new FcGlobalDefaultsEntry(),
                new FlowchartEditorQolEntry(),
                new VariableSetsEntry()
            };
        }
    }
}
