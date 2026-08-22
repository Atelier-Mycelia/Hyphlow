using System;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    /// <summary>
    /// Interface for modules that can be added to the flowchart window. Modules can 
    /// respond to various events and provide additional functionality.
    /// </summary>
    public interface IFlowchartWindowModule : IDisposable
    {
        /// <summary>
        /// Lower number, sooner execution; Modules are executed in ascending order of this value.
        /// </summary>
        int Priority { get; set; }
        void Initialize(FlowchartWindow window);
    }
}