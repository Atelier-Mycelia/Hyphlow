using AtMycelia.Myceliarium;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.ControlPanel
{
    public class HyphlowEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string MainDisplayName => "Hyphlow";
        public override bool TopLevelEntry => true;

        // TODO: Implement a working state that basically gathers up 
        // those of the sub-entries.
        public override string StringifiedState => throw new System.NotImplementedException();

        protected override string PathToTabButtonUXML => 
            "Editor/UIToolkitTemplates/Myceliarium/HyphlowTab";

        // The Hyphlow entry is really just a grouping for the other entries
        // such as the one for the FC Global Defaults. Thus, we don't need
        // a subwindow for it.
        protected override string PathToSubwindowUXML => "";

        protected override void PrepareSubentries()
        {
            base.PrepareSubentries();

            #region Register the instances
            var subsToAdd = new List<ControlPanelEntry>
            {
                new FcGlobalDefaultsEntry()
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

            // At this time, the left sidebar tab has been prepped.
        }

        protected override void PrepareLeftSidebarTab()
        {
            _tab = new HyphlowTab();
            _tab.Init();
        }

    }

    public class HyphlowTab : ControlPanelTab
    {
        public override string DisplayName => "Hyphlow";
        public override string PathToUxml => "Editor/UIToolkitTemplates/Myceliarium/HyphlowTab";

        protected override void RegisterButton()
        {
            // Given how we want other tabs nested under ours, we're using a 
            // Foldout to serve as the main button for this tab.
            _button = Root.Q<Foldout>();
        }

        public override void Register(IControlPanelTab subtab)
        {
            base.Register(subtab);
            _button.Add(subtab.Root);
        }
    }

    //[MainTab("Editor/UIToolkitTemplates/HyphlowTab")]
    //public class HyphlowTab : IMainTab
    //{
    //    public string DisplayName => "Hyphlow";

    //    public IReadOnlyList<ISubtab> Subtabs { get; set; }

    //    public VisualElement CreateDefaultContent()
    //    {
    //        var ve = new VisualElement();
    //        var label = new UitkLabel("Hyphlow Settings");
    //        ve.Add(label);
    //        return ve;
    //    }
    //}

    //[Subtab(typeof(HyphlowTab), "Editor/UIToolkitTemplates/FlowchartSubtab")]
    //public class FlowchartSubtab : ISubtab
    //{
    //    public string DisplayName => "Flowchart";

    //    public VisualElement CreateContent()
    //    {
    //        var ve = new VisualElement();
    //        var label = new UitkLabel("Flowchart Subtab Content");
    //        ve.Add(label);
    //        return ve;
    //    }
    //}

    //[Subtab(typeof(HyphlowTab), "Editor/UIToolkitTemplates/GlobalVarsSubtab")]
    //public class GlobalVarsSubtab : ISubtab
    //{
    //    public string DisplayName => "Global Vars";

    //    public VisualElement CreateContent()
    //    {
    //        var ve = new VisualElement();
    //        var label = new UitkLabel("VariableSourceAssets");
    //        ve.Add(label);
    //        return ve;
    //    }
    //}

}