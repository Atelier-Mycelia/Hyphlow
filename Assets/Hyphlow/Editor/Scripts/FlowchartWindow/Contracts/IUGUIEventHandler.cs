using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    public interface IUGUIEventHandler
    {
        /// <summary>
        /// Try to consume this Event. Returns true if it did something.
        /// </summary>
        bool Handle(Event eventToHandle, FlowchartContext ctx);
    }
}