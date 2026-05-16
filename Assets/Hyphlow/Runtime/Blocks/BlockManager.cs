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
        
        /// <summary>
        /// Optional owner for naming/context (e.g., Flowchart or BlockLogicManagerComponent).
        /// </summary>
        public IBlockSource BlockOwner
        {
            get => _blockOwner;
            set => _blockOwner = value;
        }

        private IBlockSource _blockOwner;

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

        private Dictionary<ushort, Block> _lookup = new Dictionary<ushort, Block>();

        public void Refresh()
        {
            _blocks ??= new List<Block>();
            _lookup ??= new Dictionary<ushort, Block>();
            _lookup.Clear();

            for (int i = _blocks.Count - 1; i >= 0; i--)
            {
                if (_blocks[i] == null)
                {
                    _blocks.RemoveAt(i);
                    i--;
                }
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

            // During Refresh() we rebuild lookup from scratch, so we only need to ensure
            // the ID is valid and not already claimed by another block we've processed.
            while (block.ItemId == Block.InvalidId || _lookup.ContainsKey(block.ItemId))
            {
                block.ItemId = NextValidBlockId();
            }
        }

        private bool IsRegistered(Block block)
        {
            if (block == null || _lookup == null)
            {
                return false;
            }

            if (_lookup.TryGetValue(block.ItemId, out Block registered))
            {
                return ReferenceEquals(registered, block);
            }

            return false;
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
                if (!Remove(toRemove, triggerSignals))
                {
                    // Safety fallback to avoid infinite loops on stale/invalid registrations.
                    _blocks.RemoveAt(0);

                    if (toRemove != null)
                    {
                        if (_lookup.TryGetValue(toRemove.ItemId, out Block registered) &&
                            ReferenceEquals(registered, toRemove))
                        {
                            _lookup.Remove(toRemove.ItemId);
                        }
                        else
                        {
                            RemoveLookupEntryByReference(toRemove);
                        }
                    }
                }
            }

            return anyRemoved;
        }

        public bool Contains(Block block)
        {
            if (block == null)
            {
                return false;
            }

            if (_blocks != null && _blocks.Contains(block))
            {
                return true;
            }

            if (_lookup != null && _lookup.TryGetValue(block.ItemId, out Block registered))
            {
                return ReferenceEquals(registered, block);
            }

            return false;
        }

        public Block GetBlock(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (Block block in _blocks)
            {
                if (block != null && block.BlockName == name)
                {
                    return block;
                }
            }
            return null;
        }

        public Block GetBlock(ushort id)
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

            if (triggerSignals)
            {
                MarkOwnerAsDirty();
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
            if (toRemove == null)
            {
                return false;
            }

            bool removedFromList = _blocks.Remove(toRemove);

            bool removedFromLookup = false;
            if (_lookup.TryGetValue(toRemove.ItemId, out Block registered) &&
                ReferenceEquals(registered, toRemove))
            {
                removedFromLookup = _lookup.Remove(toRemove.ItemId);
            }
            else
            {
                removedFromLookup = RemoveLookupEntryByReference(toRemove);
            }

            if (!removedFromList && !removedFromLookup)
            {
                return false;
            }

            if (triggerSignals)
            {
                PreBlockRemoved(toRemove);
                MarkOwnerAsDirty();
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
            _lookup.TryGetValue(id, out Block blockToRemove);
            if (blockToRemove == null)
            {
                return false;
            }

            return Remove(blockToRemove, triggerSignals);
        }

        private bool RemoveLookupEntryByReference(Block block)
        {
            ushort keyToRemove = 0;
            bool found = false;

            foreach (var pair in _lookup)
            {
                if (ReferenceEquals(pair.Value, block))
                {
                    keyToRemove = pair.Key;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            _lookup.Remove(keyToRemove);
            return true;
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

        private static readonly string _defaultName = "BlockManager";
        public int BlockCount => _blocks.Count;
        // ^This may seem unnecessary now, but we'll expand this after we implement poco versions of
        // Blocks. 

    }
}