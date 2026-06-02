using AtMycelia.Graphics;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    /// <summary>
    /// Encapsulates all flowchart window graphics renderers (grid, blocks, selection box).
    /// </summary>
    public sealed class FcwGraphicsRenderer : VisualElement, IFlowchartWindowModule, IDisposable,
        IFlowchartChangeResponder, IScrollWheelMoveResponder, IWindowPanResponder, IBlockCreatedResponder,
        IBlockSelectionResponder, IMultiBlockSelectionResponder, IBlockDeselectionResponder, IMultiBlockDeselectionResponder,
        IPreBlockDeletionResponder, IPostBlockDeletionResponder, IPostMultiBlockDeletionResponder,
        ILeftMouseDragStartResponder, ILeftMouseDragResponder, ILeftMouseDragEndResponder,
        IPreBlockCutResponder, IPostBlockCutResponder, IPreMultiBlockCutResponder, IPostMultiBlockCutResponder,
        IVisualResetter
    {
        public int Priority { get; set; } = 0;
        public FcwGraphicsRenderer(FlowchartContext context, DrawGridContext gridDrawContext,
            IBlockDrawerUitk blockDrawer)
        {
            #region Validate Parameters
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (gridDrawContext == null)
            {
                throw new ArgumentNullException(nameof(gridDrawContext));
            }

            if (blockDrawer == null)
            {
                throw new ArgumentNullException(nameof(blockDrawer));
            }
            #endregion

            #region Create Submodules
            _gridRenderer = new GridRenderer(context, gridDrawContext);
            _blockRenderer = new BlockRenderer(context, blockDrawer);
            _selectionBoxRenderer = new SelectionBoxRenderer(context);
            var connectionDrawer = new ConnectionDrawer(new ConnectionGatherer(_blockRenderer));
            _connectionRenderer = new ConnectionRenderer(context, connectionDrawer);
            #endregion

            #region Position and Style
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.flexGrow = 1f;
            this.StretchToParentSize();
            #endregion

            #region Add visual elements
            Add(_gridRenderer);
            Add(_blockRenderer);
            Add(_connectionRenderer);
            Add(_selectionBoxRenderer);
            #endregion

            #region Register Submodules
            _submodules.Add(_gridRenderer);
            _submodules.Add(_blockRenderer);
            _submodules.Add(_selectionBoxRenderer);
            _submodules.Add(_repaintTriggerer);
            _submodules.Add(_connectionRenderer);
            #endregion
        }

        private readonly GridRenderer _gridRenderer;
        private readonly BlockRenderer _blockRenderer;
        private readonly SelectionBoxRenderer _selectionBoxRenderer;
        private readonly ConnectionRenderer _connectionRenderer;
        private readonly FcWindowRepaintTriggerer _repaintTriggerer = new FcWindowRepaintTriggerer();
        private bool _isDisposed;

        private readonly IList<IFlowchartWindowModule> _submodules = new List<IFlowchartWindowModule>();
        // ^ Cache of all submodules for easy iteration in event handlers.
        public IReadOnlyList<IFlowchartWindowModule> Submodules => (IReadOnlyList<IFlowchartWindowModule>)_submodules;
        public void Initialize(FlowchartWindow window)
        {
            _gridRenderer.Initialize(window);
            _connectionRenderer.Initialize(window);
            _blockRenderer.Initialize(window);
            _selectionBoxRenderer.Initialize(window);
        }

        public void RefreshNow()
        {
            _gridRenderer.RefreshNow();
            _blockRenderer.RefreshBlocks();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            for (int i = 0; i < _submodules.Count; i++)
            {
                _submodules[i].Dispose();
            }
            RemoveFromHierarchy();
        }

        public void OnScrollWheelMoved()
        {
            Forward<IScrollWheelMoveResponder>(r => r.OnScrollWheelMoved());
        }

        private void Forward<TResponder>(Action<TResponder> action)
            where TResponder : class
        {
            for (int i = 0; i < _submodules.Count; i++)
            {
                if (_submodules[i] is not TResponder responder)
                {
                    continue;
                }

                action(responder);
            }
        }

        public void OnWindowPanned()
        {
            Forward<IWindowPanResponder>(r => r.OnWindowPanned());
        }

        public void OnBlockSelected(IBlock block)
        {
            Forward<IBlockSelectionResponder>(r => r.OnBlockSelected(block));
        }

        public void OnMultiBlocksSelected(IList<IBlock> blocks)
        {
            Forward<IMultiBlockSelectionResponder>(r => r.OnMultiBlocksSelected(blocks));
        }

        public void OnFlowchartChanged(Flowchart previous, Flowchart next)
        {
            Forward<IFlowchartChangeResponder>(r => r.OnFlowchartChanged(previous, next));
        }

        public void OnPreBlockDeletion(IList<IBlock> blocks)
        {
            Forward<IPreBlockDeletionResponder>(r => r.OnPreBlockDeletion(blocks));
        }

        public void OnPreBlockDeletion(IBlock block)
        {
            Forward<IPreBlockDeletionResponder>(r => r.OnPreBlockDeletion(block));
        }

        public void OnPostBlockDeletion(byte blockId)
        {
            Forward<IPostBlockDeletionResponder>(r => r.OnPostBlockDeletion(blockId));
        }

        public void OnPostMultiBlockDeletion(IList<byte> blockIds)
        {
            Forward<IPostMultiBlockDeletionResponder>(r => r.OnPostMultiBlockDeletion(blockIds));
        }

        public void OnLeftMouseDragStarted(PointerEventInfo info, Event evt)
        {
            Forward<ILeftMouseDragStartResponder>(r => r.OnLeftMouseDragStarted(info, evt));
        }

        public void OnLeftMouseDragged(PointerEventInfo info, Event evt)
        {
            Forward<ILeftMouseDragResponder>(r => r.OnLeftMouseDragged(info, evt));
        }

        public void OnLeftMouseDragEnded(PointerEventInfo info, Event evt)
        {
            Forward<ILeftMouseDragEndResponder>(r => r.OnLeftMouseDragEnded(info, evt));
        }

        public void OnBlockDeselected(IBlock block)
        {
            Forward<IBlockDeselectionResponder>(r => r.OnBlockDeselected(block));
        }

        public void OnMultiBlocksDeselected(IList<IBlock> blocks)
        {
            Forward<IMultiBlockDeselectionResponder>(r => r.OnMultiBlocksDeselected(blocks));
        }

        public void OnBlockCreated(IBlock block)
        {
            Forward<IBlockCreatedResponder>(r => r.OnBlockCreated(block));
        }

        public void OnPreBlockCut(IBlock block)
        {
            Forward<IPreBlockCutResponder>(r => r.OnPreBlockCut(block));
        }

        public void OnPostBlockCut(byte blockId)
        {
            Forward<IPostBlockCutResponder>(r => r.OnPostBlockCut(blockId));
        }

        public void OnPreMultiBlockCut(IList<IBlock> blocks)
        {
            Forward<IPreMultiBlockCutResponder>(r => r.OnPreMultiBlockCut(blocks));
        }

        public void OnPostMultiBlockCut(IList<byte> blockIds)
        {
            Forward<IPostMultiBlockCutResponder>(r => r.OnPostMultiBlockCut(blockIds));
        }

        public void ResetVisuals()
        {
            if (_isDisposed)
            {
                return;
            }

            for (int i = 0; i < _submodules.Count; i++)
            {
                if (_submodules[i] is IVisualResetter resetter)
                {
                    resetter.ResetVisuals();
                }
            }
        }
    }
}