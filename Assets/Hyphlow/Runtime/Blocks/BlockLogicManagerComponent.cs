using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BlockManagerComponent))]
    [ExecuteInEditMode]
    public class BlockLogicManagerComponent : MonoBehaviour, IRefreshable, IDisposable, 
        IBlockLogicManager
    {
        [SerializeField, HideInInspector] private MonoBehaviour _owner;
        [SerializeField, HideInInspector] private BlockLogicManager _manager;

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
                _manager.CoroutineRunner = _owner;
            }
        }

        public virtual string Name
        {
            get => name;
            set => name = value;
        }

        public virtual IReadOnlyList<IBlock> Blocks => _manager.Blocks;

        public MonoBehaviour CoroutineRunner { get => _manager.CoroutineRunner; set => _manager.CoroutineRunner = value; }

        protected virtual void Awake()
        {
            _blockManagerComponent = gameObject.GetComponent<BlockManagerComponent>();
            _manager = new BlockLogicManager();
            _manager.Initialize(_blockManagerComponent, Owner);
            Refresh();
        }

        [SerializeReference, HideInInspector] private IBlockManager _blockManagerComponent;

        protected virtual void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            EnsureOwner();
            _manager.CoroutineRunner = _owner;
            _manager.Refresh();
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

        public void ApplyDefaultConfigToFirstBlock()
        {
            if (_manager.Blocks.Count == 0)
            {
                return;
            }

            IBlock firstBlock = null;
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

        private void ApplyConfiguredEventHandlerToFirstBlock(IBlock block)
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

            bool needsReplacement = block.EventHandler == null || block.EventHandler.GetType() != configuredType;
            if (!needsReplacement)
            {
                return;
            }

            if (block.EventHandler != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(block.EventHandler as UnityObj);
                }
                else
                {
                    DestroyImmediate(block.EventHandler as UnityObj);
                }
            }

            IEventHandler newHandler = gameObject.AddComponent(configuredType) as EventHandler;
            if (newHandler == null)
            {
                Debug.LogError($"Failed to add EventHandler of type {configuredType} to Flowchart {name}.");
                return;
            }

            newHandler.ParentBlock = block;
            block.EventHandler = newHandler;
        }

        public bool ExecuteIfHasBlock(string blockName) => _manager.ExecuteIfHasBlock(blockName, ExecuteBlock);
        public void ExecuteBlock(string blockName) => _manager.ExecuteBlock(blockName);
        public void StopBlock(string blockName) => _manager.StopBlock(blockName);
        public bool ExecuteBlock(Block block, int commandIndex = 0, Action onComplete = null) => _manager.ExecuteBlock(block, commandIndex, onComplete);
        public void StopAllBlocks() => _manager.StopAllBlocks();
        public bool HasExecutingBlocks() => _manager.HasExecutingBlocks();
        public IReadOnlyList<IBlock> GetExecutingBlocks() => _manager.GetExecutingBlocks();

        public void Dispose()
        {
            _owner = null;
            _manager?.Dispose();
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        public bool ExecuteBlock(IBlock block, int commandIndex = 0, Action onComplete = null)
        {
            return _manager.ExecuteBlock(block, commandIndex, onComplete);
        }

        public bool ExecuteIfHasBlock(string blockName, Action<string> executeByName)
        {
            return _manager.ExecuteIfHasBlock(blockName, executeByName);
        }
    }
}