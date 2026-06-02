using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using AtMycelia.Graphics;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    public interface IBlockDrawerUitk
    {
        BlockButton CreateButton(IBlock block);
        void UpdateButton(BlockButton button, IBlock block, float zoom);
    }

    /// <summary>
    /// Renders Flowchart blocks as UITK buttons that size themselves to their contents.
    /// </summary>
    public sealed class BlockRenderer : VisualElement, IFlowchartWindowModule, IDisposable,
        IFlowchartChangeResponder, IWindowPanResponder, IScrollWheelMoveResponder,
        IBlockCreatedResponder,
        IBlockSelectionResponder, IPreBlockDeletionResponder,
        IPostBlockDeletionResponder, IPostMultiBlockDeletionResponder,
        ILeftMouseDragStartResponder, ILeftMouseDragResponder,
        ILeftMouseDragEndResponder, IBlockDeselectionResponder, IMultiBlockSelectionResponder,
        IMultiBlockDeselectionResponder, IBlockRectProvider,
        IPostBlockCutResponder, IPostMultiBlockCutResponder, IVisualResetter
    {
        public int Priority { get; set; } = 0;
        private readonly Dictionary<IBlock, BlockBinding> _blockBindings = new();
        private FlowchartWindow _owner;
        private bool _isDisposed;
        private bool _initialRefreshPending;

        /// <summary>
        /// Binds a block to its visual representation and event handlers.
        /// </summary>
        private sealed class BlockBinding
        {
            public BlockButton Button;
            public Action ClickHandler;
        }
        
        public BlockRenderer(FlowchartContext context, IBlockDrawerUitk blockDrawer)
        {
            _fcContext = context ?? throw new ArgumentNullException(nameof(context));
            _drawer = blockDrawer ?? throw new ArgumentNullException(nameof(blockDrawer));

            style.position = Position.Absolute;
            style.flexGrow = 1f;

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void ToggleSubs(bool on)
        {
            if (on)
            {
                Undo.undoRedoPerformed += OnUndoRedoPerformedFirst;
            }
            else
            {
                Undo.undoRedoPerformed -= OnUndoRedoPerformedFirst;
            }
        }

        private void OnUndoRedoPerformedFirst()
        {
            ClearAll(); // Helps prevent some buttons from sticking around when they shouldn't.
            RefreshBlocks();
        }

        private readonly FlowchartContext _fcContext;
        private readonly IBlockDrawerUitk _drawer;

        public void Initialize(FlowchartWindow window)
        {
            _owner = window;
            _initialRefreshPending = true;
            ToggleSubs(false);
            ToggleSubs(true);
            TryRefreshAfterLayout();
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            TryRefreshAfterLayout();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_initialRefreshPending)
            {
                return;
            }

            if (evt.newRect.width <= 0f || evt.newRect.height <= 0f)
            {
                return;
            }

            TryRefreshAfterLayout();
        }

        private void TryRefreshAfterLayout()
        {
            if (!_initialRefreshPending)
            {
                return;
            }

            if (panel == null)
            {
                return;
            }

            if (contentRect.width <= 0f || contentRect.height <= 0f)
            {
                return;
            }

            _initialRefreshPending = false;
            RefreshBlocks();
        }

        public void RefreshBlocks()
        {
            if (_isDisposed)
            {
                return;
            }

            Flowchart flowchart = _fcContext.Flowchart;
            if (flowchart == null)
            {
                ClearAll();
                return;
            }

            IReadOnlyCollection<IBlock> present = _fcContext.Document.AllBlocks;
            RemoveMissing(present);

            foreach (var block in present)
            {
                EnsureBlockVisual(block);
            }

            UpdateBlockLayouts();
            FlowchartWindowSignals.WindowPanned();
            MarkDirtyRepaint();
        }

        private void RemoveMissing(IReadOnlyCollection<IBlock> currentBlocks)
        {
            using ListPool<IBlock>.DisposableList pooledKeysHandle = ListPool<IBlock>.Get(out List<IBlock> pooledKeys);
            pooledKeys.AddRange(_blockBindings.Keys);
            for (int i = 0; i < pooledKeys.Count; i++)
            {
                IBlock tracked = pooledKeys[i];
                if (!ContainsBlock(currentBlocks, tracked))
                {
                    RemoveBlock(tracked);
                }
            }
        }

        private static bool ContainsBlock(IReadOnlyCollection<IBlock> blocks, IBlock target)
        {
            if (blocks == null)
            {
                return false;
            }

            if (blocks is ICollection<IBlock> collection)
            {
                return collection.Contains(target);
            }

            foreach (var block in blocks)
            {
                if (ReferenceEquals(block, target))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveBlock(IBlock block)
        {
            if (!_blockBindings.TryGetValue(block, out BlockBinding binding))
            {
                return;
            }

            BlockButton buttonToRemove = binding.Button;
            if (buttonToRemove != null)
            {
                UnregisterInputForwarders(buttonToRemove);

                if (binding.ClickHandler != null)
                {
                    buttonToRemove.Clicked -= binding.ClickHandler;
                }

                buttonToRemove.Dispose();
            }

            _blockBindings.Remove(block);
            MarkDirtyRepaint();
        }

        private void EnsureBlockVisual(IBlock block)
        {
            if (block == null)
            {
                return;
            }

            bool blockAlreadyDrawn = _blockBindings.TryGetValue(block, out BlockBinding binding);
            if (!blockAlreadyDrawn)
            {
                BlockButton button = _drawer.CreateButton(block);
                button.name = block.BlockName;
                button.style.position = Position.Absolute;

                RegisterInputForwarders(button);

                var capturedBlock = block;
                button.Clicked += OnClick;
                void OnClick()
                {
                    BlockSignals.BlockLeftClicked?.Invoke(capturedBlock, Event.current);
                }
                
                void OnButtonGeometryChanged(GeometryChangedEvent evt)
                {
                    if (evt.newRect.width <= 0f || evt.newRect.height <= 0f)
                    {
                        return;
                    }
                    // We do this (calling UpdateButton on the first geometry change) so that right when the
                    // window opens, the button is rendered at the right size. For some reason, putting
                    // RefreshBlocks in Initialize doesn't work...
                    button.UnregisterCallback<GeometryChangedEvent>(OnButtonGeometryChanged);
                    _drawer.UpdateButton(button, capturedBlock, CurrentZoom);
                    UpdateBlockLayouts();
                }
                button.RegisterCallback<GeometryChangedEvent>(OnButtonGeometryChanged);

                ScheduleInitialRefresh(button, capturedBlock);

                binding = new BlockBinding
                {
                    Button = button,
                    ClickHandler = OnClick
                };
                
                _blockBindings.Add(block, binding);
                Add(button);
            }

            _drawer.UpdateButton(binding.Button, block, CurrentZoom);
        }

        private void ScheduleInitialRefresh(BlockButton button, IBlock block)
        {
            if (button == null || block == null)
            {
                return;
            }

            button.schedule.Execute(() =>
            {
                if (button.panel == null)
                {
                    return;
                }

                if (!_blockBindings.TryGetValue(block, out BlockBinding binding) || binding.Button != button)
                {
                    return;
                }

                _drawer.UpdateButton(button, block, CurrentZoom);
                UpdateBlockLayouts();
            }).ExecuteLater(1);
        }

        /// <summary>
        /// Based on the current scroll and zoom, update the positions and sizes of all block buttons.
        /// </summary>
        private void UpdateBlockLayouts()
        {
            Vector2 scroll = CurrentScroll;
            float zoom = CurrentZoom;

            foreach (var pair in _blockBindings)
            {
                IBlock block = pair.Key;
                BlockButton button = pair.Value.Button;
                if (block == null || button == null)
                {
                    continue;
                }

                Rect rect = block._NodeRect;
                Vector2 viewPos = (rect.position + scroll) * zoom;

                button.style.left = viewPos.x;
                button.style.top = viewPos.y;

                _drawer.UpdateButton(button, block, zoom);
            }
        }

        private Vector2 CurrentScroll
        {
            get
            {
                Flowchart flowchart = _fcContext.Flowchart;
                return flowchart != null ? flowchart.ScrollPos : Vector2.zero;
            }
        }

        private float CurrentZoom
        {
            get
            {
                Flowchart flowchart = _fcContext.Flowchart;
                float zoom = flowchart != null ? flowchart.Zoom : 1f;
                return Mathf.Approximately(zoom, 0f) ? 1f : zoom;
            }
        }

        #region Callbacks
        public void OnFlowchartChanged(Flowchart previous, Flowchart next)
        {
            ClearAll();
            _initialRefreshPending = true;
            TryRefreshAfterLayout();
            SchedulePostLayoutRefresh();
        }

        public void OnWindowPanned()
        {
            UpdateBlockLayouts();
        }

        public void OnScrollWheelMoved()
        {
            UpdateBlockLayouts();
        }

        public void OnMultiBlocksSelected(IList<IBlock> blocks)
        {
            UpdateButtonForMultiBlocks(blocks);
        }

        private void UpdateButtonForMultiBlocks(IList<IBlock> blocks)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                UpdateButtonForBlock(blocks[i]);
            }
        }

        private void UpdateButtonForBlock(IBlock block)
        {
            // It's possible that this is being called in response to a block from another
            // Flowchart being deselected due to a Flowchart change. In that case, we won't
            // have a binding for this block, and that's fine - we just won't update any button.
            if (block == null)
            {
                return;
            }
            if (_blockBindings.TryGetValue(block, out BlockBinding binding))
            {
                _drawer.UpdateButton(binding.Button, block, CurrentZoom);
            }
            else
            {
                // We probably just created this block, so ensure it has a visual.
                EnsureBlockVisual(block);
            }
        }

        public void OnBlockDeselected(IBlock block)
        {
            UpdateButtonForBlock(block);
        }

        public void OnMultiBlocksDeselected(IList<IBlock> blocks)
        {
            UpdateButtonForMultiBlocks(blocks);
        }

        #endregion

        public void OnBlockSelected(IBlock block)
        {
            UpdateButtonForBlock(block);
        }

        private void ClearAll()
        {
            foreach (var entry in _blockBindings)
            {
                UnregisterInputForwarders(entry.Value.Button);
                UnsubClickHandler(entry.Value);
                entry.Value.Button?.Dispose();
            }
            _blockBindings.Clear();
        }

        private void UnsubClickHandler(BlockBinding binding)
        {
            if (binding.Button != null && binding.ClickHandler != null)
            {
                binding.Button.Clicked -= binding.ClickHandler;
            }
        }

        public void OnPreBlockDeletion(IList<IBlock> blocks)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                var blockEl = blocks[i];
                RemoveBlock(blockEl);
            }
        }

        public void OnPreBlockDeletion(IBlock block)
        {
            RemoveBlock(block);
        }

        public void OnLeftMouseDragStarted(PointerEventInfo info, Event evt)
        {
            #region Keep Blocks from blocking drag events
            foreach (var entry in _blockBindings)
            {
                var button = entry.Value.Button;
                if (button != null)
                {
                    button.SetPickingMode(PickingMode.Ignore);
                }
            }
            #endregion
        }

        public void OnLeftMouseDragEnded(PointerEventInfo info, Event evt)
        {
            #region Let Blocks be selectable again
            foreach (var entry in _blockBindings)
            {
                var button = entry.Value.Button;
                if (button != null)
                {
                    button.SetPickingMode(PickingMode.Position);
                }
            }
            #endregion
        }

        public bool TryGetBlockRect(IBlock block, out Rect rect)
        {
            rect = default;
            if (block == null)
            {
                return false;
            }

            if (!_blockBindings.TryGetValue(block, out BlockBinding binding) || binding.Button == null)
            {
                return false;
            }

            VisualElement parentEl = parent;
            Rect worldRect = binding.Button.worldBound;
            if (IsInvalidRect(worldRect))
            {
                return false;
            }

            if (parentEl == null)
            {
                rect = worldRect;
                return true;
            }

            Vector2 localPos = parentEl.WorldToLocal(worldRect.position);
            rect = new Rect(localPos, worldRect.size);
            return true;
        }

        private static bool IsInvalidRect(Rect rect)
        {
            return IsInvalidNumber(rect.x) ||
                   IsInvalidNumber(rect.y) ||
                   IsInvalidNumber(rect.width) ||
                   IsInvalidNumber(rect.height) ||
                   rect.width <= 0f ||
                   rect.height <= 0f;
        }

        private static bool IsInvalidNumber(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            ToggleSubs(false);
            _isDisposed = true;
            ClearAll();
            UnregisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RemoveFromHierarchy();
        }

        private InputSignalModule InputSignals => _owner != null ? _owner.InputSignals : null;

        private void RegisterInputForwarders(BlockButton button)
        {
            VisualElement inputTarget = button != null ? button.InputTarget : null;
            if (inputTarget == null)
            {
                return;
            }

            inputTarget.RegisterCallback<PointerDownEvent>(OnBlockPointerDown);
            inputTarget.RegisterCallback<PointerMoveEvent>(OnBlockPointerMove);
            inputTarget.RegisterCallback<PointerUpEvent>(OnBlockPointerUp);
            inputTarget.RegisterCallback<PointerCancelEvent>(OnBlockPointerCancel);
        }

        private void UnregisterInputForwarders(BlockButton button)
        {
            VisualElement inputTarget = button != null ? 
                button.InputTarget : 
                null;
            if (inputTarget == null)
            {
                return;
            }

            inputTarget.UnregisterCallback<PointerDownEvent>(OnBlockPointerDown);
            inputTarget.UnregisterCallback<PointerMoveEvent>(OnBlockPointerMove);
            inputTarget.UnregisterCallback<PointerUpEvent>(OnBlockPointerUp);
            inputTarget.UnregisterCallback<PointerCancelEvent>(OnBlockPointerCancel);
        }

        private void OnBlockPointerDown(PointerDownEvent evt)
        {
            InputSignals?.OnPointerDown(evt);
        }

        private void OnBlockPointerMove(PointerMoveEvent evt)
        {
            InputSignals?.OnPointerMove(evt);
        }

        private void OnBlockPointerUp(PointerUpEvent evt)
        {
            //Debug.Log("BlockRendererUitk received pointer up event, forwarding to InputSignals.");
            //InputSignals?.OnPointerUp(evt);
        }

        private void OnBlockPointerCancel(PointerCancelEvent evt)
        {
            // No op
        }

        public void OnLeftMouseDragged(PointerEventInfo info, Event evt)
        {
            if (_fcContext.Interaction.BlockDragOngoing)
            {
                UpdateBlockLayouts();
            }
        }

        public void OnBlockCreated(IBlock block)
        {
            UpdateButtonForBlock(block);
        }

        public void OnPostBlockDeletion(byte blockId)
        {
            // Why do this in post? It's because by the time that the pre signal fires, the
            // block(s) are still registered in the Flowchart. That leads to the
            // should've-been-deleted blocks still being drawn in RefreshBlocks, which causes
            // weird visual bugs. By waiting until post, we ensure that the blocks are fully
            // deleted from the Flowchart before we try to refresh our visuals.
            ClearAll();
            RefreshBlocks();
        }

        public void OnPostMultiBlockDeletion(IList<byte> blockIds)
        {
            ClearAll();
            RefreshBlocks();
        }

        public void OnPostBlockCut(byte blockId)
        {
            OnPostBlockDeletion(blockId);
        }

        public void OnPostMultiBlockCut(IList<byte> blockIds)
        {
            OnPostMultiBlockDeletion(blockIds);
        }

        public void ResetVisuals()
        {
            if (_isDisposed)
            {
                return;
            }

            ClearAll();
            _initialRefreshPending = true;

            if (panel != null && contentRect.width > 0f && contentRect.height > 0f)
            {
                _initialRefreshPending = false;
                RefreshBlocks();
                SchedulePostLayoutRefresh();
                return;
            }

            schedule.Execute(TryRefreshAfterLayout).ExecuteLater(1);
            SchedulePostLayoutRefresh();
        }

        private void SchedulePostLayoutRefresh()
        {
            schedule.Execute(() =>
            {
                if (_isDisposed)
                {
                    return;
                }

                RefreshBlocks();
            }).ExecuteLater(1);
        }
    }

}