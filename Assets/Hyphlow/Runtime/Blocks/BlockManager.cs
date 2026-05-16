using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObj = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AtMycelia.Hyphlow
{
    [Serializable]
    public sealed class BlockManager : IBlockSource, IDisposable
    {
        [SerializeField] private List<Block> _blocks = new List<Block>();
        [SerializeField] private ushort _nextValidBlockId = 1;
        [NonSerialized] private Dictionary<ushort, Block> _lookup = new Dictionary<ushort, Block>();
        [NonSerialized] private IBlockSource _blockOwner;

        private static readonly string _defaultName = "BlockManager";

        /// <summary>
        /// Optional owner for naming/context (e.g., Flowchart or BlockLogicManagerComponent).
        /// </summary>
        public IBlockSource BlockOwner
        {
            get => _blockOwner;
            set => _blockOwner = value;
        }

        public IReadOnlyList<Block> Blocks
        {
            get
            {
                if (_blocks == null || _lookup == null || _lookup.Count != _blocks.Count)
                {
                    Refresh();
                }

                return _blocks;
            }
        }

        public void Refresh()
        {
            _blocks ??= new List<Block>();
            _lookup ??= new Dictionary<ushort, Block>();
            _lookup.Clear();

            while (_blocks.Contains(null))
            {
                _blocks.Remove(null);
            }

            for (int i = 0; i < _blocks.Count; i++)
            {
                Block current = _blocks[i];
                EnsureValidIdFor(current);
                _lookup[current.ItemId] = current;
            }
        }

        private void EnsureValidIdFor(Block block)
        {
            if (block == null)
            {
                Debug.LogError("Cannot ensure valid Block ID for a null Block.");
                return;
            }
            if (IsRegistered(block))
            {
                return;
            }

            while (block.ItemId == Block.InvalidId || _lookup.ContainsKey(block.ItemId))
            {
                block.ItemId = NextValidBlockId();
            }
        }

        private bool IsRegistered(Block block)
        {
            if (block == null)
            {
                return false;
            }
            bool result = _lookup != null && _lookup.ContainsKey(block.ItemId);
            return result;
        }

        /// <summary>
        /// Returns the next valid block ID, ensuring it does not conflict with existing blocks.
        /// </summary>
        public ushort NextValidBlockId()
        {
            if (_nextValidBlockId == Block.InvalidId)
            {
                _nextValidBlockId = 1;
            }

            ushort result = _nextValidBlockId;
            _nextValidBlockId++;
            return result;
        }

        public void Initialize(bool clearExisting = false)
        {
            _blocks ??= new List<Block>();
            _lookup ??= new Dictionary<ushort, Block>();

            if (clearExisting)
            {
                ClearBlocks();
                MarkOwnerAsDirty();
                return;
            }

            Refresh();
        }

        public bool ClearBlocks(bool triggerSignals = true)
        {
            bool anyRemoved = _blocks.Count > 0;
            while (_blocks.Count > 0)
            {
                Block toRemove = _blocks[0];
                Remove(toRemove, triggerSignals);
            }
            return anyRemoved;
        }

        public bool Contains(Block block)
        {
            if (block == null)
            {
                return false;
            }

            bool result = _lookup != null && _lookup.ContainsKey(block.ItemId);
            return result;
        }

        public Block GetBlockWithId(ushort id)
        {
            _lookup.TryGetValue(id, out Block result);
            return result;
        }

        public bool Add(Block block, bool triggerSignals = true)
        {
            if (block == null)
            {
                Debug.LogError("Cannot add null Block to BlockManager.");
                return false;
            }

            bool alreadyRegistered = Contains(block);
            if (alreadyRegistered)
            {
                return false;
            }

            AddToCaches(block, triggerSignals);
            return true;
        }

        private void AddToCaches(Block toAdd, bool triggerSignals = true)
        {
            if (toAdd == null)
            {
                Debug.LogError("Cannot add null Block to caches.");
                return;
            }

            EnsureValidIdFor(toAdd);
            if (triggerSignals)
            {
                PreBlockAdded(toAdd);
            }
            _blocks.Add(toAdd);
            _lookup[toAdd.ItemId] = toAdd;
            MarkOwnerAsDirty();
            if (triggerSignals)
            {
                BlockAdded(toAdd);
            }
        }

        public event Action<Block> PreBlockAdded = delegate { };
        public event Action<Block> BlockAdded = delegate { };

        public bool Remove(Block block, bool triggerSignals = true)
        {
            bool result = RemoveFromCaches(block, triggerSignals);
            return result;
        }

        private bool RemoveFromCaches(Block toRemove, bool triggerSignals)
        {
            if (!IsRegistered(toRemove))
            {
                return false;
            }
            if (triggerSignals)
            {
                PreBlockRemoved(toRemove);
            }

            _blocks.Remove(toRemove);
            _lookup.Remove(toRemove.ItemId);

            MarkOwnerAsDirty();

            if (triggerSignals)
            {
                BlockRemoved(toRemove);
            }
            return true;
        }

        public event Action<Block> PreBlockRemoved = delegate { };

        private void MarkOwnerAsDirty()
        {
#if UNITY_EDITOR
            if (_blockOwner is UnityObj ownerUnityObj)
            {
                EditorUtility.SetDirty(ownerUnityObj);
            }
#endif
        }

        public event Action<Block> BlockRemoved = delegate { };

        public bool RemoveBlockWithId(ushort id, bool triggerSignals)
        {
            bool found = _lookup.TryGetValue(id, out Block blockToRemove);
            if (!found || blockToRemove == null)
            {
                return false;
            }

            return Remove(blockToRemove, triggerSignals);
        }

        public void Dispose()
        {
            _blocks?.Clear();
            _lookup?.Clear();
            _blockOwner = null;
        }

        

        public string Name
        {
            get
            {
                string ownerName = _blockOwner != null ?
                    _blockOwner.Name :
                    null;
                return string.IsNullOrEmpty(ownerName) ?
                    _defaultName :
                    ownerName;
            }
            set
            {
                Debug.LogWarning("BlockManager.Name is read-only and cannot be set.");
            }
        }

    }
}