using AtMycelia.ControlPanel;
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
            // non-Button to serve as the main button for this tab.
            _mainClickable = Root.Q<Foldout>();
            if (_mainClickable == null)
            {
                string logMessage = $"Failed to find a Foldout in the tab UXML " +
                    $"at {PathToUxml} for {GetType().Name}.";
                throw new System.InvalidOperationException(logMessage);
            }//
        }

        public override void Register(IControlPanelTab subtab)
        {
            base.Register(subtab);

            
            FixTemplateContainerSizingBugFor(subtab);

            _mainClickable.Add(subtab.Root);
        }

        private void FixTemplateContainerSizingBugFor(IControlPanelTab subtab)
        {
            // When parenting a TemplateContainer (or other programmatically-created VisualElement)
            // to a Foldout, the TemplateContainer's height may not be calculated correctly.
            // Thus to prevent that, we have to do a little finagling with the flex-sizing
            // and then responding to the GeometryChangedEvent.
            var tempCon = subtab.Root;
            tempCon.style.flexShrink = 0;
            tempCon.style.flexGrow = 0;
            
            VisualElement actualContent = tempCon.ElementAt(0);
            if (actualContent != null)
            {
                subtab.Root.UnregisterCallback<GeometryChangedEvent>(FixTemplateContainerHeight);
                // ^These tabs persist between ControlPanels opening and closing,
                // so just in case...

                void FixTemplateContainerHeight(GeometryChangedEvent evt)
                {
                    var childStyle = actualContent.resolvedStyle;
                    var contentHeight = childStyle.height;
                    if (contentHeight > 0)
                    {
                        tempCon.style.height = contentHeight;
                    }
                }

                subtab.Root.RegisterCallback<GeometryChangedEvent>(FixTemplateContainerHeight);
            }
        }
    }

}