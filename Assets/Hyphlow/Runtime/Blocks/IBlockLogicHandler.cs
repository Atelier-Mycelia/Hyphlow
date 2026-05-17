using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    public interface IBlockLogicHandler : IBlockExecutor, IRefreshable
    {
        MonoBehaviour Owner { get; set; }
        bool ExecuteIfHasBlock(string blockName);
    }

    public interface IBlockExecutor
    {
        void ExecuteBlock(string blockName);
        bool ExecuteBlock(IBlock block, int commandIndex = 0, Action onComplete = null);
        void StopBlock(string blockName);
        void StopAllBlocks();
        bool HasExecutingBlocks();
        IReadOnlyList<IBlock> GetExecutingBlocks();
    }

    public interface IBlockLookup
    {
        bool Contains(Block block);
        Block GetBlock(ushort id);
        Block GetBlock(string blockName);
    }

#if UNITY_EDITOR
    public interface IBlockCreator
    {
        Block CreateBlock(Vector2 position, string blockName = null);
        IList<IBlock> CreateMultiBlocks(IList<Vector2> positions);
    }
#endif
}