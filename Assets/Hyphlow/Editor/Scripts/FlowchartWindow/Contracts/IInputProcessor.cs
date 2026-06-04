using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    public interface IInputProcessor
    {
        /// <summary>
        /// Process one Unity Event and return true if it was “consumed.” 
        /// </summary>
        bool Process(Event eventToProcess, FlowchartContext context);
    }
}