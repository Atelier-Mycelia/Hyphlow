using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    public interface IBlockExecutionManager : IBlockExecutor, IDisposable, 
        IHasName, IRefreshable
    {
        
    }

    public interface ICommandResetter
    {
        void ResetCommands();
    }

    [Serializable]
    public sealed class BlockExecutionManager : IBlockExecutionManager, IDisposable
    {
        [SerializeField] [HideInInspector] private MonoBehaviour _coroutineRunner;

        public void Initialize(IBlockManager blockManager, MonoBehaviour coroutineRunner)
        {
            _blockManager = blockManager;
            _blockManager.BlockOwner = coroutineRunner;
            CoroutineRunner = coroutineRunner;
            Refresh();
        }

        private IBlockManager _blockManager;

        public MonoBehaviour CoroutineRunner
        {
            get => _coroutineRunner;
            set
            {
                _coroutineRunner = value;
            }
        }

        public string Name
        {
            get
            {
                if (_coroutineRunner != null && !string.IsNullOrEmpty(_coroutineRunner.name))
                {
                    return _coroutineRunner.name;
                }

                return _defaultName;
            }
            set => Debug.LogWarning("BlockLogicManager.Name is read-only and cannot be set.");
        }

        private static readonly string _defaultName = "UnownedBlockLogicManager";

        /// <summary>
        /// To make sure this is working with the right stuff.
        /// </summary>
        public void Refresh()
        {
            RefreshCaches();
        }

        private void RefreshCaches()
        {
            _executingCommands ??= new Dictionary<byte, ICommand>();
            _executingBlocks ??= new Dictionary<byte, IBlock>();
            _executingCommands.Clear();
            _executingBlocks.Clear();

            for (int i = 0; i < _blockManager.Blocks.Count; i++)
            {
                var block = _blockManager.Blocks[i];
                if (block != null && block.IsExecuting)
                {
                    _executingBlocks[block.ItemId] = block;
                }
                else
                {
                    continue;
                }

                for (int j = 0; j < block.CommandList.Count; j++)
                {
                    var cmd = block.CommandList[j];
                    if (cmd != null && cmd.IsExecuting)
                    {
                        _executingCommands[cmd.ItemId] = cmd;
                    }
                }
            }
        }

        private IDictionary<byte, ICommand> _executingCommands = new Dictionary<byte, ICommand>();
        private IDictionary<byte, IBlock> _executingBlocks = new Dictionary<byte, IBlock>();

        /// <summary>
        /// Returns the block with the given name, if it exists and is executing.
        /// </summary>
        public IBlock FindBlock(string blockName)
        {
            var blocks = _blockManager.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block != null && block.BlockName == blockName && block.IsExecuting)
                {
                    return block;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if the block with the given name exists and is executing.
        /// </summary>
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
            var block = _blockManager.GetBlock(blockName);
            if (block == null)
            {
                Debug.LogError("Block " + blockName + " does not exist");
                return;
            }

            if (block.IsExecuting)
            {
                Debug.LogWarning("Block " + blockName + " is already executing");
                return;
            }

            bool success = ExecuteBlock(block);
            if (!success)
            {
                Debug.LogWarning("Block " + blockName + " failed to execute");
            }
        }

        /// <summary>
        /// Returns true if execution successfully started, false if it failed to start 
        /// (e.g. block is already executing, or block is not associated with this manager).
        /// </summary>
        public bool ExecuteBlock(IBlock block, int commandIndex = 0, Action onComplete = null)
        {
            if (block == null)
            {
                Debug.LogError("Block must not be null");
                return false;
            }

            bool weShouldWorkWithIt = block == _blockManager.GetBlock(block.ItemId);
            string errorMessage;
            if (!weShouldWorkWithIt)
            {
                errorMessage = $"Block {block.BlockName} either doesn't exist, " +
                    $"is not executing, or is not associated with this manager.";
                Debug.LogError(errorMessage);
                return false;
            }

            if (block.IsExecuting)
            {
                errorMessage = $"Block {block.BlockName} is already executing and cannot " +
                    $"be executed again until it's done.";
                Debug.LogWarning(errorMessage);
                return false;
            }

            if (_coroutineRunner == null)
            {
                errorMessage = $"Cannot execute block {block.BlockName} because " +
                    $"BlockLogicManager has no CoroutineRunner.";
                Debug.LogError(errorMessage);
                return false;
            }

            _coroutineRunner.StartCoroutine(block.Execute(commandIndex, onComplete));
            return true;
        }

        private IEnumerator ExecutionCoroutine(IBlock toExecute, int commandIndex, Action onComplete = null)
        {
            if (commandIndex >= toExecute.CommandList.Count)
            {
                string warningMessage = $"Command index {commandIndex} is out of range " +
                    $"for Block {toExecute.BlockName}. Executing from the start of the " +
                    $"Block instead.";
                Debug.LogWarning(warningMessage);
                commandIndex = 0;
            }
            onComplete ??= delegate { };
            _lastOnCompleteActions[toExecute] = onComplete;
            toExecute.ExecutionCount++;
            _executionCountsAtStart[toExecute] = toExecute.ExecutionCount;

            Flowchart fc = toExecute.GetFlowchart();
            toExecute.ExecutionState = ExecutionState.Executing;
            BlockSignals.BlockExecStarted(toExecute);

            bool doAutoSelect = !toExecute.SuppressNextAutoSelection && toExecute.CommandList.Count > 0;
            if (doAutoSelect)
            {
                fc.SelectedBlock = toExecute;
                fc.ClearSelectedCommands();
                fc.AddSelectedCommand(toExecute.CommandList[commandIndex]);
            }
            yield return null;
        }

        private readonly IDictionary<IBlock, Action> _lastOnCompleteActions = new Dictionary<IBlock, Action>();
        private readonly IDictionary<IBlock, int> _executionCountsAtStart = new Dictionary<IBlock, int>();

        public void StopBlock(string blockName)
        {
            IBlock block = FindBlock(blockName);
            if (block == null)
            {
                string errorMessage = $"Block {blockName} either doesn't exist, is not executing, or is not " +
                    $"associated with this manager.";
                Debug.LogError(errorMessage);
                return;
            }

            block.Stop();
        }

        public void StopAllBlocks()
        {
            var executing = ExecutingBlocks;
            // ^So we don't mutate the dictionary while iterating over it.
            // We want to stop all executing blocks, so we make a copy of
            // the values and iterate over that.
            foreach (IBlock toStop in executing)
            {
                toStop.Stop();
            }
        }

        public bool HasExecutingBlocks()
        {
            return _executingBlocks.Count > 0;
        }

        #region IBlockSource Implementation
        public IReadOnlyList<IBlock> Blocks => _blockManager.Blocks;

        public IReadOnlyList<IBlock> ExecutingBlocks
        {
            get
            {
                var result = new List<IBlock>(_executingBlocks.Values);
                return result;
            }
        }

        public bool Contains(IBlock block) => _blockManager.Contains(block);
        public IBlock GetBlock(byte id) => _blockManager.GetBlock(id);

        /// <summary>
        /// Returns true if the given command is currently executing and is associated with this manager.
        /// </summary>
        public bool Contains(ICommand cmd)
        {
            if (cmd == null)
            {
                return false;
            }
            bool result = _executingCommands.TryGetValue(cmd.ItemId, out ICommand found) 
                && ReferenceEquals(cmd, found);
            return result;
        }

        public bool ClearBlocks(bool triggerSignals)
        {
            return _blockManager.ClearBlocks(triggerSignals);
        }
        #endregion

        /// <summary>
        /// Returns the command with the given ID (if it is executing).
        /// If it's either not executing or involved with this manager, 
        /// returns null.
        /// </summary>
        public ICommand GetCommandWithId(byte id)
        {
            _executingCommands.TryGetValue(id, out ICommand cmd);
            return cmd;
        }

        public void Dispose()
        {
            _coroutineRunner = null;
        }

        /// <summary>
        /// Returns the block with the given name, if it exists and is executing. 
        /// Otherwise, returns null.
        /// </summary>
        public IBlock GetBlock(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (IBlock block in _executingBlocks.Values)
            {
                if (block != null && block.BlockName == name)
                {
                    return block;
                }
            }
            return null;
        }

        public void ExecuteBlock(byte blockId)
        {
            IBlock toExecute = _blockManager.GetBlock(blockId);
            if (toExecute == null)
            {
                Debug.LogError($"Block with ID {blockId} does not exist.");
                return;
            }
            ExecuteBlock(toExecute);
        }

        public void StopBlock(byte blockId)
        {
            IBlock toStop = _blockManager.GetBlock(blockId);
            if (toStop == null)
            {
                Debug.LogError($"Block with ID {blockId} does not exist.");
                return;
            }
            toStop.Stop();
        }
    }
}