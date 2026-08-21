using AtMycelia.Myceliarium;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.ControlPanel
{
    public class HyphlowEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string MainDisplayName => "Hyphlow";
        public override bool IsTopLevel => true;
        public override bool IsMeantToHaveSubwindow => false;

        // TODO: Implement a working state that basically gathers up 
        // those of the sub-entries.
        public override string StringifiedState => throw new System.NotImplementedException();

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

            // At this time, the left sidebar tab should have been prepped.
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

        protected override void RegisterMainClickable()
        {
            // Given how we want other tabs nested under ours, we're using a 
            // Foldout to serve as the main button for this tab.
            _mainClickable = Root.Q<Foldout>();
            if (_mainClickable == null)
            {
                string logMessage = $"Failed to find a Foldout in the tab UXML " +
                    $"at {PathToUxml} for {GetType().Name}.";
                throw new System.InvalidOperationException(logMessage);
            }
        }

        public override void Register(IControlPanelTab subtab)
        {
            base.Register(subtab);
            _mainClickable.Add(subtab.Root);
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