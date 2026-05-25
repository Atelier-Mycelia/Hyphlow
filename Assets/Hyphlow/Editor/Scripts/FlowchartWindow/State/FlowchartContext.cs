using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AtMycelia.Graphics;

namespace AtMycelia.Hyphlow.EditorExt
{
    public class FlowchartContext : IDisposable
    {
        public FlowchartDocument Document { get; } = new FlowchartDocument();
        public SelectionState Selection { get; } = new SelectionState();
        public InteractionState Interaction { get; } = new InteractionState();

        public virtual void Dispose()
        {
            // 
            Interaction.Dispose();
            Document.Dispose();
            Selection.Dispose();
            ForceRepaintCount = 0;
            Position = default;
            QueuedForDeletion.Clear();
            Flowchart = null;
        }

        public int ForceRepaintCount { get; set; }

        private Flowchart _flowchart;

        public Flowchart Flowchart
        {
            get { return _flowchart; }
            set
            {
                _flowchart = value;
                Document.Flowchart = value;
                Selection.Flowchart = value;
            }
        }

        public virtual Rect Position { get; set; }
        public virtual IFlowchartHostCore FcHost { get; set; }

        public IList<IBlock> QueuedForDeletion
        {
            get { return _queuedForDeletion; }
            set
            {
                _queuedForDeletion.Clear();
                if (value == null)
                {
                    return;
                }

                foreach (var block in value)
                {
                    _queuedForDeletion.Add(block);
                }
            }
        }

        protected IList<IBlock> _queuedForDeletion = new List<IBlock>();

        public virtual void SnapBlocksToGrid()
        {
            foreach (var elem in Selection.Blocks)
            {
                Undo.RecordObject(elem as Block, "Block Position");
                elem._NodeRect = elem._NodeRect.SnapPosition(GridObjectSnap);
            }
        }

        public virtual float GridObjectSnap { get; set; } = 20;
    }
}
