using AtMycelia.Hyphlow.EditorUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Execution state of a Block.
    /// </summary>
    public enum ExecutionState
    {
        /// <summary> No Command executing </summary>
        Idle,       
        /// <summary> Executing a Command </summary>
        Executing,
    }

    /// <summary>
    /// A container for a sequence of Hyphlow comands.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Flowchart))]
    [AddComponentMenu("")]
    [MovedFrom(true, "AtMycelia.Hyphlow", "AtMycelia.Amanita.Core")]
    public class Block : Node, IBlock, IEquatable<Block>, ICommandSource, IRefreshable, IHasKey
    {
        [SerializeField] protected AccessScope _scope = AccessScope.Public;

        [FormerlySerializedAs("itemId")]
        [SerializeField] protected byte _itemId = 0; 

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
        [FormerlySerializedAs("_eventHandler")]
        [SerializeField] protected EventHandler _legacyEventHandler;

        [FormerlySerializedAs("commandList")]
        [FormerlySerializedAs("_commandList")]
        [SerializeField] protected List<Command> _legacyCommandList = new List<Command>();

        [Tooltip("If true, the save system will keep track of (and when appropriate, load) " +
            "this Block's execution state.")]
        [FormerlySerializedAs("includeInSaves")]
        [SerializeField] protected bool _includeInSaves = true;

        [FormerlySerializedAs("loadPriority")]
        [SerializeField] protected int _loadPriority;
        [SerializeField, HideInInspector] protected UnityObj _owner;

        [SerializeField, HideInInspector] private byte _nextValidCommandId = 1; 
        // ^Start at 1, since 0 is reserved for InvalidId

        public static readonly byte InvalidId = 0;

        public virtual bool SuppressAllAutoSelections
        {
            get { return suppressAllAutoSelections; }
            set { suppressAllAutoSelections = value; }
        }
        /// <summary>
        /// Alias for BlockName, used for IHasKey interface.
        /// </summary>
        public virtual string Key
        {
            get { return BlockName; }
            set { BlockName = value; }
        }

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

        public virtual ExecutionState ExecutionState
        {
            get { return _executionState; }
            set { _executionState = value; }
        }

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
            for (byte i = 0; i < _legacyCommandList.Count; i++)
            {
                var command = _legacyCommandList[i];
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
            for (int i = 0; i < _legacyCommandList.Count; i++)
            {
                var command = _legacyCommandList[i];
                if (command != null)
                {
                    _commandListDict[command.ItemId] = command;
                }
            }
        }

        private IDictionary<byte, ICommand> _commandListDict = new Dictionary<byte, ICommand>();
        // ^This is used to speed up lookup of Commands by their unique Ids, which
        // things such as the editor may want to do frequently.

        private void RefreshCommands()
        {
            for (int i = 0; i < _legacyCommandList.Count; i++)
            {
                var command = _legacyCommandList[i];
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
            for (byte i = 0; i < _legacyCommandList.Count; i++)
            {
                var command = _legacyCommandList[i];
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
        
        public FilteredState FilterState { get; set; }    //local cache of filteredness
        public bool IsControlSelected { get; set; } //local cache of being part of the control exclusion group

        #region Public members

        /// <summary>
        /// The execution state of the Block.
        /// </summary>
        public virtual ExecutionState State { get { return _executionState; } }

        /// <summary>
        /// Unique identifier for the Block (relative to the others in the same container).
        /// </summary>
        public virtual byte ItemId { get { return _itemId; } set { _itemId = value; } }

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
        public virtual IEventHandler EventHandler
        {
            get
            {
                if (_eventHandler == null && _legacyEventHandler != null)
                {
                    _eventHandler = _legacyEventHandler;
                }
                return _eventHandler;
            }
            set
            {
                _eventHandler = value;
                _legacyEventHandler = value as EventHandler;
            }
        }

        private IEventHandler _eventHandler;

        /// <summary>
        /// The currently executing command.
        /// </summary>
        public virtual ICommand ActiveCommand { get { return _activeCommand; } }

        /// <summary>
        /// Timer for fading Block execution icon.
        /// </summary>
        public virtual float ExecutingIconTimer { get; set; }

        /// <summary>
        /// The list of commands in the sequence.
        /// </summary>
        public virtual IList<ICommand> CommandList => _legacyCommandList.OfType<ICommand>().ToList();

        /// <summary>
        /// Controls the next command to execute in the block execution coroutine.
        /// </summary>
        public virtual int JumpToCommandIndex { set { _jumpToCommandIndex = value; } }

        public IReadOnlyList<ICommand> Commands => _legacyCommandList;

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

        /// <summary>
        /// Returns true if the Block is executing a Command.
        /// </summary>
        public virtual bool IsExecuting => _executionState == ExecutionState.Executing;

        /// <summary>
        /// Returns the number of times this Block has executed.
        /// </summary>
        public virtual int ExecutionCount
        {
            get { return _executionCount; }
            set { _executionCount = value; }
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
            GetFlowchart();
            var flowchart = _owner as Flowchart;

            _executionState = ExecutionState.Executing;
            BlockSignals.BlockExecStarted(this);

            bool suppressSelectionChanges = false;
            SelectTheExecutingBlockAndCommand(ref suppressSelectionChanges);
            
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

                _legacyCommandList.RemoveAll(cmd => cmd == null); 
                // Clean up any null entries that may be in the list
                // Skip disabled commands, comments and labels

                while (i < _legacyCommandList.Count && !_legacyCommandList[i].enabled)
                {
                    i = _legacyCommandList[i].CommandIndex + 1;
                }

                if (i >= _legacyCommandList.Count)
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

                var command = _legacyCommandList[i];
                _activeCommand = command;

                if (Selection.activeGameObject == flowchart.gameObject && 
                    flowchart.IsActive() && !suppressSelectionChanges)
                {
                    // Auto select a command in some situations
                    if ((flowchart.SelectedCommandCount == 0 && i == 0) ||
                        (flowchart.SelectedCommandCount == 1 && flowchart.SelectedCommands[0].CommandIndex == _previousActiveCommandIndex))
                    {
                        flowchart.ClearSelectedCommands();
                        flowchart.AddSelectedCommand(_legacyCommandList[i]);
                    }
                }

                command.IsExecuting = true;
                // This icon timer is managed by the FlowchartWindow class, but we also need to
                // set it here in case a command starts and finishes execution before the next window update.
                command.ExecutionIconTimer = Time.realtimeSinceStartup + HyphlowConstants.ExecutingIconFadeTime;
                BlockSignals.DoCommandExecute(this, command, i, _legacyCommandList.Count);

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
                FlowchartEditorQol editorQol = flowchart.EditorQol;
                float stepPause = editorQol != null ? 
                    editorQol.StepPause : 
                    0f;
                if (stepPause > 0f)
                {
                    yield return new WaitForSeconds(stepPause);
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

        private static readonly Type _commentType = typeof(Comment);
        private static readonly Type _labelType = typeof(Label);
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
        public virtual IList<IBlock> GetConnectedBlocks()
        {
            IList<IBlock> connectedBlocks = new List<IBlock>();
            RefreshConnectedBlockCache(ref connectedBlocks);
            return connectedBlocks;
        }

        void SelectTheExecutingBlockAndCommand(ref bool suppressSelectionChanges)
        {
#if UNITY_EDITOR
            var flowchart = _owner as Flowchart;
            if (suppressAllAutoSelections || SuppressNextAutoSelection)
            {
                SuppressNextAutoSelection = false;
                suppressSelectionChanges = true;
            }
            else if (Selection.activeGameObject == flowchart.gameObject)
            {
                flowchart.SelectedBlock = this;
                if (_legacyCommandList.Count > 0)
                {
                    flowchart.ClearSelectedCommands();
                    flowchart.AddSelectedCommand(_legacyCommandList[0]);
                }
            }
#endif
        }

        public virtual bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public virtual void RefreshConnectedBlockCache(ref IList<IBlock> toRefresh)
        {
            if (_legacyCommandList == null)
            {
                return;
            }
            for (int i = 0; i < _legacyCommandList.Count; i++)
            {
                var command = _legacyCommandList[i];
                if (command != null)
                {
                    command.GetConnectedBlocks(ref toRefresh);
                }
            }
        }

        protected IList<IBlock> _connectedBlocks = new List<IBlock>();

        /// <summary>
        /// Returns the type of the previously executing command.
        /// </summary>
        /// <returns>The previous active command type.</returns>
        public virtual Type GetPreviousActiveCommandType()
        {
            if (_previousActiveCommandIndex >= 0 &&
                _previousActiveCommandIndex < _legacyCommandList.Count)
            {
                return _legacyCommandList[_previousActiveCommandIndex].GetType();
            }

            return null;
        }

        public virtual int GetPreviousActiveCommandIndent()
        {
            if (_previousActiveCommandIndex >= 0 &&
                _previousActiveCommandIndex < _legacyCommandList.Count)
            {
                return _legacyCommandList[_previousActiveCommandIndex].IndentLevel;
            }

            return -1;
        }

        public virtual Command GetPreviousActiveCommand()
        {
            if (_previousActiveCommandIndex >= 0 &&
                _previousActiveCommandIndex < _legacyCommandList.Count)
            {
                return _legacyCommandList[_previousActiveCommandIndex];
            }

            return null;
        }

        /// <summary>
        /// Recalculate the indent levels for all commands in the list.
        /// </summary>
        public virtual void UpdateIndentLevels()
        {
            int indentLevel = 0;
            for (int i = 0; i < _legacyCommandList.Count; i++)
            {
                var command = _legacyCommandList[i];
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

            for (int i = 0; i < _legacyCommandList.Count; i++)
            {
                var command = _legacyCommandList[i];
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

        public bool Contains(ICommand cmd)
        {
            bool result = ReferenceEquals(_commandListDict[cmd.ItemId], cmd);
            return result;
        }

        public ICommand GetCommandWithId(byte id)
        {
            ICommand result = _commandListDict[id];
            return result;
        }

        public HideFlags HideFlags
        {
            get { return gameObject.hideFlags; }
            set { gameObject.hideFlags = value; }
        }

        Component IBlock.Owner
        {
            get => _owner as Component;
            set => Owner = value;
        }

        public FilteredState FilteredState
        {
            get => FilterState;
            set => FilterState = value;
        }
        object IHasItemId.ItemId 
        { 
            get => ItemId;
            set
            {
                if (value is byte b)
                {
                    ItemId = b;
                }
                else
                {
                    string errorMessage = $"Cannot set ItemId to a value of type " +
                        $"{value.GetType().Name}. Expected type: byte.";
                    throw new InvalidCastException(errorMessage);
                }
            }
        }

        /// <summary>
        /// Add a command to the end of the command list. If it already exists in the list, 
        /// it will not be added again.
        /// </summary>
        public bool Add(ICommand cmd, bool triggerSignals = true)
        {
            return Insert(cmd, (byte)_legacyCommandList.Count, triggerSignals);
        }

        public bool Insert(ICommand cmd, byte index, bool triggerSignals)
        {
            bool alreadyRegistered = _commandListDict.TryGetValue(cmd.ItemId, out ICommand existingCmd)
                && existingCmd == cmd;
            if (alreadyRegistered)
            {
                string warningMessage = $"Command with id {cmd.ItemId} is already " +
                    $"registered to block {BlockName}";
                Debug.LogWarning(warningMessage);
                return false;
            }

            EnsureValidIdFor(cmd);
            if (triggerSignals)
            {
                CommandSignals.PreCommandAdded(cmd, this);
            }
            cmd.ParentBlock = this;
            _commandListDict[cmd.ItemId] = cmd;
            Command legCommand = cmd as Command;
            if (legCommand != null)
            {
                _legacyCommandList.Insert(index, legCommand);
            }

            legCommand.CommandIndex = (byte)index;

            if (triggerSignals)
            {
                cmd.OnCommandAdded(this);
                CommandSignals.CommandAdded(cmd, this);
            }
            return true;
        }

        private void EnsureValidIdFor(ICommand cmd)
        {
            while (cmd.ItemId == InvalidId || _commandListDict.ContainsKey(cmd.ItemId))
            {
                cmd.ItemId = NextValidCommandId();
            }
        }

        private byte NextValidCommandId()
        {
            byte nextId = _nextValidCommandId;
            _nextValidCommandId++;
            if (_nextValidCommandId == InvalidId)
            {
                _nextValidCommandId++;
            }
            return nextId;
        }

        public virtual bool RemoveCommandWithId(byte id, bool triggerSignals = true)
        {
            ICommand toRemove = _commandListDict[id];
            bool successfulRemoval = Remove(toRemove, triggerSignals);
            return successfulRemoval;
        }

        public virtual bool Remove(ICommand cmd, bool triggerSignals = true)
        {
            bool alreadyRegistered = _commandListDict.TryGetValue(cmd.ItemId, out ICommand existingCmd)
                && existingCmd == cmd;
            if (alreadyRegistered)
            {
                return false;
            }

            if (triggerSignals)
            {
                CommandSignals.PreCommandRemoved(cmd, this);
            }

            Command legCommand = cmd as Command;
            bool successfulRemoval = _legacyCommandList.Remove(legCommand);
            if (successfulRemoval)
            {
                _legacyCommandList.Remove(legCommand);
                _commandListDict.Remove(cmd.ItemId);

                if (triggerSignals)
                {
                    cmd.OnCommandRemoved(this);
                    CommandSignals.CommandRemoved(cmd, this);
                }
                
            }
            return successfulRemoval;
        }

        public virtual bool RemoveAllCommands(bool triggerSignals = true)
        {
            bool anyToRemove = _legacyCommandList.Count > 0;
            while (_legacyCommandList.Count > 0)
            {
                var cmd = _legacyCommandList[0];
                Remove(cmd, triggerSignals);
            }
            return anyToRemove;
        }

        public void ResetCommands()
        {
            for (int i = 0; i < _legacyCommandList.Count; i++)
            {
                var command = _legacyCommandList[i];
                if (command != null)
                {
                    command.OnReset();
                }
            }
        }
    }
}
