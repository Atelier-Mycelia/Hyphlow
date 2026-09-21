using System;
using System.Collections.Generic;
using System.Linq;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Centralized class for handling clipboard operations related to Hyphlow 
    /// (such as Flowchart blocks and commands).
    /// </summary>
    public class HyphlowClipboard : IDisposable
    {
        // If we ever find ourselves needing to track more types of copied data
        // (e.g. variables, comments, etc.) we can expand this class to include
        // additional clipboards or a more generic clipboard system.

        public HyphlowClipboard()
            : this(new BlockClipboard(null), new CommandClipboard())
        {
        }

        public HyphlowClipboard(BlockClipboard blockClipboard, CommandClipboard commandClipboard)
        {
            BlockClipboard = blockClipboard;
            CommandClipboard = commandClipboard;
        }

        public HyphlowClipboard(IFlowchartHostCore host)
            : this(new BlockClipboard(host), new CommandClipboard())
        {
        }

        public BlockClipboard BlockClipboard { get; private set; }
        public CommandClipboard CommandClipboard { get; private set; }

        public bool HasBlockEntries => BlockClipboard != null && BlockClipboard.HasEntries;
        public bool HasCommandEntries => CommandClipboard != null && CommandClipboard.HasCommands();

        public virtual void CopyBlocks(FlowchartContext context, bool doSignal = true)
        {
            if (context == null || BlockClipboard == null)
            {
                return;
            }

            IList<IBlock> selectedBlocks = context.Selection.Blocks;
            if (selectedBlocks == null || selectedBlocks.Count == 0)
            {
                return;
            }

            BlockClipboard.Copy(selectedBlocks);
            if (doSignal)
            {
                BlockSignals.BlocksCopied(selectedBlocks);
            }
        }

        public virtual void CutBlocks(FlowchartContext context, bool doSignal = true)
        {
            if (context == null)
            {
                return;
            }
            var selected = context.Selection.Blocks;
            IList<byte> blockIds = null;

            #region Pre-Signals
            if (doSignal)
            {
                blockIds = new List<byte>();
                for (int i = 0; i < selected.Count; i++)
                {
                    blockIds.Add(selected[i].ItemId);
                }

                if (selected.Count == 1)
                {
                    BlockSignals.PreBlockCut(selected[0]);
                }
                else
                {
                    BlockSignals.PreMultiBlockCut(selected);
                }
            }
            #endregion

            // Not signaling the copying and deletion so that client code can have
            // an easier time differentiating the timings of cpoying, cutting and deleting.
            BlockClipboard.Copy(selected, true);
            DeleteBlocks(context, false);

            #region Post-Signals
            if (doSignal)
            {
                if (selected.Count == 1)
                {
                    BlockSignals.PostBlockCut(blockIds[0]);
                }
                else
                {
                    BlockSignals.PostMultiBlockCut(blockIds);
                }
            }
            #endregion
        }

        public virtual void DeleteBlocks(FlowchartContext context, bool doSignal = true)
        {
            if (context == null || context.FcHost == null)
            {
                return;
            }

            var selection = context.Selection;
            int blockCount = selection.Blocks?.Count ?? 0;
            if (blockCount == 0)
            {
                return;
            }

            #region Gather up Block IDs for post-deletion signals
            IList<byte> blockIDs = null;
            #endregion

            #region Pre-Delete Broadcasts
            if (doSignal)
            {
                blockIDs = selection.Blocks.Select(elem => elem.ItemId).ToList();
                if (blockCount == 1)
                {
                    BlockSignals.PreBlockDelete(selection.Blocks[0]);
                }
                else
                {
                    BlockSignals.PreMultiBlockDelete(selection.Blocks);
                }
            }
            #endregion

            _blockDeletion.Execute(context);

            #region Post-Delete Broadcasts
            if (doSignal)
            {
                if (blockCount == 1)
                {
                    BlockSignals.PostBlockDelete(blockIDs[0]);
                }
                else
                {
                    BlockSignals.PostMultiBlockDelete(blockIDs);
                }
            }
            #endregion
        }

        private readonly FcWindowBlockDeletion _blockDeletion = new FcWindowBlockDeletion();

        public virtual void CopySelectedCommands(Flowchart flowchart)
        {
            if (CommandClipboard == null)
            {
                return;
            }

            CommandClipboard.CopySelectedCommands(flowchart);
        }

        public virtual void CutSelectedCommands(Flowchart flowchart)
        {
            if (CommandClipboard == null)
            {
                return;
            }

            CommandClipboard.CutSelectedCommands(flowchart);
        }

        public void Dispose()
        {
            BlockClipboard?.Dispose();
            BlockClipboard = null;
            CommandClipboard = null;
        }
    }
}