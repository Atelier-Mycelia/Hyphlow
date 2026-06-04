using System;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    /// <summary>
    /// Handles viewport panning in the UITK flowchart window by reacting to scroll-wheel drag deltas.
    /// </summary>
    public sealed class PanHandler : IFlowchartWindowModule, IScrollWheelDragResponder, IRightMouseDragResponder
    {
        public int Priority { get; set; } = 0;
        private FlowchartContext _flowchartContext;
        private FlowchartWindow _owner;
        private bool _isDisposed;

        public PanHandler(FlowchartContext context)
        {
            _flowchartContext = context;
        }

        public void Initialize(FlowchartWindow window)
        {
            _owner = window != null ? 
                window : 
                throw new ArgumentNullException(nameof(window));
        }

        public void OnScrollWheelDragged(Vector2 direction)
        {
            OnDragInput(direction);
        }

        private void OnDragInput(Vector2 direction)
        {
            Flowchart flowchart = _flowchartContext.Flowchart;
            if (_isDisposed || flowchart == null)
            {
                Debug.LogWarning("PanHandlerUitk is disposed or Flowchart is null.");
                return;
            }

            HandlePanning(direction);
        }

        private void HandlePanning(Vector2 direction)
        {
            if (direction.sqrMagnitude <= _minDirectionMagnitude)
            {
                Debug.Log("Direction too small.");
                return;
            }

            Flowchart flowchart = _flowchartContext.Flowchart;
            float zoom = Mathf.Approximately(flowchart.Zoom, 0f) ? 1f : flowchart.Zoom;
            Vector2 directionAdjusted = direction / zoom;

            flowchart.ScrollPos -= directionAdjusted;
            FlowchartWindowSignals.WindowPanned();
        }

        private static readonly float _minDirectionMagnitude = 0.01f;

        public void OnRightMouseDragged(PointerEventInfo info, Event evt)
        {
            if (!evt.shift)
            {
                return;
            }

            OnDragInput(info.PanelDelta);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _owner = null;
            _flowchartContext = null;
        }

        
    }
}