using System;
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
    /// Execution state of a Block or Command.
    /// </summary>
    public enum ExecutionState
    {
        Idle,       
        Executing,
    }

    /// <summary>
    /// A container for a sequence of Hyphlow comands.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Flowchart))]
    [AddComponentMenu("")]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Block : Node, IBlock, IEquatable<IBlock>, ICommandSource, IRefreshable, IHasKey,
        ISerializationCallbackReceiver
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

        [SerializeField, HideInInspector] protected byte _nextValidCommandId = 1; 
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

        private ExecutionState _executionState;

        private Command _activeCommand;

        private Action _lastOnCompleteAction;

        public virtual Flowchart ParentFlowchart
        {
            get
            {
                if (this == null)
                {
                    return null;
                }

                return _owner as Flowchart;
            }
        }

        /// <summary>
        // Index of last command executed before the current one.
        // -1 indicates no previous command.
        /// </summary>
        private int _previousActiveCommandIndex = -1;

        public virtual int PreviousActiveCommandIndex
        {
            get { return _previousActiveCommandIndex; } 
            set { _previousActiveCommandIndex = value; }
        }

        private int _jumpToCommandIndex = -1;

        private int _executionCount;

        public virtual ExecutionState ExecutionState
        {
            get { return _executionState; }
            set
            {
                if (_executionState == value)
                {
                    return;
                }
                ExecutionState prevState = _executionState;
                _executionState = value;

                if (prevState == ExecutionState.Executing)
                {
                    OnReturnToIdle();
                }
                if (_executionState == ExecutionState.Executing)
                {
                    ExecStarted(this);
                    BlockSignals.BlockExecStarted(this);
                }
                else if (_executionState == ExecutionState.Idle)
                {
                    ExecEnded(this);
                    BlockSignals.BlockExecEnded(this);
                }
            }
        }

        /// <summary>
        /// If set, flowchart will not auto select when it is next executed, 
        /// used by eventhandlers. Only affects the editor.
        /// </summary>
        public virtual bool SuppressNextAutoSelection { get; set; } = true;

        [SerializeField] bool suppressAllAutoSelections = true;
        
        protected virtual void Awake()
        {
            _owner = GetComponent<Flowchart>();
            Refresh();
        }

        protected virtual void OnEnable()
        {
            Refresh();
        }

        public virtual void Refresh()
        {
            AssertOwnershipAndUpdateIndexes();
            RefreshCommandListDict();
            RefreshCommands();
            UpdateIndentLevels();
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

            if (EventHandler != null)
            {
                EventHandler.ParentBlock = this;
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

        private readonly IDictionary<byte, ICommand> _commandListDict = new Dictionary<byte, ICommand>();
        // ^This is used to speed up lookup of Commands by their unique Ids, which
        // things such as the editor may want to do frequently.

        private void RefreshCommands()
        {
            _legacyCommandList.RemoveAll(cmd => cmd == null);
            for (int i = 0; i < _legacyCommandList.Count; i++)
            {
                var command = _legacyCommandList[i];
                    command.Refresh();
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
        public virtual bool IsSelected { get; set; }    //local cache of selectedness
        
        public virtual FilteredState FilterState { get; set; } //local cache of filteredness
        public virtual bool IsControlSelected { get; set; } //local cache of being part of the control exclusion group

        #region public virtual members

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
                _eventHandler.ParentBlock = this;
                _legacyEventHandler = value as EventHandler;
            }
        }

        private IEventHandler _eventHandler;

        /// <summary>
        /// The currently executing command.
        /// </summary>
        public virtual ICommand ActiveCommand
        {
            get { return _activeCommand; }
            set { _activeCommand = value as Command; }
        }

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
        public virtual int NextExecCmdIndex
        {
            get { return _jumpToCommandIndex; }
            set { _jumpToCommandIndex = value; }
        }

        public virtual IReadOnlyList<ICommand> Commands => _legacyCommandList;

        /// <summary>
        /// Returns the parent Flowchart for this Block.
        /// </summary>
        public virtual Flowchart GetFlowchart()
        {
            if (this == null)
            {
                return null;
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

        private void OnReturnToIdle()
        {
            _executionState = ExecutionState.Idle;
            _activeCommand = null;
            ExecEnded(this);
            BlockSignals.BlockExecEnded(this);

            _lastOnCompleteAction?.Invoke();
            _lastOnCompleteAction = delegate { };
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
            OnReturnToIdle();
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

        private IList<IBlock> _connectedBlocks = new List<IBlock>();

        public virtual event Action<IBlock> ExecStarted = delegate { };
        public virtual event Action<IBlock> ExecEnded = delegate { };

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

        public virtual bool Equals(IBlock other)
        {
            return this != null && other != null &&
                this._itemId == other.ItemId &&
                this.ParentFlowchart.UniqueId == other.ParentFlowchart.UniqueId &&
                this.BlockName == other.BlockName;
        }

        public virtual bool Contains(ICommand cmd)
        {
            _commandListDict.TryGetValue(cmd.ItemId, out ICommand foundCmd);
            bool result = ReferenceEquals(foundCmd, cmd);
            return result;
        }

        public virtual ICommand GetCommandWithId(byte id)
        {
            ICommand result = _commandListDict[id];
            return result;
        }

        public virtual HideFlags HideFlags
        {
            get { return hideFlags; }
            set { hideFlags = value; }
        }

        Component IBlock.Owner
        {
            get => _owner as Component;
            set => Owner = value;
        }

        public virtual FilteredState FilteredState
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
        public virtual bool Add(ICommand cmd, bool triggerSignals = true)
        {
            return Insert(cmd, (byte)_legacyCommandList.Count, triggerSignals);
        }

        public virtual bool Insert(ICommand cmd, byte index, bool triggerSignals)
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
            #region Wrap around as needed
            if (_nextValidCommandId == InvalidId)
            {
                _nextValidCommandId = 1;
            }
            #endregion

            byte nextId = _nextValidCommandId;
            _nextValidCommandId++;
            
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

        public virtual void ResetCommands()
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

        public virtual void OnBeforeSerialize()
        {
        }

        protected virtual void OnValidate()
        {
            hideFlags = HideFlags.HideInInspector;
#if UNITY_EDITOR
            if (_owner == null)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null)
                    {
                        return;
                    }
                    _owner = GetComponent<Flowchart>();
                };
            }
#endif
        }

        public virtual void OnAfterDeserialize()
        {
        }

        public override string ToString()
        {
            string result = $"Block: {BlockName} (Id: {ItemId})";

            if (this.ParentFlowchart != null)
            {
                result += $" in Flowchart: {this.ParentFlowchart.name}";
            }
            return result;
        }
    }
}
