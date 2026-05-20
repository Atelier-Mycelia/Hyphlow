using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BlockManagerComponent))]
    [ExecuteInEditMode]
    public class BlockExecutionManagerComponent : MonoBehaviour, IRefreshable, IDisposable, 
        IBlockExecutionManager
    {
        [SerializeField, HideInInspector] private MonoBehaviour _owner;
        [SerializeField, HideInInspector] private BlockExecutionManager _manager = new BlockExecutionManager();

        public virtual MonoBehaviour Owner
        {
            get
            {
                EnsureOwner();
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
            GetNeededComponents();
            EnsureOwner();
            _manager.Initialize(UnderlyingBlockManager, Owner);
            Refresh();
        }

        private void GetNeededComponents()
        {
            if (UnderlyingBlockManager == null)
            {
                UnderlyingBlockManager = gameObject.GetComponent<BlockManagerComponent>();
            }
        }

        private IBlockManager UnderlyingBlockManager
        {
            get
            {
                if (_unityObjBlockManager is IBlockManager)
                {
                    return _unityObjBlockManager as IBlockManager;
                }
                else
                {
                    return _nonUnityObjBlockManager;
                }
            }
            set
            {
                if (value == null)
                {
                    _unityObjBlockManager = null;
                    _nonUnityObjBlockManager = null;
                    return;
                }

                if (value is UnityObj uobj)
                {
                    _unityObjBlockManager = uobj;
                    _nonUnityObjBlockManager = null;
                }
                else
                {
                    _nonUnityObjBlockManager = value;
                    // ^Need to avoid assigning UnityObjs to this. Gotta avoid
                    // the serialization issues that come with that.
                    _unityObjBlockManager = null;
                }
            }
        }

        [SerializeField, HideInInspector] private UnityObj _unityObjBlockManager;
        [SerializeReference, HideInInspector] private IBlockManager _nonUnityObjBlockManager;

        protected virtual void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            GetNeededComponents();
            EnsureOwner();
            _manager.Initialize(UnderlyingBlockManager, Owner);
            // ^Later, we'll find a way to make the manager stick
            _manager.CoroutineRunner = _owner;
            _manager.Refresh();
        }

        private void EnsureOwner()
        {
            if (_owner == this)
            {
                _owner = null;
            }
            if (_owner != null)
            {
                return;
            }

            _owner = GetComponent<Flowchart>();
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

            var config = FlowchartGlobalDefaults.S;
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

            Type configuredType = FlowchartGlobalDefaults.S.FirstBlockEventHandlerType;
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
        public bool ExecuteBlock(IBlock block, int commandIndex = 0, Action onComplete = null) => _manager.ExecuteBlock(block, commandIndex, onComplete);
        public void StopAllBlocks() => _manager.StopAllBlocks();
        public bool HasExecutingBlocks() => _manager.HasExecutingBlocks();
        public IReadOnlyList<IBlock> ExecutingBlocks => _manager.ExecutingBlocks;

        public void Dispose()
        {
            _owner = null;
            _manager?.Dispose();
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        public bool ExecuteIfHasBlock(string blockName, Action<string> executeByName)
        {
            return _manager.ExecuteIfHasBlock(blockName, executeByName);
        }

        public void ExecuteBlock(byte blockId)
        {
            _manager.ExecuteBlock(blockId);
        }

        public void StopBlock(byte blockId)
        {
            _manager.StopBlock(blockId);
        }
    }
}