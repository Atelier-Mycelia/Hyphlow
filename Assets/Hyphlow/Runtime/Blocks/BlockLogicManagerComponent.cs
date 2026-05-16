using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AtMycelia.Hyphlow
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class BlockLogicManagerComponent : MonoBehaviour, IBlockSource, ICommandSource,
        IRefreshable, IDisposable, IBlockLogicHandler, IBlockCreator
    {
        [SerializeField, HideInInspector] private MonoBehaviour _owner;
        [SerializeField, HideInInspector] private BlockLogicManager _manager = new BlockLogicManager();

        public virtual MonoBehaviour Owner
        {
            get
            {
                if (_owner == null)
                {
                    _owner = this;
                }
                return _owner;
            }
            set
            {
                _owner = value;
                _manager.Owner = _owner;
            }
        }

        public virtual string Name
        {
            get => name;
            set => name = value;
        }

        public virtual IReadOnlyList<Block> Blocks => _manager.Blocks;
        public virtual IReadOnlyList<Command> Commands => _manager.Commands;
        public virtual IReadOnlyDictionary<ushort, Block> BlockLookup => _manager.BlockLookup;

        protected virtual void Awake()
        {
            Refresh();
        }

        protected virtual void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            EnsureOwner();
            _manager.Owner = _owner;
            _manager.RefreshBlockAndCommandCache();
            _manager.RefreshBlocks();
        }

        private void EnsureOwner()
        {
            if (_owner != null)
            {
                return;
            }
        }

        public ushort NextItemId()
        {
            return _manager.NextItemId();
        }

        public Block CreateBlock(Vector2 position, string blockName = null)
        {
            bool creatingFirstBlock = _manager.Blocks.Count == 0;
            var config = FlowchartDefaultConfig.S;

            if (creatingFirstBlock)
            {
                blockName ??= config.FirstBlockName;
            }
            else
            {
                blockName ??= config.NewBlockName;
            }

            Block created = gameObject.AddComponent<Block>();
#if UNITY_EDITOR
            created._NodeRect = new Rect(position, new Vector2(300, 100));
#endif
            created.BlockName = UniqueKeyGenerator.GetUniqueKeyFor<Block>(blockName, _manager.Blocks, created);

            created.Scope = config.NewBlockScope;
            created.ItemId = _manager.NextItemId();
            _manager.Add(created);

            if (creatingFirstBlock)
            {
                ApplyConfiguredEventHandlerToFirstBlock(created);
            }

            BlockSignals.BlockCreated(created);
            return created;
        }

        public IList<Block> CreateMultiBlocks(IList<Vector2> positions)
        {
            IList<Block> blocksCreated = new Block[positions.Count];
            for (int i = 0; i < positions.Count; i++)
            {
                blocksCreated[i] = CreateBlock(positions[i]);
            }

            return blocksCreated;
        }

        public void ApplyDefaultConfigToFirstBlock()
        {
            _manager.RefreshBlockAndCommandCache();
            if (_manager.Blocks.Count == 0)
            {
                return;
            }

            Block firstBlock = null;
            for (int i = 0; i < _manager.Blocks.Count; i++)
            {
                var elem = _manager.Blocks[i];
                if (elem == null)
                {
                    continue;
                }

                if (firstBlock == null || elem.ItemId < firstBlock.ItemId)
                {
                    firstBlock = elem;
                }
            }

            if (firstBlock == null)
            {
                return;
            }

            var config = FlowchartDefaultConfig.S;
            firstBlock.Scope = config.NewBlockScope;
            firstBlock.BlockName = UniqueKeyGenerator.GetUniqueKeyFor(config.FirstBlockName, _manager.Blocks,
                ignoreItem: firstBlock, defaultKey: config.FirstBlockName);
            ApplyConfiguredEventHandlerToFirstBlock(firstBlock);
        }

        private void ApplyConfiguredEventHandlerToFirstBlock(Block block)
        {
            if (block == null)
            {
                return;
            }

            Type configuredType = FlowchartDefaultConfig.S.FirstBlockEventHandlerType;
            if (configuredType == null)
            {
                return;
            }

            bool invalidType =
                !typeof(EventHandler).IsAssignableFrom(configuredType) ||
                configuredType.IsAbstract ||
                configuredType.IsInterface;
            if (invalidType)
            {
                Debug.LogError($"Configured first-block event handler type is invalid: {configuredType}");
                return;
            }

            bool needsReplacement = block._EventHandler == null || block._EventHandler.GetType() != configuredType;
            if (!needsReplacement)
            {
                return;
            }

            if (block._EventHandler != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(block._EventHandler);
                }
                else
                {
                    DestroyImmediate(block._EventHandler);
                }
            }

            EventHandler newHandler = gameObject.AddComponent(configuredType) as EventHandler;
            if (newHandler == null)
            {
                Debug.LogError($"Failed to add EventHandler of type {configuredType} to Flowchart {name}.");
                return;
            }

            newHandler.ParentBlock = block;
            block._EventHandler = newHandler;
        }

        public Block FindBlock(string blockName) => _manager.FindBlock(blockName);
        public bool HasBlock(string blockName) => _manager.HasBlock(blockName);
        public bool ExecuteIfHasBlock(string blockName) => _manager.ExecuteIfHasBlock(blockName, ExecuteBlock);
        public void ExecuteBlock(string blockName) => _manager.ExecuteBlock(blockName);
        public void StopBlock(string blockName) => _manager.StopBlock(blockName);
        public bool ExecuteBlock(Block block, int commandIndex = 0, Action onComplete = null) => _manager.ExecuteBlock(block, commandIndex, onComplete);
        public void StopAllBlocks() => _manager.StopAllBlocks();
        public bool HasExecutingBlocks() => _manager.HasExecutingBlocks();
        public IReadOnlyList<Block> GetExecutingBlocks() => _manager.GetExecutingBlocks();

        public bool Contains(Block block) => _manager.Contains(block);
        public Block GetBlockWithId(ushort id) => _manager.GetBlockWithId(id);
        public void Add(Block block) => _manager.Add(block);
        public bool Remove(Block block) => _manager.Remove(block);
        public bool RemoveBlockWithId(ushort id) => _manager.RemoveBlockWithId(id);

        public bool Contains(Command cmd) => _manager.Contains(cmd);
        public Command GetCommandWithId(ushort id) => _manager.GetCommandWithId(id);
        public void Add(Command cmd) => _manager.Add(cmd);
        public bool Remove(Command cmd) => _manager.Remove(cmd);
        public bool RemoveCommandWithId(ushort id) => _manager.RemoveCommandWithId(id);
        public bool RemoveAllCommands() => _manager.RemoveAllCommands();

#if UNITY_EDITOR
        public T AddCommand<T>(Block toAddTo) where T : Command
        {
            return AddCommand(typeof(T), toAddTo) as T;
        }

        public Command AddCommand(Type commandType, Block toAddTo)
        {
            if (!typeof(Command).IsAssignableFrom(commandType))
            {
                Debug.LogError($"AddCommand: {commandType} does not inherit from Command.");
                return null;
            }

            Undo.RecordObject(this, $"Add {commandType.Name} Command");
            Undo.RecordObject(this.gameObject, $"Add {commandType.Name} Command Component");

            var added = Undo.AddComponent(this.gameObject, commandType) as Command;
            if (added == null)
            {
                Debug.LogError($"AddCommand: Failed to add component of type {commandType}.");
                return null;
            }

            added.ItemId = _manager.NextItemId();
            _manager.Add(added);
            toAddTo.CommandList.Add(added);
            added.OnCommandAdded(toAddTo);

            EditorUtility.SetDirty(this);

            return added;
        }

        public bool RemoveMultiBlocks(IList<Block> toUnregister)
        {
            bool success = false;
            for (int i = 0; i < toUnregister.Count; i++)
            {
                success |= Remove(toUnregister[i]);
            }

            return success;
        }
#endif

        public void Dispose()
        {
            _owner = null;
            _manager?.Dispose();
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        public bool Add(Block block, bool triggerSignals)
        {
            return _manager.Add(block, triggerSignals);
        }

        public bool Remove(Block block, bool triggerSignals)
        {
            return _manager.Remove(block, triggerSignals);
        }

        public bool RemoveBlockWithId(ushort id, bool triggerSignals)
        {
            return _manager.RemoveBlockWithId(id, triggerSignals);
        }

        public bool ClearBlocks(bool triggerSignals)
        {
            return _manager.ClearBlocks(triggerSignals);
        }
    }
}