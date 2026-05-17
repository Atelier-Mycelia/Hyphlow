using UnityEngine;
using System.Collections.Generic;
using System;

namespace AtMycelia.Hyphlow.EditorUtils
{
    public class FlowchartDocument : IDisposable
    {
        private static readonly IBlock[] EmptyBlocks = Array.Empty<IBlock>();

        public Flowchart Flowchart
        {
            get { return _flowchart; }
            set { _flowchart = value; }
        }

        private Flowchart _flowchart;

        public IReadOnlyCollection<IBlock> AllBlocks
        {
            get
            {
                if (Flowchart == null)
                {
                    return EmptyBlocks;
                }

                return Flowchart.Blocks;
            }
        }

        public IBlock TopmostBlockOverlapping(Vector2 mousePosition)
        {
            if (Flowchart == null)
            {
                return null;
            }

            IList<IBlock> blocks = GetOrderedBlocksSnapshot();
            if (blocks.Count == 0)
            {
                return null;
            }

            Vector2 mousePosInWindowSpace = ToWindowSpace(mousePosition);

            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                var currentBlock = blocks[i];
                if (currentBlock == null)
                {
                    continue;
                }

                Rect windowSpaceRect = currentBlock._NodeRect;
                windowSpaceRect.position += Flowchart.ScrollPos;

                if (windowSpaceRect.Contains(mousePosInWindowSpace))
                {
                    return currentBlock;
                }
            }

            return null;
        }

        private IList<IBlock> GetOrderedBlocksSnapshot()
        {
            if (Flowchart == null)
            {
                return EmptyBlocks;
            }

            IReadOnlyCollection<IBlock> blocks = AllBlocks;
            if (blocks == null)
            {
                return EmptyBlocks;
            }

            if (blocks.Count == 0)
            {
                return Flowchart.GetComponents<IBlock>();
            }

            if (blocks is IList<IBlock> list)
            {
                return list;
            }

            if (blocks is IReadOnlyList<IBlock> readOnlyList)
            {
                return CopyReadOnlyList(readOnlyList);
            }

            return new List<IBlock>(blocks);
        }

        private static IList<IBlock> CopyReadOnlyList(IReadOnlyList<IBlock> source)
        {
            List<IBlock> copy = new List<IBlock>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                copy.Add(source[i]);
            }

            return copy;
        }

        public Vector2 ToWindowSpace(Vector2 mousePosition)
        {
            if (Flowchart == null)
            {
                return mousePosition;
            }

            float zoom = Mathf.Approximately(Flowchart.Zoom, 0f) ? 1f : Flowchart.Zoom;
            return mousePosition / zoom;
        }

        public virtual void Dispose()
        {
            Flowchart = null;
        }
    }
}