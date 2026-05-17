using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    public interface IBlockSource : IHasName
    {
        IReadOnlyList<IBlock> Blocks { get; }

        bool Contains(IBlock block);
        IBlock GetBlock(string name);
        IBlock GetBlock(ushort id);

        /// <summary>
        /// Returns true if the Block was successfully added, false otherwise.
        /// </summary>
        bool Add(IBlock block, bool triggerSignals);

        /// <summary>
        /// Returns true if the Block was successfully removed, false otherwise.
        /// </summary>
        bool Remove(IBlock block, bool triggerSignals);

        /// <summary>
        /// Returns true if the Block with the given id was successfully
        /// removed, false otherwise.
        /// </summary>
        bool RemoveBlockWithId(ushort id, bool triggerSignals);
        bool ClearBlocks(bool triggerSignals);
    }
}