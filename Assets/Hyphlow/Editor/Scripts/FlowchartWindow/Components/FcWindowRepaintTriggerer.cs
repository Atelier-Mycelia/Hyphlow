using System;
using System.Collections.Generic;
using UnityEditor;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    /// <summary>
    /// Handles when to get the new flowchart window to repaint.
    /// </summary>
    public sealed class FcWindowRepaintTriggerer : IFlowchartWindowModule, IFlowchartChangeResponder,
        IBlockSelectionResponder, IVariableAddResponder, IVariableRemoveResponder, 
        IPostBlockDeletionResponder
    {
        public int Priority { get; set; } = 0;
        public void Initialize(FlowchartWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            _owner = window;
            _isDisposed = false;
        }

        private FlowchartWindow _owner;
        private bool _isDisposed;

        public void OnFlowchartChanged(Flowchart previous, Flowchart next)
        {
            TriggerRepaint();
        }

        private void TriggerRepaint()
        {
            // Without this func, we'd have a lot more boilerplate in the other event responses.
            if (_isDisposed)
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (_owner != null)
                {
                    _owner.Repaint();
                }
            };
        }

        public void OnVariableAdded(Flowchart addedTo, IVariable variable)
        {
            TriggerRepaint();
        }

        public void OnVariableRemoved(Flowchart removedFrom, IVariable variable)
        {
            TriggerRepaint();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _owner = null;
        }

        public void OnPostMultiBlockDeletion(IList<short> blockIds)
        {
            TriggerRepaint();
        }

        public void OnPostBlockDeletion(byte blockId)
        {
            TriggerRepaint();
        }

        public void OnBlockSelected(IBlock block)
        {
            TriggerRepaint();
        }

        public void OnMultiBlocksSelected(IList<IBlock> blocks)
        {
            TriggerRepaint();
        }
    }
}