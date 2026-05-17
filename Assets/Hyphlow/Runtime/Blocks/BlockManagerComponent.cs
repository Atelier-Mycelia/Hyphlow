using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class BlockManagerComponent : MonoBehaviour, IBlockManager, IDisposable
    {
        [SerializeField, HideInInspector] private MonoBehaviour _owner;
        [SerializeField, HideInInspector] private BlockManager _blockManager = new BlockManager();
        [SerializeField, HideInInspector] private Flowchart _cachedFlowchart;

        public virtual UnityObj Owner
        {
            get => _blockManager.BlockOwner;
            set => _blockManager.BlockOwner = value;
        }

        public virtual string Name
        {
            get => name;
            set => name = value;
        }

        public virtual IReadOnlyList<IBlock> Blocks => _blockManager.Blocks;

        protected virtual void Awake()
        {
            Refresh();
        }

        protected virtual void OnEnable()
        {
            Refresh();
        }

        protected virtual void EnsureOwner()
        {
            if (_owner is IBlockSource)
            {
                return;
            }

            if (_cachedFlowchart == null)
            {
                _cachedFlowchart = GetComponent<Flowchart>();
            }

            if (_cachedFlowchart != null)
            {
                Owner = _cachedFlowchart;
            }
        }

        public virtual void Refresh()
        {
            EnsureOwner();
            _blockManager.BlockOwner = Owner;

            _blockManager.ClearBlocks(false);

            Block[] blocksOnGameObject = GetComponents<Block>();
            for (int i = 0; i < blocksOnGameObject.Length; i++)
            {
                Block block = blocksOnGameObject[i];
                if (block == null)
                {
                    continue;
                }

                _blockManager.Add(block, false);
            }
        }

        public virtual bool Contains(Block block) => _blockManager.Contains(block);
        public virtual bool Add(Block block, bool triggerSignals) => _blockManager.Add(block, triggerSignals);
        public virtual bool Remove(Block block, bool triggerSignals) => _blockManager.Remove(block, triggerSignals);
        public virtual bool RemoveBlockWithId(ushort id, bool triggerSignals) => _blockManager.RemoveBlockWithId(id, triggerSignals);
        public virtual bool ClearBlocks(bool triggerSignals) => _blockManager.ClearBlocks(triggerSignals);

        public virtual void Dispose()
        {
            _owner = null;
            _cachedFlowchart = null;
            _blockManager?.Dispose();
        }

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        public IBlock GetBlock(ushort id) => _blockManager.GetBlock(id);

        public IBlock GetBlock(string name) => _blockManager.GetBlock(name);

        public bool Contains(IBlock block) => _blockManager.Contains(block);

        public bool Add(IBlock block, bool triggerSignals) => _blockManager.Add(block, triggerSignals);

        public bool Remove(IBlock block, bool triggerSignals) => _blockManager.Remove(block, triggerSignals);

        public int BlockCount => _blockManager.Blocks.Count;

        public UnityObj BlockOwner { get => _blockManager.BlockOwner; set => _blockManager.BlockOwner = value; }

        IReadOnlyList<IBlock> IBlockSource.Blocks => (_blockManager).Blocks;
    }
}