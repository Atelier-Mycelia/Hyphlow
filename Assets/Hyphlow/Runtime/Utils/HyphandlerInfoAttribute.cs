using System;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Puts a <see cref="Hyphandler"/> in the event handler selector, the way
    /// <see cref="EventHandlerInfoAttribute"/> does for the legacy MonoBehaviour handlers.
    /// Kept separate from that attribute so tools that list legacy handlers (and add them as
    /// components) don't pick these up by accident.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class HyphandlerInfoAttribute : Attribute
    {
        public HyphandlerInfoAttribute(string category, string handlerName, string helpText)
        {
            Category = category;
            HandlerName = handlerName;
            HelpText = helpText;
        }

        /// <summary>
        /// Submenu the handler is listed under in the selector. Empty for the top level.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Shown in the selector and above the Block in the Flowchart Window. Keep the Fungus
        /// name when porting a handler, so users can find it.
        /// </summary>
        public string HandlerName { get; set; }

        public string HelpText { get; set; }
    }
}
