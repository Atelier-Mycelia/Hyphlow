using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    public interface IBlockLogicManager : ICommandSource, IDisposable, IHasName
    {
        /// <summary>
        /// The MonoBehaviour that owns this BlockLogicManager. This is used to 
        /// execute logic that may take more than one frame.
        /// </summary>
        MonoBehaviour Owner { get; set; }
        bool ExecuteIfHasBlock(string blockName, Action<string> executeByName);
        void ExecuteBlock(string blockName);
        void StopBlock(string blockName);
        bool ExecuteBlock(IBlock block, int commandIndex = 0, Action onComplete = null);
        void StopAllBlocks();
        bool HasExecutingBlocks();
        IList<IBlock> GetExecutingBlocks();
    }

    [Serializable]
    public sealed class BlockLogicManager : IBlockLogicManager, IDisposable
    {
        [SerializeField] [HideInInspector] private List<Command> _legacyCommands = new List<Command>();
        [SerializeField] [HideInInspector] private MonoBehaviour _owner;
        [SerializeField] private ushort _nextItemId = 1;

        public void Initialize(IBlockManager blockManager, MonoBehaviour owner)
        {
            _blockManager = blockManager;
            _blockManager.BlockOwner = owner;
            Owner = owner;
            RefreshBlockAndCommandCache();
        }

        private IBlockManager _blockManager;

        public MonoBehaviour Owner
        {
            get => _owner;
            set
            {
                _owner = value;
                RefreshBlockAndCommandCache();
            }
        }

        public string Name
        {
            get
            {
                if (_owner != null && !string.IsNullOrEmpty(_owner.name))
                {
                    return _owner.name;
                }

                return _defaultName;
            }
            set => Debug.LogWarning("BlockLogicManager.Name is read-only and cannot be set.");
        }

        private static readonly string _defaultName = "UnownedBlockLogicManager";

        public IReadOnlyList<ICommand> Commands => _legacyCommands;
        public IReadOnlyDictionary<ushort, IBlock> BlockLookup
        {
            get
            {
                // If you later expose lookup in BlockManager publicly, delegate directly.
                // For now, build from source methods on demand.
                Dictionary<ushort, IBlock> dict = new Dictionary<ushort, IBlock>();
                for (int i = 0; i < _blockManager.Blocks.Count; i++)
                {
                    var b = _blockManager.Blocks[i];
                    if (b != null)
                    {
                        dict[b.ItemId] = b;
                    }
                }

                return dict;
            }
        }

        public void RefreshBlockAndCommandCache()
        {
            _legacyCommands ??= new List<Command>();

            _blockManager = _blockManager ?? new BlockManager();
            _blockManager.BlockOwner = Owner;
            _blockManager.Refresh();

            _legacyCommands.Clear();

            if (_owner == null)
            {
                _blockManager.ClearBlocks(false);
                return;
            }

            HashSet<ushort> usedIds = new HashSet<ushort>();

            // 1) Commands first
            var commandsFound = _owner.GetComponents<Command>();
            for (int i = 0; i < commandsFound.Length; i++)
            {
                var cmd = commandsFound[i];
                if (cmd == null)
                {
                    continue;
                }

                while (cmd.ItemId == Block.InvalidId || usedIds.Contains(cmd.ItemId))
                {
                    cmd.ItemId = NextItemId();
                }

                usedIds.Add(cmd.ItemId);
                _legacyCommands.Add(cmd);
            }

            // 2) Rebuild block storage through BlockManager
            _blockManager.ClearBlocks(false);
            var blocksFound = _owner.GetComponents<Block>();
            for (int i = 0; i < blocksFound.Length; i++)
            {
                var block = blocksFound[i];
                if (block == null)
                {
                    continue;
                }

                while (block.ItemId == Block.InvalidId || usedIds.Contains(block.ItemId))
                {
                    block.ItemId = NextItemId();
                }

                usedIds.Add(block.ItemId);
                _blockManager.Add(block, false);
            }
        }

        public void RefreshBlocks()
        {
            var blocks = _blockManager.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block != null)
                {
                    block.Refresh();
                }
            }
        }

        public ushort NextItemId()
        {
            ushort result = _nextItemId;
            _nextItemId++;
            return result;
        }

        public IBlock FindBlock(string blockName)
        {
            var blocks = _blockManager.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block != null && block.BlockName == blockName)
                {
                    return block;
                }
            }

            return null;
        }

        public bool HasBlock(string blockName)
        {
            return FindBlock(blockName) != null;
        }

        public bool ExecuteIfHasBlock(string blockName, Action<string> executeByName)
        {
            if (!HasBlock(blockName))
            {
                return false;
            }

            executeByName?.Invoke(blockName);
            return true;
        }

        public void ExecuteBlock(string blockName)
        {
            var block = FindBlock(blockName);
            if (block == null)
            {
                Debug.LogError("Block " + blockName + " does not exist");
                return;
            }

            if (!ExecuteBlock(block))
            {
                Debug.LogWarning("Block " + blockName + " failed to execute");
            }
        }

        public void StopBlock(string blockName)
        {
            var block = FindBlock(blockName);
            if (block == null)
            {
                Debug.LogError("Block " + blockName + " does not exist");
                return;
            }

            if (block.IsExecuting())
            {
                block.Stop();
            }
        }

        public bool ExecuteBlock(IBlock block, int commandIndex = 0, Action onComplete = null)
        {
            bool BlockBelongsToOwner(IBlock candidate)
            {
                bool actualInput = candidate != null && candidate.Owner != null && candidate.Owner.gameObject != null;
                bool weHaveOwner = Owner != null && Owner.gameObject != null;
                bool result = weHaveOwner && actualInput && candidate.Owner.gameObject == Owner.gameObject;
                return result;
            }

            if (block == null)
            {
                Debug.LogError("Block must not be null");
                return false;
            }

            if (!BlockBelongsToOwner(block))
            {
                Debug.LogError("Block must belong to the same gameObject as this Flowchart");
                return false;
            }

            if (block.IsExecuting())
            {
                Debug.LogWarning(block.BlockName + " cannot be called/executed, it is already running.");
                return false;
            }

            if (_owner == null)
            {
                Debug.LogError("Cannot execute block because BlockLogicManager has no owner.");
                return false;
            }

            _owner.StartCoroutine(block.Execute(commandIndex, onComplete));
            return true;
        }

        public void StopAllBlocks()
        {
            var blocks = _blockManager.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block != null && block.IsExecuting())
                {
                    block.Stop();
                }
            }
        }

        public bool HasExecutingBlocks()
        {
            var blocks = _blockManager.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block != null && block.IsExecuting())
                {
                    return true;
                }
            }

            return false;
        }

        public IList<IBlock> GetExecutingBlocks()
        {
            var result = new List<IBlock>();
            var blocks = _blockManager.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block != null && block.IsExecuting())
                {
                    result.Add(block);
                }
            }

            return result;
        }

        #region IBlockSource Implementation
        public IReadOnlyList<IBlock> Blocks => _blockManager.Blocks;
        public bool Contains(IBlock block) => _blockManager.Contains(block);
        public IBlock GetBlock(ushort id) => _blockManager.GetBlock(id);
        public bool Add(IBlock block, bool triggerSignals = true) => _blockManager.Add(block, triggerSignals);
        public bool Remove(IBlock block, bool triggerSignals = true) => _blockManager.Remove(block, triggerSignals);
        public bool RemoveBlockWithId(ushort id, bool triggerSignals = true) => _blockManager.RemoveBlockWithId(id, triggerSignals);

        public bool Contains(ICommand cmd) => _legacyCommands.Contains(cmd as Command);

        public bool ClearBlocks(bool triggerSignals)
        {
            return _blockManager.ClearBlocks(triggerSignals);
        }
        #endregion

        public ICommand GetCommandWithId(ushort id)
        {
            for (int i = 0; i < _legacyCommands.Count; i++)
            {
                var cmd = _legacyCommands[i];
                if (cmd != null && cmd.ItemId == id)
                {
                    return cmd;
                }
            }

            return null;
        }

        public void Add(ICommand cmd)
        {
            if (cmd != null && cmd is Command legacyCommand)
            {
                _legacyCommands.Add(legacyCommand);
            }
        }

        public bool Remove(ICommand cmd)
        {
            if (cmd == null)
            {
                return false;
            }

            bool belongsToUs = cmd.ParentBlock != null &&
                               _blockManager.GetBlock(cmd.ParentBlock.ItemId) == cmd.ParentBlock;

            if (!belongsToUs)
            {
                Debug.LogWarning($"Trying to remove Command {cmd.Name} from Flowchart {Name}, " +
                    $"but its ParentBlock does not belong to this Flowchart.");
                return false;
            }

            _legacyCommands.Remove(cmd as Command);
            cmd.ParentBlock.Remove(cmd);
            return true;
        }

        public bool RemoveAllCommands()
        {
            bool anyRemoved = _legacyCommands.Count > 0;

            var blocks = _blockManager.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block != null)
                {
                    block.RemoveAllCommands();
                }
            }

            _legacyCommands.Clear();
            return anyRemoved;
        }

        public bool RemoveCommandWithId(ushort id)
        {
            ICommand cmd = GetCommandWithId(id);
            return cmd != null && Remove(cmd);
        }

        public void Dispose()
        {
            _owner = null;
            _legacyCommands?.Clear();
        }

        public IBlock GetBlock(string name)
        {
            return _blockManager.GetBlock(name);
        }
    }
}