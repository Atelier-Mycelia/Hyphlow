using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AtMycelia.Hyphlow.EditorExt;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

    /// <summary>
    /// Manages execution of blocks and commands.
    /// </summary>
    [Serializable]
    public sealed class BlockExecutionManager : IBlockExecutionManager, IDisposable
    {
        [SerializeField, HideInInspector] private MonoBehaviour _coroutineRunner;
        
        public MonoBehaviour CoroutineRunner
        {
            get => _coroutineRunner;
            set => _coroutineRunner = value;
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

        public void Initialize(IBlockManager blockManager, MonoBehaviour coroutineRunner)
        {
            _blockManager = blockManager;
            if (_blockManager != null)
            {
                _blockManager.BlockOwner = coroutineRunner;
            }

            CoroutineRunner = coroutineRunner;
            Refresh();
        }

        private IBlockManager _blockManager;

        public void Refresh()
        {
            RebuildSubscriptionsAndCaches();
        }

        private void RebuildSubscriptionsAndCaches()
        {
            _executingCommands.Clear();
            _executingBlocks.Clear();
            UnsubscribeAll();

            if (_blockManager == null || _blockManager.Blocks == null)
            {
                return;
            }

            IReadOnlyList<IBlock> blocks = _blockManager.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                IBlock block = blocks[i];
                if (block == null)
                {
                    continue;
                }

                SubscribeBlock(block);

                if (block.IsExecuting)
                {
                    _executingBlocks[block.ItemId] = block;
                }

                IList<ICommand> commands = block.CommandList;
                for (int j = 0; j < commands.Count; j++)
                {
                    ICommand command = commands[j];
                    if (command == null)
                    {
                        continue;
                    }

                    SubscribeCommand(command);

                    if (command.IsExecuting)
                    {
                        _executingCommands[command.ItemId] = command;
                    }
                }
            }
        }

        private readonly IDictionary<byte, ICommand> _executingCommands = new Dictionary<byte, ICommand>();
        private readonly IDictionary<byte, IBlock> _executingBlocks = new Dictionary<byte, IBlock>();

        private void UnsubscribeAll()
        {
            foreach (IBlock block in _subscribedBlocks)
            {
                if (block == null)
                {
                    continue;
                }

                block.ExecStarted -= OnBlockExecStarted;
                block.ExecEnded -= OnBlockExecEnded;
            }
            _subscribedBlocks.Clear();

            foreach (ICommand command in _subscribedCommands)
            {
                if (command == null)
                {
                    continue;
                }

                command.ExecStarted -= OnCommandExecStarted;
                command.ExecEnded -= OnCommandExecEnded;
            }
            _subscribedCommands.Clear();
        }

        /// <summary>
        /// Returns the block with the given name, if it exists and is executing.
        /// </summary>
        public IBlock FindBlock(string blockName)
        {
            foreach (var pair in _executingBlocks)
            {
                IBlock block = pair.Value;
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

        public void ExecuteBlock(string blockName)
        {
            IBlock block = _blockManager.GetBlock(blockName);
            bool success = ExecuteBlock(block);
            if (!success)
            {
                Debug.LogWarning($"Block {blockName} failed to execute");
            }
        }

        public void ExecuteBlock(byte blockId)
        {
            if (_blockManager == null)
            {
                Debug.LogError("Cannot execute block by ID because BlockManager is null.");
                return;
            }

            IBlock toExecute = _blockManager.GetBlock(blockId);
            ValidateBlockAsEntity(out bool isValid, toExecute);
            if (!isValid)
            {
                return;
            }

            ExecuteBlock(toExecute);
        }

        private void ValidateBlockAsEntity(out bool isValid, IBlock block)
        {
            isValid = false;
            if (block == null)
            {
                Debug.LogError("Block must not be null");
                return;
            }

            if (_blockManager == null)
            {
                Debug.LogError("Cannot execute block because BlockManager is null.");
                return;
            }

            bool considerWorkingWithIt = block == _blockManager.GetBlock(block.ItemId);
            if (!considerWorkingWithIt)
            {
                string errorMessage = $"Block {block.BlockName} either doesn't exist, " +
                    $"is not associated with this manager, or has stale registration.";
                Debug.LogError(errorMessage);
                return;
            }

            isValid = true;
        }

        /// <summary>
        /// Returns true if execution started.
        /// </summary>
        public bool ExecuteBlock(IBlock block, int commandIndex = 0, Action onComplete = null)
        {
            ValidateBlockAsEntity(out bool isValid, block);
            if (!isValid)
            {
                return false;
            }

            SubscribeBlock(block);
            ValidateBlockForExecution(out isValid, block);
            if (!isValid)
            {
                return false;
            }

            if (!block.Enabled)
            {
                onComplete?.Invoke();
                return false;
            }
            // ^We have this separate from ValidateBlockForExecution because in cases of 
            // trying to execute disabled blocks, we want to treat it as if the execution 
            // completes in the same frame this func is called. Hence the 
            // onComplete being invoked here.

            IEnumerator coroutine = ExecutionCoroutine(block, commandIndex, onComplete);
            _coroutineRunner.StartCoroutine(coroutine);
            return true;
        }

        private void ValidateBlockForExecution(out bool isValid, IBlock block)
        {
            ValidateBlockAsEntity(out isValid, block);
            if (!isValid)
            {
                return;
            }
            isValid = false;

            if (block.IsExecuting)
            {
                string errorMessage = $"Block {block.BlockName} is already executing and cannot " +
                    $"be executed again until it's done.";
                Debug.LogWarning(errorMessage);
                return;
            }

            if (_coroutineRunner == null)
            {
                string errorMessage = $"Cannot execute block {block.BlockName} because " +
                    $"BlockLogicManager has no CoroutineRunner.";
                Debug.LogError(errorMessage);
                return;
            }

            var commands = block.Commands;
            if (commands == null || commands.Count == 0)
            {
                Debug.LogWarning($"Block {block.BlockName} has no commands to execute.");
                return;
            }

            isValid = true;
        }

        private IEnumerator ExecutionCoroutine(IBlock blockToExec, int commandIndex, Action onComplete = null)
        {
            // We assume that the block is valid for execution. We also assume that
            // the block's CommandList is not null and has not changed since the
            // start of execution, as these are both things that should be guaranteed
            // by the block's validity for execution. If either of these assumptions
            // is violated, then it's likely that something has gone very wrong in
            // the execution environment, and we allow exceptions to be thrown in
            // that case rather than trying to handle them gracefully.
            IList<ICommand> commandsAtStart = blockToExec.CommandList;

            onComplete ??= delegate { };

            if (commandIndex < 0 || commandIndex >= commandsAtStart.Count)
            {
                string warningMessage = $"Command index {commandIndex} is out of range " +
                    $"for Block {blockToExec.BlockName}. Executing from the start of the " +
                    $"Block instead.";
                Debug.LogWarning(warningMessage);
                commandIndex = 0;
            }

            _lastOnCompleteActions[blockToExec] = onComplete;

            blockToExec.ExecutionCount++;
            int executionCountAtStart = blockToExec.ExecutionCount;
            _executionCountsAtStart[blockToExec] = executionCountAtStart;

            Flowchart flowchart = (Flowchart)blockToExec.Owner;
            blockToExec.ExecutionState = ExecutionState.Executing;
            blockToExec.NextExecCmdIndex = commandIndex;
            blockToExec.ActiveCommand = null;
            blockToExec.PreviousActiveCommandIndex = -1;

            bool suppressSelectionChanges = false;
            TrySelectExecutingBlockAtStart(blockToExec, flowchart, 
                commandIndex, ref suppressSelectionChanges);

            int commandCursor = 0;
            while (true)
            {
                // The reason we care (and need) the NextExecCmdIndex property is because
                // some Commands may require you to execute a different Command next
                // instead of the one that would normally follow in sequence. This is
                // especially relevant for Label and Jump.
                if (blockToExec.NextExecCmdIndex > -1)
                {
                    commandCursor = blockToExec.NextExecCmdIndex;
                    blockToExec.NextExecCmdIndex = -1;
                }

                IList<ICommand> commands = blockToExec.CommandList;
                if (commands == null || commandCursor >= commands.Count)
                {
                    break;
                }

                #region Move Command Cursor to Next Valid Command to Execute
                while (commandCursor < commands.Count)
                {
                    ICommand candidate = commands[commandCursor];
                    if (candidate != null && candidate.Enabled &&
                        !candidate.SkipExecution)
                    {
                        break;
                    }

                    commandCursor++;
                }
                #endregion

                bool movedTooFar = commandCursor >= commands.Count;
                // ^This can happen when the last command(s) in a block are disabled or set
                // to skip execution. In that case, we consider the block done executing
                // and exit.
                if (movedTooFar)
                {
                    break;
                }

                #region Handle Case Where Active Command Was Somehow Invalidated Since Last Command Executed
                if (blockToExec.ActiveCommand == null)
                {
                    blockToExec.PreviousActiveCommandIndex = -1;
                }
                else
                {
                    blockToExec.PreviousActiveCommandIndex = 
                        blockToExec.ActiveCommand.CommandIndex;
                }
                #endregion

                ICommand command = commands[commandCursor];
                SubscribeCommand(command);
                blockToExec.ActiveCommand = command;

                TrySelectExecutingCommand(flowchart, blockToExec, 
                    commands, commandCursor, suppressSelectionChanges);

                command.IsExecuting = true;
                command.ExecutionIconTimer = Time.realtimeSinceStartup + 
                    HyphlowConstants.ExecutingIconFadeTime;
                BlockSignals.DoCommandExecute(blockToExec, command, 
                    commandCursor, commands.Count);

#if UNITY_EDITOR
                try
                {
                    command.Execute();
                }
                catch (Exception)
                {
                    Debug.LogError("Rethrowing Exception thrown by:" + 
                        command.LocationIdentifier);
                    throw;
                }
#else
                command.Execute();
#endif

                while (blockToExec.NextExecCmdIndex == -1 && 
                    blockToExec.ExecutionState == ExecutionState.Executing)
                {
                    yield return null;
                }

#if UNITY_EDITOR
                if (flowchart != null)
                {
                    FlowchartEditorQol editorQol = flowchart.EditorQol;
                    float stepPause = editorQol != null ? editorQol.StepPause : 0f;
                    if (stepPause > 0f)
                    {
                        yield return new WaitForSeconds(stepPause);
                    }
                }
#endif

                command.IsExecuting = false;

                if (blockToExec.ExecutionState != ExecutionState.Executing)
                {
                    break;
                }
            }

            if (blockToExec.ExecutionState == ExecutionState.Executing &&
                executionCountAtStart == blockToExec.ExecutionCount)
            {
                ReturnToIdle(blockToExec, true);
            }
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

            StopBlockInternal(block);
        }

        public void StopBlock(byte blockId)
        {
            if (!_executingBlocks.TryGetValue(blockId, out IBlock block) || block == null)
            {
                if (_blockManager == null || _blockManager.GetBlock(blockId) == null)
                {
                    Debug.LogError($"Block with ID {blockId} does not exist.");
                    return;
                }

                return;
            }

            StopBlockInternal(block);
        }

        public void StopAllBlocks()
        {
            var executing = ExecutingBlocks;
            for (int i = 0; i < executing.Count; i++)
            {
                StopBlockInternal(executing[i]);
            }
        }

        private void StopBlockInternal(IBlock block)
        {
            if (block == null || block.ExecutionState != ExecutionState.Executing)
            {
                return;
            }

            ICommand active = block.ActiveCommand;
            if (active != null)
            {
                active.IsExecuting = false;
                _executingCommands.Remove(active.ItemId);
            }

            block.NextExecCmdIndex = int.MaxValue;
            ReturnToIdle(block, true);
        }

        private void ReturnToIdle(IBlock block, bool invokeOnComplete)
        {
            if (block == null)
            {
                return;
            }

            block.ExecutionState = ExecutionState.Idle;
            OnBlockExecEnded(block);

            block.ActiveCommand = null;
            block.PreviousActiveCommandIndex = -1;
            block.NextExecCmdIndex = -1;

            if (_executionCountsAtStart.ContainsKey(block))
            {
                _executionCountsAtStart.Remove(block);
            }

            BlockSignals.BlockExecEnded(block);

            if (!invokeOnComplete)
            {
                _lastOnCompleteActions.Remove(block);
                return;
            }

            if (_lastOnCompleteActions.TryGetValue(block, out Action callback))
            {
                _lastOnCompleteActions.Remove(block);
                callback?.Invoke();
            }
        }

        public bool HasExecutingBlocks()
        {
            return _executingBlocks.Count > 0;
        }

        public IReadOnlyList<IBlock> ExecutingBlocks
        {
            get
            {
                return new List<IBlock>(_executingBlocks.Values);
            }
        }

        public ICommand GetCommandWithId(byte id)
        {
            _executingCommands.TryGetValue(id, out ICommand cmd);
            return cmd;
        }

        public void Dispose()
        {
            UnsubscribeAll();

            _coroutineRunner = null;
            _blockManager = null;

            _executingCommands.Clear();
            _executingBlocks.Clear();
            _lastOnCompleteActions.Clear();
            _executionCountsAtStart.Clear();
        }

        private void SubscribeBlock(IBlock block)
        {
            if (block == null || _subscribedBlocks.Contains(block))
            {
                return;
            }

            block.ExecStarted += OnBlockExecStarted;
            block.ExecEnded += OnBlockExecEnded;
            _subscribedBlocks.Add(block);

            EnsureCommandSubscriptions(block);
        }

        private readonly ISet<IBlock> _subscribedBlocks = new HashSet<IBlock>();

        private void SubscribeCommand(ICommand command)
        {
            if (command == null || _subscribedCommands.Contains(command))
            {
                return;
            }

            command.ExecStarted += OnCommandExecStarted;
            command.ExecEnded += OnCommandExecEnded;
            _subscribedCommands.Add(command);
        }

        private readonly ISet<ICommand> _subscribedCommands = new HashSet<ICommand>();

        private void OnCommandExecStarted(ICommand command)
        {
            if (command == null)
            {
                return;
            }

            _executingCommands[command.ItemId] = command;
        }

        private void OnCommandExecEnded(ICommand command)
        {
            if (command == null)
            {
                return;
            }

            _executingCommands.Remove(command.ItemId);
        }

        private void EnsureCommandSubscriptions(IBlock block)
        {
            if (block == null || block.CommandList == null)
            {
                return;
            }

            IList<ICommand> commands = block.CommandList;
            for (int i = 0; i < commands.Count; i++)
            {
                SubscribeCommand(commands[i]);
            }
        }

        private void OnBlockExecStarted(IBlock block)
        {
            if (block == null)
            {
                return;
            }

            _executingBlocks[block.ItemId] = block;
            EnsureCommandSubscriptions(block);
        }

        private void OnBlockExecEnded(IBlock block)
        {
            if (block == null)
            {
                return;
            }

            _executingBlocks.Remove(block.ItemId);

            var cmdIdsToRemove = new List<byte>();
            foreach (var pair in _executingCommands)
            {
                ICommand cmd = pair.Value;
                if (cmd == null || cmd.ParentBlock == block)
                {
                    cmdIdsToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < cmdIdsToRemove.Count; i++)
            {
                _executingCommands.Remove(cmdIdsToRemove[i]);
            }
        }

        private static void TrySelectExecutingBlockAtStart(IBlock block, Flowchart flowchart,
            int commandIndex, ref bool suppressSelectionChanges)
        {
#if UNITY_EDITOR
            if (flowchart == null)
            {
                return;
            }

            if (block.SuppressAllAutoSelections || block.SuppressNextAutoSelection)
            {
                block.SuppressNextAutoSelection = false;
                suppressSelectionChanges = true;
                return;
            }

            if (Selection.activeGameObject == flowchart.gameObject &&
                commandIndex >= 0 &&
                commandIndex < block.CommandList.Count)
            {
                flowchart.SelectedBlock = block;
                flowchart.ClearSelectedCommands();
                flowchart.AddSelectedCommand(block.CommandList[commandIndex]);
            }
#endif
        }

        private static void TrySelectExecutingCommand(Flowchart flowchart, IBlock block, IList<ICommand> commands,
            int commandIndex, bool suppressSelectionChanges)
        {
#if UNITY_EDITOR
            if (flowchart == null || suppressSelectionChanges)
            {
                return;
            }

            if (Selection.activeGameObject != flowchart.gameObject || !flowchart.IsActive())
            {
                return;
            }

            bool shouldAutoSelect =
                (flowchart.SelectedCommandCount == 0 && commandIndex == 0) ||
                (flowchart.SelectedCommandCount == 1 &&
                 flowchart.SelectedCommands[0].CommandIndex == block.PreviousActiveCommandIndex);

            if (!shouldAutoSelect)
            {
                return;
            }

            flowchart.ClearSelectedCommands();
            flowchart.AddSelectedCommand(commands[commandIndex]);
#endif
        }
    }
}