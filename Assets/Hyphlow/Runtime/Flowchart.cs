using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AtMycelia.Hyphlow.UI;
using UnityEngine;
using UnityEngine.Serialization;
using AtMycelia.Hyphlow.EditorExt;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Hyphlow's main visual programming component. A Flowchart is a collection of 
    /// Blocks and Commands that define a program's logic.
    /// Flowchart objects may be edited visually using the Flowchart editor window.
    /// </summary>
    [ExecuteInEditMode]
    [MovedFrom(true, "AtMycelia.Hyphlow", "AtMycelia.Amanita.Core")]
    public class Flowchart : MonoBehaviour, IReorderableMuscariableSource,
        IForceResetUidHandler, ISerializationCallbackReceiver, ITearDownResponder, IRefreshable,
        IBackwardsCompatibilityApplier, IBlockSource, ICommandRemovable
    {
        [SerializeField, HideInInspector] private VariableManagerComponent _varManager;
        [SerializeField, HideInInspector] private BlockExecutionManagerComponent _execManager;
        [SerializeField, HideInInspector] private BlockManagerComponent _blockManager;

        [FormerlySerializedAs("variableManager")]
        [SerializeField, HideInInspector] private VariableManager legacyVariableManager = new VariableManager();

        [HideInInspector]
        [SerializeField] protected int version = 0; 
        // ^Default to 0 to always trigger an update for older versions of Hyphlow.

        [HideInInspector]
        [FormerlySerializedAs("variables")]
        [FormerlySerializedAs("legacyVariables")]
        [SerializeField] protected List<Variable> _legacyVariables = new List<Variable>();

        [HideInInspector]
        [FormerlySerializedAs("muscariables")]
        [SerializeReference] protected List<Muscariable> _oldMuscariables = new List<Muscariable>();

        [Tooltip("ScriptableObjects that contain settings that should apply to this Flowchart. " +
            "For example, how this Flowchart should handle Lua compatibility.")]
        [SerializeField] protected ScriptableObject[] _otherSettings = new ScriptableObject[0];

        [SerializeField] protected FlowchartEditorQol _flowchartSettings;

        public IReadOnlyList<ScriptableObject> OtherSettings => _otherSettings;

        public FlowchartEditorQol EditorQol => _flowchartSettings;


        /// <summary>
        /// Force reset the unique identifier for this Flowchart. Use with caution!
        /// </summary>
        public virtual void ForceResetUid()
        {
            this.UniqueId = Guid.NewGuid().ToString();
        }

#if UNITY_EDITOR

        // Locking this under #if UNITY_EDITOR to avoid unnecessary serialization in builds

        [TextArea(3, 5)]
        [Tooltip("Description text displayed in the Flowchart editor window")]
        [FormerlySerializedAs("description")]
        [SerializeField] protected string _description = "";

        /// <summary>
        /// What the editor utils should use to decide how to render this FC's data in the 
        /// FlowchartWindow and BlockInspector.
        /// </summary>
        public virtual FlowchartUIModel UIModel
        {
            get { return _uiModel; }
        }

        [HideInInspector]
        [SerializeField]
        [FormerlySerializedAs("uiModel")]
        protected FlowchartUIModel _uiModel = new FlowchartUIModel();

#endif

        #region Save Sys Involvement
        [Tooltip("Whether or not the save system should save (and when appropriate, load) this Flowchart's variables.")]
        [FormerlySerializedAs("includeInSaves")]
        [SerializeField] protected bool _includeInSaves = true;

        [Tooltip("Whether or not the execution state of this FC's Blocks should be considered for saving.")]
        [FormerlySerializedAs("saveBlocks")]
        [SerializeField] protected bool _saveBlocks = true;

        [Tooltip("Whether or not this FC's vars should be saved or loaded.")]
        [FormerlySerializedAs("saveVariables")]
        [SerializeField] protected bool _saveVariables = true;

        [Tooltip("Affects the order this FC will get loaded relative to others. Lower number, earlier loading.")]
        [FormerlySerializedAs("loadPriority")]
        [SerializeField] protected int _loadPriority = 0;
        #endregion

        [FormerlySerializedAs("alwaysKeepGuid")]
        [SerializeField] private bool _alwaysKeepGuid = true;

        public virtual bool IncludeInSaves
        {
            get { return _includeInSaves; }
            set { _includeInSaves = value; }
        }

        #region SaveSys Involvement
        public virtual bool SaveBlocks
        {
            get { return _saveBlocks; }
            set { _saveBlocks = value; }
        }

        public virtual bool SaveVariables
        {
            get { return _saveVariables; }
            set { _saveVariables = value; }
        }

        public virtual int LoadPriority
        {
            get { return _loadPriority; }
            set { _loadPriority = value; }
        }
        #endregion

        protected StringSubstituter _stringSubstituter;

        public IReadOnlyList<IBlock> Blocks
        {
            get
            {
                // Refresh if cache is empty or contains null entries.
                EnsureBlockManagerComponent();
                Block nullEntry = null;
                if (_blockManager.BlockCount == 0 || _blockManager.Contains(nullEntry))
                {
                    RefreshBlockAndCommandCache();
                }

                return _blockManager.Blocks;
            }
        }
        public IReadOnlyList<ICommand> Commands => _blockManager.Commands;

        protected virtual void Awake()
        {
            EnsureSubmanagerComponents();

            RegisterLegacyVars();
            void RegisterLegacyVars()
            {
                _legacyVariables ??= new List<Variable>();
                if (_legacyVariables.Count == 0)
                {
                    var found = GetComponents<Variable>();
                    _legacyVariables.AddRange(found);
                }
            }

            AssertOwnership();
            RefreshBlockAndCommandCache();
            RefreshBlocks();
            _varManager.Refresh();
#if UNITY_EDITOR
            UIModel.Owner = this.gameObject;
            EditorUtility.SetDirty(this);
#endif

        }

        private void RefreshBlockAndCommandCache()
        {
            EnsureSubmanagerComponents();
            _blockManager.Owner = this;
            _blockManager.Refresh();
        }

        private void RefreshBlocks()
        {
            _blockManager.Refresh();
        }

        protected virtual void Start()
        {
            if (Application.IsPlaying(this))
            {
                StartCoroutine(HandleGameStartedBlocks());
            }
        }

        protected virtual IEnumerator HandleGameStartedBlocks()
        {
            IList<GameStarted> gsEventHandler = GetComponents<GameStarted>();

            if (gsEventHandler.Count == 0)
            {
                yield break;
            }

            foreach (var elem in gsEventHandler)//
            {
                elem.Trigger();
            }
        }

        protected virtual void OnEnable()
        {
            if (!this.IsInTheScene)
            {
                // Don't do anything if this isn't even in the scene yet
                return;
            }

            Refresh();
            ToggleSubs(true);

            FlowchartSignals.FlowchartEnabled(this);
        }

        private void ToggleSubs(bool on)
        {
            if (_varManager == null)
            {
                return;
            }
            if (on)
            {
                _varManager.VariableAdded += OnVarAdded;
                _varManager.VariableRemoved += OnVarRemoved;
            }
            else
            {
                _varManager.VariableAdded -= OnVarAdded;
                _varManager.VariableRemoved -= OnVarRemoved;
            }
        }

        private void OnVarAdded(IVariable added)
        {
            FlowchartSignals.VariableAdded(this, added);
        }

        public event Action<IVariable> VariableAdded
        {
            add
            {
                _varManager.VariableAdded += value;
            }
            remove
            {
                _varManager.VariableAdded -= value;
            }
        }

        private void OnVarRemoved(IVariable removed)
        {
            FlowchartSignals.VariableRemoved(this, removed);
        }

        public event Action<IVariable> VariableRemoved
        {
            add
            {
                _varManager.VariableRemoved += value;
            }
            remove
            {
                _varManager.VariableRemoved -= value;
            }
        }

        public int VariableCount
        {
            get
            {
                if (_varManager == null)
                {
                    _varManager = gameObject.GetOrAddComponent<VariableManagerComponent>();
                }

                return _varManager.Variables.Count;
            }
        }

        private bool IsInTheScene
        {
            get
            {
                if (gameObject == null || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
                {
                    return false;
                }

#if UNITY_EDITOR
                //return PrefabStageUtility.GetPrefabStage(gameObject) == null;
                return true;
#else
        return true;
#endif
            }
        }

        public virtual void Refresh()
        {
            EnsureSubmanagerComponents();

            AssertUniqueID();
            AssertOwnership();//
#if UNITY_EDITOR
            RefreshEditorCaches();
#endif
            CleanupComponents();
            UpdateVersion();
        }

#if UNITY_EDITOR
        private void RefreshEditorCaches()
        {
            if (Application.IsPlaying(this))
            {
                return;
            }

            RefreshBlockAndCommandCache();
        }
#endif

        public IVariableManager VariableManager
        {
            get
            {
                if (_varManager != null)
                {
                    return _varManager;
                }
                else
                {
                    return legacyVariableManager;
                }
            }
        }

        public IBlockManager BlockManager
        {
            get
            {
                EnsureBlockManagerComponent();
                return _blockManager;
            }
        }

        public IBlockExecutionManager BlockLogicManager
        {
            get
            {
                EnsureBlockLogicManagerComponent();
                return _execManager;
            }
        }

        protected virtual void AssertOwnership()
        {
            _varManager.Owner = this;
            // Legacy variables automatically get their owner-registration done;
            // it's always the Flowchart they're attached to.

            _blockManager.Owner = this;

            _execManager.Owner = this;
        }

        private void EnsureSubmanagerComponents()
        {
            EnsureVariableManagerComponent();
            EnsureBlockManagerComponent();
            EnsureBlockLogicManagerComponent();
        }

        private void EnsureBlockLogicManagerComponent()
        {
            if (_execManager == null)
            {
                _execManager = GetComponent<BlockExecutionManagerComponent>();
                if (_execManager == null)
                {
                    _execManager = gameObject.AddComponent<BlockExecutionManagerComponent>();
#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
#endif
                }
            }
        }

        private void EnsureBlockManagerComponent()
        {
            if (_blockManager != null)
            {
                return;
            }

            _blockManager = GetComponent<BlockManagerComponent>();
            if (_blockManager == null)
            {
                _blockManager = gameObject.AddComponent<BlockManagerComponent>();
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        protected virtual void OnDisable()
        {
            ToggleSubs(false);
            StopAllBlocks();
            StopAllCoroutines();
            FlowchartSignals.FlowchartDisabled(this);
        }

        protected virtual void OnDestroy()
        {
            FlowchartSignals.FlowchartDestroyed(this);
        }

        protected virtual void UpdateVersion()
        {
            if (version == HyphlowConstants.CurrentVersion)
            {
                // No need to update
                return;
            }

            // Tell all components that implement IUpdateable to update to the new version
            // This is important for when we rework Variables and Blocks to be more lightweight;
            // might want to make the old var and Block types IUpdatables
            var components = GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                IUpdateable toUpdate = component as IUpdateable;
                toUpdate?.UpdateToVersion(version, HyphlowConstants.CurrentVersion);
            }

            version = HyphlowConstants.CurrentVersion;
        }

        [HideInInspector]
        [SerializeField] protected byte nextValidVarID = 1;

        protected virtual void CleanupComponents()
        {
            _legacyVariables.RemoveAll(item => item == null);

            RepairEventHandlerLinks();

            #region Destroy EventHandlers that aren't on any Blocks
            var eventHandlers = GetComponents<EventHandler>();
            for (int i = 0; i < eventHandlers.Length; i++)
            {
                var eventHandler = eventHandlers[i];
                bool found = false;
                foreach (IBlock block in _blockManager.Blocks)
                {
                    if (block != null && ReferenceEquals(block.EventHandler, eventHandler))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    DestroyImmediate(eventHandler);
                }
            }
            #endregion
        }

        private void RepairEventHandlerLinks()
        {
            var eventHandlers = GetComponents<EventHandler>();

            for (int i = 0; i < eventHandlers.Length; i++)
            {
                var eventHandler = eventHandlers[i];
                if (eventHandler == null)
                {
                    continue;
                }

                IBlock parentBlock = eventHandler.ParentBlock;
                if (parentBlock != null &&
                    parentBlock.GetFlowchart() == this &&
                    !ReferenceEquals(parentBlock.EventHandler, eventHandler))
                {
                    parentBlock.EventHandler = eventHandler;
                }
            }

            foreach (IBlock blockEl in Blocks)
            {
                if (blockEl == null || blockEl.EventHandler != null)
                {
                    continue;
                }

                for (int i = 0; i < eventHandlers.Length; i++)
                {
                    var eventHandler = eventHandlers[i];
                    if (eventHandler != null && ReferenceEquals(eventHandler.ParentBlock, blockEl))
                    {
                        blockEl.EventHandler = eventHandler;
                        break;
                    }
                }
            }
        }

        #region Public members

#if UNITY_EDITOR
        #region Flowchart UI State and Methods

        public bool SelectedCommandsStale
        {
            get => UIModel.SelectedCommandsStale;
            set => UIModel.SelectedCommandsStale = value;
        }

        /// <summary>
        /// Scroll position of Flowchart editor window.
        /// </summary>
        public virtual Vector2 ScrollPos
        {
            get => _uiModel.ScrollPos;
            set => _uiModel.ScrollPos = value;
        }

        public virtual float Zoom
        {
            get => _uiModel.Zoom;
            set => _uiModel.Zoom = value;
        }

        /// <summary>
        /// Scrollable area for Flowchart editor window.
        /// </summary>
        public virtual Rect ScrollViewRect
        {
            get => _uiModel.ScrollViewRect;
            set => _uiModel.ScrollViewRect = value;
        }

        /// <summary>
        /// Current actively selected block in the Flowchart editor.
        /// </summary>
        public virtual IBlock SelectedBlock
        {
            get => _uiModel.SelectedBlock;
            set => _uiModel.SelectedBlock = value;
        }

        public virtual IList<IBlock> SelectedBlocks
        {
            get => _uiModel.SelectedBlocks;
            set => _uiModel.SelectedBlocks = value;
        }

        /// <summary>
        /// Currently selected command in the Flowchart editor.
        /// </summary>
        public virtual IList<ICommand> SelectedCommands
        {
            get => _uiModel.SelectedCommands; // Returns a copy
            set => _uiModel.SelectedCommands = value;
        }

        public virtual int SelectedCommandCount
        {
            get { return _uiModel.CommandCount; }
        }

        public virtual int SelectedBlockCount
        {
            get { return _uiModel.BlockCount; }
        }

        public virtual void UpdateSelectedCache()
        {
            SelectedBlocks.Clear();
            var res = gameObject.GetComponents<IBlock>();
            SelectedBlocks = res.Where(x => x.IsSelected).ToList();
        }

        public virtual void ReverseUpdateSelectedCache()
        {
            for (int i = 0; i < SelectedBlockCount; i++)
            {
                if (SelectedBlocks[i] != null)
                {
                    SelectedBlocks[i].IsSelected = true;
                }
            }
        }

        /// <summary>
        /// Clears the list of selected blocks.
        /// </summary>
        public virtual void ClearSelectedBlocks()
        {
            IList<IBlock> blocksToSignal = SelectedBlocks;
            UIModel.ClearSelectedBlocks();
        }

        public virtual void AddRangeToSelection(IList<IBlock> toSelect)
        {
            UIModel.AddRangeToSelection(toSelect);
        }

        /// <summary>
        /// Adds a block to the list of selected blocks.
        /// </summary>
        public virtual void AddToSelection(IBlock block) => UIModel.AddToSelection(block);

        public virtual void DeselectBlockNoCheck(IBlock toDeselect) => UIModel.Deselect(toDeselect);

        public virtual void DeselectAll()
        {
            UIModel.ClearSelectedBlocks();
            UIModel.ClearSelectedCommands();
        }

        /// <summary>
        /// Clears the list of selected commands.
        /// </summary>
        public virtual void ClearSelectedCommands()
        {
            UIModel.ClearSelectedCommands();
#if UNITY_EDITOR
            SelectedCommandsStale = true;
#endif
        }

        /// <summary>
        /// Adds a command to the list of selected commands.
        /// </summary>
        public virtual void AddSelectedCommand(ICommand command)
        {
            if (!_uiModel.Contains(command))
            {
                // The SelectedCommands getter returns a defensive decoy. Thus, rather than something
                // like SelectedCommands.Add, we call the ui model's method specifically for registering
                // Commands.
                UIModel.AddToSelection(command);
#if UNITY_EDITOR
                SelectedCommandsStale = true;
#endif
                SelectedCommandAdded(command);
            }
        }

        /// <summary>
        /// For when added through AddSelectedCommand (as opposed to just setting 
        /// the SelectedCommands property or such)
        /// </summary>
        public event Action<ICommand> SelectedCommandAdded = delegate { };

        #endregion
#endif

        /// <summary>
        /// Description text displayed in the Flowchart editor window
        /// </summary>
        public virtual string Description { get { return _description; } }

        /// <summary>
        /// Position in the center of all blocks in the flowchart.
        /// </summary>
        public virtual Vector2 CenterPosition { set; get; }

        /// <summary>
        /// Variable to track flowchart's version so components can update to new versions.
        /// </summary>
        public int Version { set { version = value; } }

        /// <summary>
        /// Returns true if the Flowchart gameobject is active.
        /// </summary>
        public bool IsActive()
        {
            return gameObject.activeInHierarchy;
        }


        #region Block-Handling

        /// <summary>
        /// Create a new block node which you can then add Commands to.
        /// </summary>
        public virtual IBlock CreateBlock(Vector2 position, string blockName = null)
        {
            EnsureSubmanagerComponents();
            return _blockManager.CreateBlock(position, blockName);
        }

        public virtual IList<IBlock> CreateMultiBlocks(IList<Vector2> positions)
        {
            return _blockManager.CreateMultiBlocks(positions);
        }

        /// <summary>
        /// Returns the named Block in the flowchart, or null if not found.
        /// </summary>
        public virtual IBlock GetBlock(string blockName)
        {
            EnsureSubmanagerComponents();
            return _blockManager.GetBlock(blockName);
        }

        public virtual IBlock GetBlock(byte itemId)
        {
            EnsureSubmanagerComponents();
            return _blockManager.GetBlock(itemId);
        }

        /// <summary>
        /// Execute a child block in the Flowchart.
        /// You can use this method in a UI event. e.g. to handle a button click.
        public virtual void ExecuteBlock(string blockName)
        {
            EnsureSubmanagerComponents();
            _execManager.ExecuteBlock(blockName);
        }

        /// <summary>
        /// Execute a child block in the flowchart.
        /// The block must be in an idle state to be executed.
        /// This version provides extra options to control how the block is executed.
        /// Returns true if the Block started execution.            
        /// </summary>
        public virtual bool ExecuteBlock(IBlock block, int commandIndex = 0, Action onComplete = null)
        {
            EnsureSubmanagerComponents();
            return _execManager.ExecuteBlock(block, commandIndex, onComplete);
        }

        /// <summary>
        /// Stops an executing Block in the Flowchart.
        /// </summary>
        public virtual void StopBlock(string blockName)
        {
            EnsureSubmanagerComponents();
            _execManager.StopBlock(blockName);
        }

        /// <summary>
        /// Stop all executing Blocks in this Flowchart.
        /// </summary>
        public virtual void StopAllBlocks()
        {
            EnsureSubmanagerComponents();
            _execManager.StopAllBlocks();
        }

        protected static FlowchartGlobalDefaults GlobalDefaults => FlowchartGlobalDefaults.S;
        #endregion

        #region Variable-Handling

        /// <summary>
        /// Reorders the legacy Variable list to match the sequence supplied (only
        /// for those Variables already registered). Muscariables are not affected.
        /// Variables not present in newOrder retain their relative order at the end.
        /// Does not raise add/remove events (pure reordering).
        /// </summary>
        public virtual void ReorderVariables(IList<IVariable> newOrder)
        {
            EnsureSubmanagerComponents();
            VariableManager.ReorderVariables(newOrder);
        }

        #endregion

        /// <summary>
        /// Reset the Commands and Variables in the Flowchart.
        /// </summary>
        public virtual void ResetFlowchart(bool resetCommands, bool resetVariables)
        {
            EnsureSubmanagerComponents();
            if (resetCommands)
            {
                _blockManager.ResetCommands();
            }

            if (resetVariables)
            {
                VariableManager.ResetAllVars();
            }
        }

        /// <summary>
        /// Returns true if there are any executing blocks in this Flowchart.
        /// </summary>
        public virtual bool HasExecutingBlocks()
        {
            EnsureSubmanagerComponents();
            return _execManager.HasExecutingBlocks();
        }

        #endregion

        [HideInInspector]
        [SerializeField] private string _uniqueId = string.Empty;
        /// <summary>
        /// Unique identifier not specific to localization. Don't assign to this 
        /// unless you know what you're doing.
        /// </summary>
        public string UniqueId
        {
            get => _uniqueId;
            set
            {
                if (!string.IsNullOrEmpty(_uniqueId))
                {
                    Debug.LogWarning($"Assigning a new unique ID to {this.name}, a " +
                        $"Flowchart that already has one. Old ID: {_uniqueId}, New ID: " +
                        $"{value}. If this was intentional, make sure you know what " +
                        $"you're doing.");
                }

                _uniqueId = value;
            }
        }

        private void OnValidate()
        {
            if (!this.IsInTheScene || Application.isPlaying)
            {
                // Don't do anything if this isn't even in the scene yet
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (this == null) // Object may have been destroyed
                {
                    return;
                }

                EnsureSubmanagerComponents();

                _legacyVariables.RemoveAll((elem) => elem == null);
                _oldMuscariables.RemoveAll((elem) => elem == null);

                _uiModel ??= new FlowchartUIModel();
                if (_uiModel.Owner == null)
                {
                    _uiModel.Owner = this.gameObject;
                }

                Refresh();

            };

        }

        protected virtual void AssertUniqueID()
        {
            if (string.IsNullOrEmpty(_uniqueId))
            {
                UniqueId = Guid.NewGuid().ToString();
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public virtual bool AlwaysKeepGuid
        {
            get
            {
                return _alwaysKeepGuid;
            }
            set
            {
                _alwaysKeepGuid = value;
            }
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public virtual IReadOnlyList<IVariable> Variables
        {
            get
            {
#if UNITY_EDITOR
                if (this == null) // Possible in unit tests
                {
                    return Array.Empty<IVariable>();
                }
#endif
                EnsureSubmanagerComponents();
                _varManager.Owner = this;
                return VariableManager.Variables;
            }
        }

        IReadOnlyList<Muscariable> IVariableSource<Muscariable>.Variables => 
            ((IMuscariableSource)_varManager).Variables;

        private void EnsureVariableManagerComponent()
        {
            if (_varManager != null)
            {
                return;
            }

            _varManager = gameObject.GetComponent<VariableManagerComponent>();
            if (_varManager == null)
            {
                _varManager = gameObject.AddComponent<VariableManagerComponent>();
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

#if UNITY_EDITOR

        public static void ResetStaticsForTest()
        {
        }

        public virtual void OnTearDown()
        {
        }

#endif

        public bool Contains(IVariable var)
        {
            return _varManager.Contains(var);
        }

        public virtual void OnBeforeSerialize()
        {
        }

        public virtual void OnAfterSerialize()
        {
        }


#if UNITY_EDITOR
        public T AddCommand<T>(IBlock toAddTo) where T : Command
        {
            return AddCommand(typeof(T), toAddTo) as T;
        }

        public ICommand AddCommand(Type commandType, IBlock toAddTo)
        {
            if (!_iCommandType.IsAssignableFrom(commandType))
            {
                Debug.LogError($"AddCommand: {commandType} does not inherit from Command.");
                return null;
            }

            Undo.RecordObject(this, $"Add {commandType.Name} Command");
            Undo.RecordObject(this.gameObject, $"Add {commandType.Name} Command Component");

            var cmdAdded = Undo.AddComponent(this.gameObject, commandType) as Command;
            if (cmdAdded == null)
            {
                Debug.LogError($"AddCommand: Failed to add component of type {commandType}.");
                return null;
            }

            // Route through block manager so command cache stays authoritative.
            bool success = toAddTo.Add(cmdAdded, false);
            if (!success)
            {
                Debug.LogError($"AddCommand: Failed to register command {cmdAdded.Name} in block manager.");
                return null;
            }

            EditorUtility.SetDirty(this);
            return cmdAdded;
        }

        private static readonly Type _iCommandType = typeof(ICommand);

        /// <summary>
        /// For editor operations only. Removes the blocks from the list of blocks in the flowchart,
        /// without destroying them. This is used for operations like deleting multiple blocks, where we
        /// want to remove the blocks from the flowchart's list of blocks before destroying them,
        /// to avoid null references in the flowchart's list of blocks.
        /// 
        /// Returns true if any blocks were removed, false if the input list was null or empty.
        /// </summary>
        public virtual bool RemoveMultiBlocks(IList<IBlock> toUnregister)
        {
            bool success = false;
            for (int i = 0; i < toUnregister.Count; i++)
            {
                success = success | Remove(toUnregister[i]);
            }
            return success;
        }

        public virtual void ApplyBackwardsCompatibility()
        {
        }

        public virtual void OnAfterDeserialize()
        {

        }
#endif

        public virtual IVariable AddVariable(IVariable toAdd)
        {
            return VariableManager.AddVariable(toAdd);
        }

        public virtual void RemoveVariable(IVariable toRemove)
        {
            VariableManager.RemoveVariable(toRemove);
        }

        public IVariable GetVariable(byte itemID)
        {
            return VariableManager.GetVariable(itemID);
        }

        public virtual void ClearVariables()
        {
            _varManager.Clear();
        }

        public Muscariable AddVariable(Muscariable toAdd)
        {
            return _varManager.AddVariable(toAdd);
        }

        public virtual void RemoveVariable(Muscariable toRemove)
        {
            _varManager.RemoveVariable(toRemove);
        }

        T IVariableSource.GetVariableOfType<T>()
        {
            return VariableManager.GetVariableOfType<T>();
        }

        public IVariable GetVariable(string name, StringComparison strCompare = StringComparison.Ordinal)
        {
            return VariableManager.GetVariable(name, strCompare);
        }

        T IVariableSource.GetVariableOfType<T>(string name, StringComparison strCompare)
        {
            return VariableManager.GetVariableOfType<T>(name, strCompare);
        }

        public IVariable GetVariableOfType(Type type, string name, 
            StringComparison strCompare = StringComparison.Ordinal)
        {
            return VariableManager.GetVariableOfType(type, name, strCompare);
        }

        public virtual TVarType AddNewMuscariable<TContentType, TVarType>(string key,
            TContentType defaultValue = default,
            AccessScope scope = AccessScope.Private)
            where TVarType : Muscariable<TContentType>, new()
        {
            var result = _varManager.AddNewVariableOfContentType(typeof(TContentType), key) as TVarType;
            if (result != null)
            {
                result.Scope = scope;
                result.Init(defaultValue);
            }
            return result;
        }

        public virtual IVariable<TContentType> AddNewVariable<TContentType>(string key,
            TContentType defaultValue = default,
            AccessScope scope = AccessScope.Private)
        {
            var result = _varManager.AddNewVariableOfContentType(typeof(TContentType), key) 
                as IVariable<TContentType>;
            if (result != null)
            {
                result.Value = defaultValue;
                result.Scope = scope;
            }
            return result;
        }

        public Muscariable AddNewVariableOfContentType<TContentType>(string k, TContentType defaultVal,
            AccessScope scope = AccessScope.Private)
        {
            return _varManager.AddNewVariableOfContentType(k, defaultVal, scope);
        }

        public Muscariable AddNewVariableOfContentType(Type contentType, string key)
        {
            return _varManager.AddNewVariableOfContentType(contentType, key);
        }

        public bool Remove(IBlock block, bool triggerSignals = true)
        {
            EnsureSubmanagerComponents();
            return _blockManager.Remove(block, triggerSignals);
        }

        public bool RemoveBlockWithId(byte id, bool triggerSignals = true)
        {
            EnsureSubmanagerComponents();
            return _blockManager.RemoveBlockWithId(id, triggerSignals);
        }

        public bool Contains(ICommand cmd)
        {
            EnsureSubmanagerComponents();
            return _blockManager.Contains(cmd);
        }

        public ICommand GetCommandWithId(byte id)
        {
            EnsureSubmanagerComponents();
            return _blockManager.GetCommandWithId(id);
        }
        
        public bool Remove(ICommand cmd, bool triggerSignals = true)
        {
            EnsureSubmanagerComponents();
            return _blockManager.Remove(cmd, triggerSignals);
        }
            
        /// <summary>
        /// Removes all Commands from this Flowchart. Returns true
        /// if any Commands were removed, false if there weren't any to remove.
        /// </summary>
        public virtual bool RemoveAllCommands(bool triggerSignals = true)
        {
            EnsureSubmanagerComponents();
            return _blockManager.RemoveAllCommands(triggerSignals);
        }

        public bool RemoveCommandWithId(byte id, bool triggerSignals = true)
        {
            EnsureSubmanagerComponents();
            return _blockManager.RemoveCommandWithId(id, triggerSignals);
        }

        public bool ClearBlocks(bool triggerSignals)
        {
            EnsureSubmanagerComponents();
            return _blockManager.ClearBlocks(triggerSignals);
        }

        public bool Contains(IBlock block)
        {
            EnsureSubmanagerComponents();
            return _blockManager.Contains(block);
        }

        public bool Add(IBlock block, bool triggerSignals) => _blockManager.Add(block, triggerSignals);

    }
    
}
