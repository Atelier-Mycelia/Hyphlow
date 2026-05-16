using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    [Serializable]
    public sealed class BlockLogicManager : IBlockSource, ICommandSource, IDisposable
    {
        [SerializeField] [HideInInspector] private BlockManager _blocks = new BlockManager();
        [SerializeField] [HideInInspector] private List<Command> _commands = new List<Command>();
        [SerializeField] [HideInInspector] private MonoBehaviour _owner;

        private static readonly string _defaultName = "UnownedBlockLogicManager";

        [SerializeField] private ushort _nextItemId = 1;

        public BlockLogicManager()
        {
            _blocks.BlockOwner = this;
        }

        public BlockLogicManager(MonoBehaviour blockHolder)
        {
            _blocks.BlockOwner = this;
            Owner = blockHolder;
        }

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

        public IReadOnlyList<Command> Commands => _commands;
        public IReadOnlyDictionary<ushort, Block> BlockLookup
        {
            get
            {
                // If you later expose lookup in BlockManager publicly, delegate directly.
                // For now, build from source methods on demand.
                Dictionary<ushort, Block> dict = new Dictionary<ushort, Block>();
                for (int i = 0; i < _blocks.Blocks.Count; i++)
                {
                    var b = _blocks.Blocks[i];
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
            _commands ??= new List<Command>();

            _blocks = _blocks ?? new BlockManager();
            _blocks.BlockOwner = this;
            _blocks.Refresh();

            _commands.Clear();

            if (_owner == null)
            {
                _blocks.ClearBlocks(false);
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
                _commands.Add(cmd);
            }

            // 2) Rebuild block storage through BlockManager
            _blocks.ClearBlocks(false);
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
                _blocks.Add(block, false);
            }
        }

        public void RefreshBlocks()
        {
            var blocks = _blocks.Blocks;
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

        public Block FindBlock(string blockName)
        {
            var blocks = _blocks.Blocks;
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

        public bool ExecuteBlock(Block block, int commandIndex = 0, Action onComplete = null)
        {
            bool BlockBelongsToOwner(Block candidate)
            {
                return _owner != null && candidate != null && candidate.gameObject == _owner.gameObject;
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
            var blocks = _blocks.Blocks;
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
            var blocks = _blocks.Blocks;
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

        public List<Block> GetExecutingBlocks()
        {
            var result = new List<Block>();
            var blocks = _blocks.Blocks;
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
        public IReadOnlyList<Block> Blocks => _blocks.Blocks;
        public bool Contains(Block block) => _blocks.Contains(block);
        public Block GetBlockWithId(ushort id) => _blocks.GetBlockWithId(id);
        public bool Add(Block block, bool triggerSignals = true) => _blocks.Add(block, triggerSignals);
        public bool Remove(Block block, bool triggerSignals = true) => _blocks.Remove(block, triggerSignals);
        public bool RemoveBlockWithId(ushort id, bool triggerSignals = true) => _blocks.RemoveBlockWithId(id, triggerSignals);

        public bool Contains(Command cmd) => _commands.Contains(cmd);

        public bool ClearBlocks(bool triggerSignals)
        {
            return _blocks.ClearBlocks(triggerSignals);
        }
        #endregion

        public Command GetCommandWithId(ushort id)
        {
            for (int i = 0; i < _commands.Count; i++)
            {
                var cmd = _commands[i];
                if (cmd != null && cmd.ItemId == id)
                {
                    return cmd;
                }
            }

            return null;
        }

        public void Add(Command cmd)
        {
            if (cmd != null)
            {
                _commands.Add(cmd);
            }
        }

        public bool Remove(Command cmd)
        {
            if (cmd == null)
            {
                return false;
            }

            Block ourBlock = null;
            bool belongsToUs = cmd.ParentBlock != null &&
                               _blocks.GetBlockWithId(cmd.ParentBlock.ItemId) == cmd.ParentBlock;

            if (!belongsToUs)
            {
                Debug.LogWarning($"Trying to remove Command {cmd.name} from Flowchart {Name}, but its ParentBlock does not belong to this Flowchart.");
                return false;
            }

            _commands.Remove(cmd);
            cmd.ParentBlock.Remove(cmd);
            return true;
        }

        public bool RemoveAllCommands()
        {
            bool anyRemoved = _commands.Count > 0;

            var blocks = _blocks.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block != null)
                {
                    block.RemoveAllCommands();
                }
            }

            _commands.Clear();
            return anyRemoved;
        }

        public bool RemoveCommandWithId(ushort id)
        {
            Command cmd = GetCommandWithId(id);
            return cmd != null && Remove(cmd);
        }

        public void Dispose()
        {
            _owner = null;
            _commands?.Clear();
            _blocks?.Dispose();
        }

    }
}