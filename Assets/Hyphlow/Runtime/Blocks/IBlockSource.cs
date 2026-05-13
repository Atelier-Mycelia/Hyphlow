using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    public interface IBlockSource : IHasName
    {
        IReadOnlyList<Block> Blocks { get; }

        bool Contains(Block block);
        Block GetBlockWithId(ushort id);
        void Add(Block block);
        void Remove(Block block);
    }
}