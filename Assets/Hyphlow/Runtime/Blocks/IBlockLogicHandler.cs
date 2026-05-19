using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    public interface IBlockExecutor
    {
        /// <summary>
        /// This is used to execute logic that may take more than one frame.
        /// </summary>
        MonoBehaviour CoroutineRunner { get; set; }

        void ExecuteBlock(byte blockId);
        void ExecuteBlock(string blockName);
        bool ExecuteBlock(IBlock block, int commandIndex = 0, Action onComplete = null);

        void StopBlock(byte blockId);
        void StopBlock(string blockName);

        void StopAllBlocks();
        bool HasExecutingBlocks();
        IReadOnlyList<IBlock> ExecutingBlocks { get; }
    }

    public interface IBlockLookup
    {
        bool Contains(Block block);
        IBlock GetBlock(ushort id);
        IBlock GetBlock(string blockName);
    }

#if UNITY_EDITOR
    public interface IBlockCreator
    {
        IBlock CreateBlock(Vector2 position, string blockName = null, bool triggerSignals = true);
        IList<IBlock> CreateMultiBlocks(IList<Vector2> positions);
    }
#endif
}