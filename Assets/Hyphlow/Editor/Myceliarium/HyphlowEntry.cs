using AtMycelia.Myceliarium;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public class HyphlowEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Hyphlow";
        public override bool IsTopLevel => true;
        public override bool IsMeantToHaveSubwindow => false;

		protected override IEnumerable<IControlPanelEntry> CreateSubentries()
		{
            var subsToAdd = new List<ControlPanelEntry>
            {
                new FcGlobalDefaultsEntry(),
                new FlowchartEditorQolEntry()
            };
            return subsToAdd;
            
		}

        protected override void PrepareLeftSidebarTab()
        {
            _tab = new HyphlowRootTab();
            _tab.Init();
        }

    }

}