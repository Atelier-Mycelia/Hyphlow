using System;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    /// <summary>
    /// Handles single-click-driven block selection and empty space deselection.
    /// </summary>
    public sealed class SingleSelectionHandler : IFlowchartWindowModule, 
        IEmptySpaceLeftClickResponder, IBlockClickResponder, IBlockCreatedResponder
    {
        public int Priority { get; set; } = 0;
        public SingleSelectionHandler(FlowchartContext context)
        {
            _flowchartContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        private readonly FlowchartContext _flowchartContext;
        
        public void Initialize(FlowchartWindow window)
        {
            _isDisposed = false;
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }
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

        public void OnBlockClicked(IBlock block, Event _)
        {
            if (!_isDisposed)
            {
                if (_.shift || _.control || _.command)
                {
                    return; // Let multi-selection handler deal with it.
                }
                SetFlowchartAsSelecting(block);
            }
        }

        private void SetFlowchartAsSelecting(IBlock block)
        {
            if (Flowchart == null)
            {
                return;
            }

            bool validBlock = block != null; // We assume that the block belongs to the flowchart.
            if (!validBlock) // Probably empty space clicked.
            {
                Flowchart.DeselectAll();
                return;
            }

            if (!block.IsSelected) // Single-clicking one block should deselect all other blocks and commands.
            {
                Flowchart.ClearSelectedCommands();
                Flowchart.ClearSelectedBlocks();
            }
            
            Flowchart.SelectedBlock = block;
            Flowchart.AddToSelection(block);
        }

        private Flowchart Flowchart => _flowchartContext.Flowchart;

        public void OnBlockCreated(IBlock block)
        {
            if (!_isDisposed)
            {
                SetFlowchartAsSelecting(block);
            }
        }

        public void OnEmptySpaceLeftClicked(PointerEventInfo info)
        {
            if (_isDisposed || Flowchart == null)
            {
                return;
            }

            SetFlowchartAsSelecting(null);

            if (Selection.activeGameObject != Flowchart.gameObject)
            {
                Selection.activeGameObject = Flowchart.gameObject;
            }
        }

    }
}