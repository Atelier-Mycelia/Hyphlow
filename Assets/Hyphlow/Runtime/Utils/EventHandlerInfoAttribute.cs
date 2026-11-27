using System;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Attribute class for Hyphlow event handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class EventHandlerInfoAttribute : Attribute
    {
        public EventHandlerInfoAttribute(string category, string eventHandlerName,
        string helpText, bool showInMenu = true, int order = 0, bool isTest = false)
        {
            this.Category = category;
            this.EventHandlerName = eventHandlerName;
            this.HelpText = helpText;
            this.ShowInMenu = showInMenu;
            this.Order = order;
            this.IsTest = isTest;
        }

        public string Category { get; set; }
        public string EventHandlerName { get; set; }
        public string HelpText { get; set; }
        public bool ShowInMenu { get; set; }
        public int Order { get; set; }
        public bool IsTest { get; set; }
    }
}