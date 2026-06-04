using AtMycelia.Graphics;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    /// <summary>
    /// UITK-based connection renderer that draws using Painter2D.
    /// </summary>
    public sealed class ConnectionRenderer : VisualElement, IFlowchartWindowModule, IDisposable,
        IFlowchartChangeResponder, IWindowPanResponder, IScrollWheelMoveResponder,
        ILeftMouseDragStartResponder, ILeftMouseDragResponder, ILeftMouseDragEndResponder,
        IBlockSelectionResponder, IBlockDeselectionResponder, IMultiBlockSelectionResponder,
        IMultiBlockDeselectionResponder, IPreBlockDeletionResponder, IPostBlockDeletionResponder,
        IPostMultiBlockDeletionResponder, IBlockCreatedResponder, IBlocksCopiedResponder,
        ICommandSelectionResponder, IVisualResetter
    {
        public int Priority { get; set; } = 0;
        private const float DefaultBlockHeight = 40f;
        private const float BlockMinWidth = 60f;
        private const float BlockMaxWidth = 260f;

        private const bool DiagnosticsEnabled = true;
        private int _diagnosticsRemaining = 6;

        private readonly FlowchartContext _flowchartContext;
        private readonly DrawBlockContext _drawBlockContext = new DrawBlockContext();
        private readonly ConnectionDrawer _connectionDrawer;

        private FlowchartWindow _owner;
        private bool _isDisposed;

        public ConnectionRenderer(FlowchartContext context, ConnectionDrawer connectionDrawer)
        {
            _flowchartContext = context ?? throw new ArgumentNullException(nameof(context));
            this._connectionDrawer = connectionDrawer ?? throw new ArgumentNullException(nameof(connectionDrawer));

            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.flexGrow = 1f;
            this.contentContainer.StretchToParentSize();
        }

        public void Initialize(FlowchartWindow window)
        {
            _owner = window != null ? 
                window : 
                throw new ArgumentNullException(nameof(window));
            ToggleSubs(true);
        }

        void ToggleSubs(bool on)
        {
            if (on)
            {
                Undo.undoRedoPerformed += OnUndoRedoPerformed;
                generateVisualContent += OnGenerateVisualContent;
                RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
                RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
            else
            {
                Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                generateVisualContent -= OnGenerateVisualContent;
                UnregisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
                UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }

        private void OnUndoRedoPerformed()
        {
            RequestRepaint();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            ToggleSubs(false);
            _isDisposed = true;

            
            generateVisualContent -= OnGenerateVisualContent;

            _connectionDrawer.Dispose();
            _drawBlockContext.Dispose();
            RemoveFromHierarchy();
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            LogDiagnostics("AttachToPanel");
            RequestRepaint();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            LogDiagnostics($"GeometryChanged newRect={evt.newRect}");
            RequestRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_isDisposed)
            {
                return;
            }

            Flowchart flowchart = _flowchartContext.Flowchart;
            if (flowchart == null)
            {
                return;
            }

            UpdateDrawContext();
            LogDiagnostics($"GenerateVisualContent contentRect={contentRect} viewRect={_drawBlockContext.ViewRect}");
            _connectionDrawer.Draw(mgc.painter2D, _drawBlockContext, _flowchartContext);
        }

        private void LogDiagnostics(string message)
        {
            if (!DiagnosticsEnabled || _diagnosticsRemaining <= 0)
            {
                return;
            }

            _diagnosticsRemaining--;
            //Debug.Log($"[ConnectionRenderer] {message} frame={Time.frameCount}");
        }

        private void UpdateDrawContext()
        {
            if (_owner != null)
            {
                _flowchartContext.Position = _owner.position;
            }

            _drawBlockContext.FlowchartCtx = _flowchartContext;
            _drawBlockContext.DefaultBlockHeight = DefaultBlockHeight;
            _drawBlockContext.BlockMinWidth = BlockMinWidth;
            _drawBlockContext.BlockMaxWidth = BlockMaxWidth;

            Rect viewRectSource = contentRect;
            if (viewRectSource.width <= 0f || viewRectSource.height <= 0f)
            {
                viewRectSource = _flowchartContext.Position;
            }

            _drawBlockContext.ViewRect = new Rect(0f, 0f, viewRectSource.width, viewRectSource.height);
        }

        private void RequestRepaint()
        {
            if (_isDisposed)
            {
                return;
            }

            MarkDirtyRepaint();
        }

        public void ResetVisuals()
        {
            RequestRepaint();
        }

        #region Just request a repaint for all of these events
        // Since any of them could change the connections that need to be drawn.
        public void OnFlowchartChanged(Flowchart previous, Flowchart next) => RequestRepaint();
        public void OnWindowPanned() => RequestRepaint();
        public void OnScrollWheelMoved() => RequestRepaint();
        public void OnLeftMouseDragStarted(PointerEventInfo info, Event evt) => RequestRepaint();
        public void OnLeftMouseDragged(PointerEventInfo info, Event evt) => RequestRepaint();
        public void OnLeftMouseDragEnded(PointerEventInfo info, Event evt) => RequestRepaint();
        public void OnBlockSelected(IBlock block) => RequestRepaint();
        public void OnBlockDeselected(IBlock block) => RequestRepaint();
        public void OnMultiBlocksSelected(IList<IBlock> blocks) => RequestRepaint();
        public void OnMultiBlocksDeselected(IList<IBlock> blocks) => RequestRepaint();
        public void OnPreBlockDeletion(IList<IBlock> blocks) => RequestRepaint();
        public void OnPreBlockDeletion(IBlock block) => RequestRepaint();
        public void OnPostBlockDeletion(byte blockId) => RequestRepaint();
        public void OnPostMultiBlockDeletion(IList<byte> blockIds) => RequestRepaint();
        public void OnBlockCreated(IBlock block) => RequestRepaint();
        public void OnBlocksCopied(IList<IBlock> blocks) => RequestRepaint();
        public void OnCommandSelected(ICommand command) => RequestRepaint();
        #endregion
    }
}