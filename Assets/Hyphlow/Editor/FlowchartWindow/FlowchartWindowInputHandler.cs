using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    public class FlowchartWindowInputHandler : IInputProcessor, IDisposable
    {
        public static readonly float RightClickTolerance = 5f;
        public static readonly float MinZoomValue = 0.25f;
        public static readonly float MaxZoomValue = 1f;
        public static readonly float GridLineSpacingSize = 120;
        public static readonly float GridObjectSnap = 20;

        public FlowchartWindowInputHandler(params IUGUIEventHandler[] subhandlers)
        {
            this._subhandlers = subhandlers;
        }

        protected IList<IUGUIEventHandler> _subhandlers;

        public virtual bool Process(Event currentEv, FlowchartContext flowchartCtx)
        {
            foreach (var elem in _subhandlers)
                if (elem.Handle(currentEv, flowchartCtx))
                    return true;
            return false;

        }

        protected static readonly int _leftMouseButton = 0;
        protected FlowchartContext _currentContext;

        public virtual void Dispose()
        {
            for (var i = 0; i < _subhandlers.Count; i++)
            {
                var disposableHandler = _subhandlers[i] as IDisposable;
                disposableHandler?.Dispose();
            }
        }

    }

}