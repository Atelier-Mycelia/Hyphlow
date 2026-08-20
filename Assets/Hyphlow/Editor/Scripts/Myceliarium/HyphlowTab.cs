using AtMycelia.Myceliarium;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
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

}