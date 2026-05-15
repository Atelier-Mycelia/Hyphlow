using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    public interface IBlockSource : IHasName
    {
        IReadOnlyList<Block> Blocks { get; }

        bool Contains(Block block);
        Block GetBlockWithId(ushort id);
        void Add(Block block);

        /// <summary>
        /// Returns true if the Block was successfully removed, false otherwise.
        /// </summary>
        bool Remove(Block block);

        /// <summary>
        /// Returns true if the Block with the given id was successfully
        /// removed, false otherwise.
        /// </summary>
        bool RemoveBlockWithId(ushort id);
    }
}