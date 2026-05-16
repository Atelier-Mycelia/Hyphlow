using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    public interface IBlockSource : IHasName
    {
        IReadOnlyList<Block> Blocks { get; }

        bool Contains(Block block);
        Block GetBlock(string name);
        Block GetBlock(ushort id);

        /// <summary>
        /// Returns true if the Block was successfully added, false otherwise.
        /// </summary>
        bool Add(Block block, bool triggerSignals);

        /// <summary>
        /// Returns true if the Block was successfully removed, false otherwise.
        /// </summary>
        bool Remove(Block block, bool triggerSignals);

        /// <summary>
        /// Returns true if the Block with the given id was successfully
        /// removed, false otherwise.
        /// </summary>
        bool RemoveBlockWithId(ushort id, bool triggerSignals);
        bool ClearBlocks(bool triggerSignals);
    }
}