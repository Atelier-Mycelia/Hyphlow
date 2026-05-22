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