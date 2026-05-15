using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Scripting.APIUpdating;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Execution state of a Block.
    /// </summary>
    public enum ExecutionState
    {
        /// <summary> No command executing </summary>
        Idle,       
        /// <summary> Executing a command </summary>
        Executing,
    }

    /// <summary>
    /// A container for a sequence of Fungus comands.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Flowchart))]
    [AddComponentMenu("")]
    [MovedFrom(true, "AtMycelia.Hyphlow", "AtMycelia.Amanita.Core")]
    public class Block : Node, IEquatable<Block>, ICommandSource, IRefreshable
    {
        [SerializeField] protected AccessScope _scope = AccessScope.Public;

        [FormerlySerializedAs("itemId")]
        [SerializeField] protected ushort _itemId = 0; 

        [FormerlySerializedAs("sequenceName")]
        [Tooltip("The name of the block node as displayed in the Flowchart window")]
        [FormerlySerializedAs("blockName")]
        [SerializeField] protected string _blockName = "New Block";

        [TextArea(2, 5)]
        [Tooltip("Description text to display under the block node")]
        [FormerlySerializedAs("description")]
        [SerializeField] protected string _description = "";

        [Tooltip("An optional Event Handler which can execute the block when an event occurs")]
        [FormerlySerializedAs("eventHandler")]
        [SerializeField] protected EventHandler _eventHandler;

        [FormerlySerializedAs("commandList")]
        [SerializeField] protected List<Command> _commandList = new List<Command>();

        [Tooltip("If true, the save system will keep track of (and when appropriate, load) " +
            "this Block's execution state.")]
        [FormerlySerializedAs("includeInSaves")]
        [SerializeField] protected bool _includeInSaves = true;

        [FormerlySerializedAs("loadPriority")]
        [SerializeField] protected int _loadPriority;

        public static readonly ushort InvalidId = 0;

        public virtual AccessScope Scope
        {
            get { return _scope; }
            set { _scope = value; }
        }

        public virtual bool IncludeInSaves
        {
            get { return _includeInSaves; }
            set { _includeInSaves = value; }
        }

        public virtual int LoadPriority
        {
            get { return _loadPriority; }
            set { _loadPriority = value; }
        }

        protected ExecutionState _executionState;

        protected Command _activeCommand;

        protected Action _lastOnCompleteAction;

        /// <summary>
        // Index of last command executed before the current one.
        // -1 indicates no previous command.
        /// </summary>
        protected int _previousActiveCommandIndex = -1;

        public int PreviousActiveCommandIndex { get { return _previousActiveCommandIndex; } }

        protected int _jumpToCommandIndex = -1;

        protected int _executionCount;

        protected bool _executionInfoSet = false;

        /// <summary>
        /// If set, flowchart will not auto select when it is next executed, 
        /// used by eventhandlers. Only affects the editor.
        /// </summary>
        public bool SuppressNextAutoSelection { get; set; } = true;

        [SerializeField] bool suppressAllAutoSelections = true;
        

        protected virtual void Awake()
        {
            Refresh();
        }

        public virtual void Refresh()
        {
            AssertOwnershipAndUpdateIndexes();
            RefreshCommandListDict();
            RefreshCommands();
            UpdateIndentLevels();

            _executionInfoSet = true;
        }

        private void AssertOwnershipAndUpdateIndexes()
        {
            // Give each child command a reference back to its parent block
            // and tell each command its index in the list.
            byte index = 0;
            for (byte i = 0; i < _commandList.Count; i++)
            {
                var command = _commandList[i];
                if (command == null)
                {
                    continue;
                }
                command.ParentBlock = this;
                command.CommandIndex = index++;
            }
        }

        private void RefreshCommandListDict()
        {
            _commandListDict.Clear();
            for (int i = 0; i < _commandList.Count; i++)
            {
                var command = _commandList[i];
                if (command != null)
                {
                    _commandListDict[command.ItemId] = command;
                }
            }
        }

        private IDictionary<ushort, Command> _commandListDict = new Dictionary<ushort, Command>();
        // ^This is used to speed up lookup of Commands by their unique Ids, which
        // things such as the editor may want to do frequently.

        private void RefreshCommands()
        {
            for (int i = 0; i < _commandList.Count; i++)
            {
                var command = _commandList[i];
                if (command != null)
                {
                    command.Refresh();
                }
            }
        }

#if UNITY_EDITOR
        // The user can modify the command list order while playing in the editor,
        // so we keep the command indices updated every frame. There's no need to
        // do this in player builds, so we compile this bit out for those.
        protected virtual void Update()
        {
            byte index = 0;
            for (byte i = 0; i < _commandList.Count; i++)
            {
                var command = _commandList[i];
                if (command == null) // Null entry will be deleted automatically later
                {
                    continue;
                }
                command.CommandIndex = index++;
            }
        }
#endif
        //editor only state for speeding up flowchart window drawing
        public bool IsSelected { get; set; }    //local cache of selectedness
        public enum FilteredState { Full, Partial, None}
        public FilteredState FilterState { get; set; }    //local cache of filteredness
        public bool IsControlSelected { get; set; } //local cache of being part of the control exclusion group

        #region Public members

        /// <summary>
        /// The execution state of the Block.
        /// </summary>
        public virtual ExecutionState State { get { return _executionState; } }

        /// <summary>
        /// Unique identifier for the Block.
        /// </summary>
        public virtual ushort ItemId { get { return _itemId; } set { _itemId = value; } }

        /// <summary>
        /// The name of the block node as displayed in the Flowchart window.
        /// </summary>
        public virtual string BlockName { get { return _blockName; } set { _blockName = value; } }

        /// <summary>
        /// Description text to display under the block node
        /// </summary>
        public virtual string Description { get { return _description; } }

        /// <summary>
        /// An optional Event Handler which can execute the block when an event occurs.
        /// Note: Using the concrete class instead of the interface here because 
        /// of weird editor behaviour.
        /// </summary>
        public virtual EventHandler _EventHandler { get { return _eventHandler; } set { _eventHandler = value; } }

        /// <summary>
        /// The currently executing command.
        /// </summary>
        public virtual Command ActiveCommand { get { return _activeCommand; } }

        /// <summary>
        /// Timer for fading Block execution icon.
        /// </summary>
        public virtual float ExecutingIconTimer { get; set; }

        /// <summary>
        /// The list of commands in the sequence.
        /// </summary>
        public virtual List<Command> CommandList { get { return _commandList; } }

        /// <summary>
        /// Controls the next command to execute in the block execution coroutine.
        /// </summary>
        public virtual int JumpToCommandIndex { set { _jumpToCommandIndex = value; } }

        public IReadOnlyList<Command> Commands => _commandList;

        /// <summary>
        /// Returns the parent Flowchart for this Block.
        /// </summary>
        public virtual Flowchart GetFlowchart()
        {
            if (this == null)
            {
                return null;
            }
            if (_owner == null)
            {
                _owner = GetComponent<Flowchart>();
            }
            return _owner as Flowchart;
        }

        public virtual UnityObj Owner
        {
            get
            {
                if (_owner == null)
                {
                    _owner = GetComponent<Flowchart>();
                }
                return _owner;
            }
            set
            {
                _owner = value;
            }
        }

        private UnityObj _owner;

        /// <summary>
        /// Returns true if the Block is executing a command.
        /// </summary>
        public virtual bool IsExecuting()
        {
            return (_executionState == ExecutionState.Executing);
        }

        /// <summary>
        /// Returns the number of times this Block has executed.
        /// </summary>
        public virtual int GetExecutionCount()
        {
            return _executionCount;
        }

        /// <summary>
        /// Start a coroutine which executes all commands in the Block. Only one running 
        /// instance of each Block is permitted.
        /// </summary>
        public virtual void StartExecution()
        {
            StartCoroutine(Execute());
        }

        /// <summary>
        /// A coroutine method that executes all commands in the Block. Only one 
        /// running instance of each Block is permitted.
        /// </summary>
        /// <param name="commandIndex">Index of command to start execution at</param>
        /// <param name="onComplete">Delegate function to call when execution completes</param>
        public virtual IEnumerator Execute(int commandIndex = 0, Action onComplete = null)
        {
            if (_executionState != ExecutionState.Idle)
            {
                Debug.LogWarning(BlockName + " cannot be executed, it is already running.");
                yield break;
            }

            _lastOnCompleteAction = onComplete;

            if (!_executionInfoSet)
            {
                Refresh();
            }

            _executionCount++;
            var executionCountAtStart = _executionCount;

            var flowchart = GetFlowchart();
            _executionState = ExecutionState.Executing;
            BlockSignals.DoBlockStart(this);

            bool suppressSelectionChanges = false;

            #if UNITY_EDITOR
            // Select the executing block & the first command
            if (suppressAllAutoSelections || SuppressNextAutoSelection)
            {
                SuppressNextAutoSelection = false;
                suppressSelectionChanges = true;
            }
            else if (Selection.activeGameObject == flowchart.gameObject)
            {
                flowchart.SelectedBlock = this;
                if (_commandList.Count > 0)
                {
                    flowchart.ClearSelectedCommands();
                    flowchart.AddSelectedCommand(_commandList[0]);
                }
            }
            #endif

            _jumpToCommandIndex = commandIndex;

            int i = 0;
            while (true)
            {
                // Executing commands specify the next command to skip to by setting
                // jumpToCommandIndex using Command.Continue()
                if (_jumpToCommandIndex > -1)
                {
                    i = _jumpToCommandIndex;
                    _jumpToCommandIndex = -1;
                }

                // Skip disabled commands, comments and labels
                _commandList.RemoveAll(cmd => cmd == null); // Clean up any null entries that may be in the list
                while (i < _commandList.Count &&
                      (!_commandList[i].enabled || 
                        _commandList[i].GetType() == typeof(Comment) ||
                        _commandList[i].GetType() == typeof(Label)))
                {
                    i = _commandList[i].CommandIndex + 1;
                }

                if (i >= _commandList.Count)
                {
                    break;
                }

                // The previous active command is needed for if / else / else if commands
                if (_activeCommand == null)
                {
                    _previousActiveCommandIndex = -1;
                }
                else
                {
                    _previousActiveCommandIndex = _activeCommand.CommandIndex;
                }

                var command = _commandList[i];
                _activeCommand = command;

                if (Selection.activeGameObject == flowchart.gameObject && flowchart.IsActive() && !suppressSelectionChanges)
                {
                    // Auto select a command in some situations
                    if ((flowchart.SelectedCommandCount == 0 && i == 0) ||
                        (flowchart.SelectedCommandCount == 1 && flowchart.SelectedCommands[0].CommandIndex == _previousActiveCommandIndex))
                    {
                        flowchart.ClearSelectedCommands();
                        flowchart.AddSelectedCommand(_commandList[i]);
                    }
                }

                command.IsExecuting = true;
                // This icon timer is managed by the FlowchartWindow class, but we also need to
                // set it here in case a command starts and finishes execution before the next window update.
                command.ExecutingIconTimer = Time.realtimeSinceStartup + HyphlowConstants.ExecutingIconFadeTime;
                BlockSignals.DoCommandExecute(this, command, i, _commandList.Count);

#if UNITY_EDITOR
                try
                {
                    command.Execute();
                }
                catch (Exception)
                {
                    Debug.LogError("Rethrowing Exception thrown by:" + command.GetLocationIdentifier());
                    throw;
                }
#else
                command.Execute();
#endif

                // Wait until the executing command sets another command to jump to via Command.Continue()
                while (_jumpToCommandIndex == -1)
                {
                    yield return null;
                }

                #if UNITY_EDITOR
                if (flowchart.StepPause > 0f)
                {
                    yield return new WaitForSeconds(flowchart.StepPause);
                }
                #endif

                command.IsExecuting = false;
            }

            if(State == ExecutionState.Executing &&
                //ensure we aren't dangling from a previous stopage and stopping a future run
                executionCountAtStart == _executionCount)
            {
                ReturnToIdle();
            }
        }

        private void ReturnToIdle()
        {
            _executionState = ExecutionState.Idle;
            _activeCommand = null;
            BlockSignals.DoBlockEnd(this);

            if (_lastOnCompleteAction != null)
            {
                _lastOnCompleteAction();
            }
            _lastOnCompleteAction = null;
        }

        /// <summary>
        /// Stop executing commands in this Block.
        /// </summary>
        public virtual void Stop()
        {
            // Tell the executing command to stop immediately
            if (_activeCommand != null)
            {
                _activeCommand.IsExecuting = false;
                _activeCommand.OnStopExecuting();
            }

            // This will cause the execution loop to break on the next iteration
            _jumpToCommandIndex = int.MaxValue;

            //force idle here so other commands that rely on block not executing are informed this frame rather than next
            ReturnToIdle();
        }

        /// <summary>
        /// Returns a list of all Blocks connected to this one.
        /// </summary>
        public virtual List<Block> GetConnectedBlocks()
        {
            var connectedBlocks = new List<Block>();
            RefreshConnectedBlockCache(ref connectedBlocks);
            return connectedBlocks;
        }

        public virtual void RefreshConnectedBlockCache(ref List<Block> toRefresh)
        {
            if (_commandList == null)
            {
                return;
            }
            for (int i = 0; i < _commandList.Count; i++)
            {
                var command = _commandList[i];
                if (command != null)
                {
                    command.GetConnectedBlocks(ref toRefresh);
                }
            }
        }

        protected IList<Block> _connectedBlocks = new List<Block>();

        /// <summary>
        /// Returns the type of the previously executing command.
        /// </summary>
        /// <returns>The previous active command type.</returns>
        public virtual Type GetPreviousActiveCommandType()
        {
            if (_previousActiveCommandIndex >= 0 &&
                _previousActiveCommandIndex < _commandList.Count)
            {
                return _commandList[_previousActiveCommandIndex].GetType();
            }

            return null;
        }

        public virtual int GetPreviousActiveCommandIndent()
        {
            if (_previousActiveCommandIndex >= 0 &&
                _previousActiveCommandIndex < _commandList.Count)
            {
                return _commandList[_previousActiveCommandIndex].IndentLevel;
            }

            return -1;
        }

        public virtual Command GetPreviousActiveCommand()
        {
            if (_previousActiveCommandIndex >= 0 &&
                _previousActiveCommandIndex < _commandList.Count)
            {
                return _commandList[_previousActiveCommandIndex];
            }

            return null;
        }

        /// <summary>
        /// Recalculate the indent levels for all commands in the list.
        /// </summary>
        public virtual void UpdateIndentLevels()
        {
            int indentLevel = 0;
            for (int i = 0; i < _commandList.Count; i++)
            {
                var command = _commandList[i];
                if (command == null)
                {
                    continue;
                }
                if (command.CloseBlock())
                {
                    indentLevel--;
                }
                // Negative indent level is not permitted
                indentLevel = Math.Max(indentLevel, 0);
                command.IndentLevel = indentLevel;
                if (command.OpenBlock())
                {
                    indentLevel++;
                }
            }
        }

        /// <summary>
        /// Returns the index of the Label command with matching key, or -1 if not found.
        /// </summary>
        public virtual int GetLabelIndex(string labelKey)
        {
            if (labelKey.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < _commandList.Count; i++)
            {
                var command = _commandList[i];
                var labelCommand = command as Label;
                bool foundIt = labelCommand != null && String.Compare(labelCommand.Key, labelKey, true) == 0;
                if (foundIt)
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion

        public virtual bool Equals(Block other)
        {
            return this != null && other != null &&
                this._itemId == other._itemId &&
                this.GetFlowchart().UniqueId == other.GetFlowchart().UniqueId &&
                this.BlockName == other.BlockName;
        }

        public bool Contains(Command cmd)
        {
            bool result = _commandListDict[cmd.ItemId] == cmd;
            return result;
        }

        public Command GetCommandWithId(ushort id)
        {
            Command result = _commandListDict[id];
            return result;
        }

        /// <summary>
        /// Add a command to the end of the command list. If a command with the same unique ID 
        /// already exists in the list, we won't re-add it.
        /// </summary>
        public void Add(Command cmd)
        {
            bool alreadyRegistered = _commandListDict.TryGetValue(cmd.ItemId, out Command existingCmd) && existingCmd == cmd;
            if (alreadyRegistered)
            {
                string warningMessage = $"Command with id {cmd.ItemId} is already " +
                    $"registered to block {BlockName}";
                Debug.LogWarning(warningMessage);
                return;
            }
            _commandList.Add(cmd);
            _commandListDict[cmd.ItemId] = cmd;
            cmd.ParentBlock = this;
            if (_owner is Flowchart fc)
            {
                fc.Add(cmd);
                // ^To keep the FC's cache updated
            }
            cmd.OnCommandAdded(this);
        }

        public virtual bool RemoveCommandWithId(ushort id)
        {
            Command toRemove = _commandListDict[id];
            bool successfulRemoval = Remove(toRemove);
            return successfulRemoval;
        }

        public virtual bool Remove(Command cmd)
        {
            bool successfulRemoval = _commandList.Remove(cmd);
            if (successfulRemoval)
            {
                _commandList.Remove(cmd);
                _commandListDict.Remove(cmd.ItemId);
                cmd.OnCommandRemoved(this);
            }
            return successfulRemoval;
        }

        public virtual bool RemoveAllCommands()
        {
            bool anyToRemove = _commandList.Count > 0;
            while (_commandList.Count > 0)
            {
                var cmd = _commandList[0];
                Remove(cmd);
            }
            return anyToRemove;
        }

    }
}
