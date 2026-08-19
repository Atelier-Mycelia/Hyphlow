using System;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    public class ControlPanelTab
    {
        public string Name { get; }
        public Func<VisualElement> CreateContent { get; }

        public ControlPanelTab(string name, Func<VisualElement> createContent)
        {
            Name = name;
            CreateContent = createContent;
        }
    }
}

