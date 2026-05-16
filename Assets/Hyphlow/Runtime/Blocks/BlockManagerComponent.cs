using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class BlockManagerComponent : MonoBehaviour, IBlockSource, IRefreshable, IDisposable
    {
        [SerializeField, HideInInspector] private MonoBehaviour _owner;
        [SerializeField, HideInInspector] private BlockManager _blockManager = new BlockManager();
        [SerializeField, HideInInspector] private Flowchart _cachedFlowchart;

        public virtual IBlockSource Owner
        {
            get
            {
                if (_owner is IBlockSource blockSource)
                {
                    return blockSource;
                }

                return this;
            }
            set
            {
                _owner = value as MonoBehaviour;
                _blockManager.BlockOwner = value;
            }
        }

        public virtual string Name
        {
            get => name;
            set => name = value;
        }

        public virtual IReadOnlyList<Block> Blocks => _blockManager.Blocks;

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

        public Block GetBlock(ushort id)
        {
            return _blockManager.GetBlock(id);
        }

        public Block GetBlock(string name)
        {
            return _blockManager.GetBlock(name);
        }

        public int BlockCount => _blockManager.Blocks.Count;
    }
}