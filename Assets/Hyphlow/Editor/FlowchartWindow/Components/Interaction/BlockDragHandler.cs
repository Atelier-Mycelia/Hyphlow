using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    /// <summary>
    /// Keeps the click-and-drag selection box updated based on user input.
    /// </summary>
    public sealed class BlockDragHandler : IFlowchartWindowModule, ILeftMouseDownResponder, 
        ILeftMouseDragStartResponder, ILeftMouseDragResponder, ILeftMouseUpResponder
    {
        public int Priority { get; set; } = 0;
        public BlockDragHandler(FlowchartContext context)
        {
            _flowchartContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        private readonly FlowchartContext _flowchartContext;
        
        public void Initialize(FlowchartWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            _isDisposed = false;
        }

        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
        }

        public void OnLeftMouseDown(PointerEventInfo info)
        {
            if (_isDisposed)
            {
                return;
            }

            var interaction = _flowchartContext.Interaction;
            var topmostBlock = BlockHitTester.FindTopmostBlock(info.PanelPosition);
            interaction.BlockHitInLastMouseDown = topmostBlock;
        }

        public void OnLeftMouseDragStarted(PointerEventInfo info, Event evt)
        {
            if (_isDisposed || evt == null || evt.alt)
            {
                return;
            }

            var flowchart = _flowchartContext.Flowchart;
            var interaction = _flowchartContext.Interaction;

            if (flowchart == null || !interaction.WeHitBlockInLastMouseDown)
            {
                return;
            }

            Vector2 mousePosInWindowSpace = _flowchartContext.Document.ToWindowSpace(info.FlowchartPosition);
            interaction.StartDragPosition = mousePosInWindowSpace - flowchart.ScrollPos;

            IBlock blockHit = interaction.BlockHitInLastMouseDown;
            if (blockHit == null)
            {
                throw new InvalidOperationException("Hit metadata indicated a block, but none was found.");
            }

            interaction.RootBlockToDrag = blockHit;
            interaction.DragUndoRecorded = false;
            interaction.HasDraggedSelected = false;
        }

        public void OnLeftMouseDragged(PointerEventInfo info, Event evt)
        {
            if (_isDisposed || evt == null || evt.alt)
            {
                return;
            }

            var flowchart = _flowchartContext.Flowchart;
            var interaction = _flowchartContext.Interaction;

            if (flowchart == null || interaction.RootBlockToDrag == null)
            {
                return;
            }

            var selection = _flowchartContext.Selection.Blocks;
            bool atTheStartOfADrag = !interaction.DragUndoRecorded;
            if (atTheStartOfADrag)
            {
                RegisterUndoForDraggedBlocks();
                interaction.DragUndoRecorded = true;
                interaction.BlockDragOngoing = true;
            }

            void RegisterUndoForDraggedBlocks()
            {
                var undoTargets = selection.Cast<UnityObj>().ToArray();
                if (undoTargets.Length > 0)
                {
                    Undo.RegisterCompleteObjectUndo(undoTargets, "Adjust Block Position(s)");
                }
            }

            float zoom = Mathf.Approximately(flowchart.Zoom, 0f) ? 
                1f : 
                flowchart.Zoom;
            Vector2 movementDelta = info.PanelDelta / zoom;
            //Debug.Log($"Dragging blocks with movement delta {movementDelta} at zoom {flowchart.Zoom}");
            foreach (var block in selection)
            {
                if (block == null)
                {
                    continue;
                }

                Rect rect = block._NodeRect;
                rect.position += movementDelta;
                block._NodeRect = rect;
                //Debug.Log($"Moved block '{block.BlockName}'");
            }

            interaction.HasDraggedSelected = true;
        }

        public void OnLeftMouseUp(PointerEventInfo info, Event evt)
        {
            if (_isDisposed || evt == null)
            {
                return;
            }

            var interaction = _flowchartContext.Interaction;
            if (interaction.RootBlockToDrag == null)
            {
                return;
            }

            interaction.ResetDragState();
        }

    }

}