using AtMycelia.Myceliarium;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public class HyphlowEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string MainDisplayName => "Hyphlow";
        public override bool IsTopLevel => true;
        public override bool IsMeantToHaveSubwindow => false;
        public override string StringifiedState => "";

        protected override void PrepareSubentries()
        {
            base.PrepareSubentries();

            #region Register the instances
            var subsToAdd = new List<ControlPanelEntry>
            {
                new FcGlobalDefaultsEntry(),
                new FlowchartEditorQolEntry()
            };
            _subentries.AddRange(subsToAdd);
            #endregion

            #region Init Subentries
            for (int i = 0; i < _subentries.Count; i++)
            {
                var elem = _subentries[i];
                elem.Init();
                Tab.Register(elem.Tab);
            }
            #endregion

            // At this time, the left sidebar tab should have been prepped.
        }

        protected override void PrepareLeftSidebarTab()
        {
            _tab = new HyphlowTab();
            _tab.Init();
        }

    }

}