using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Runtime-agnostic contract for objects that contain Blocks.
    /// </summary>
    public interface IHasBlocks
    {
        IReadOnlyList<IBlock> Blocks { get; }
        bool Contains(IBlock block);
        IBlock GetBlock(string name);
        IBlock GetBlock(byte id);
    }

    /// <summary>
    /// Runtime-agnostic contract for objects that: <br></br>
    /// - contain Blocks <br></br>
    /// - have a name <br></br>
    /// - have Commands <br></br>
    /// - Let you add or remove Blocks, optionally triggering signals/events.
    /// </summary>
    public interface IBlockSource : IHasBlocks, IHasName, IHasCommands, ICommandRemovable
    {
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
        bool RemoveBlockWithId(byte id, bool triggerSignals);
        bool ClearBlocks(bool triggerSignals);
    }
}