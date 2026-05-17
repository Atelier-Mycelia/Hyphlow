using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObj = UnityEngine.Object;
using AtMycelia.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AtMycelia.Hyphlow
{
    public interface IBlockManager : IBlockSource, IRefreshable
    {
        UnityObj BlockOwner { get; set; }
    }

    [Serializable]
    public sealed class BlockManager : IBlockManager, IDisposable
    {
        [SerializeField] private List<Block> _legacyBlocks = new List<Block>();
        [SerializeField] private ushort _nextValidBlockId = 1;
        [SerializeField] private UnityObj _blockOwner;

        /// <summary>
        /// Optional owner for naming/context (e.g., Flowchart or BlockLogicManagerComponent).
        /// </summary>
        public UnityObj BlockOwner
        {
            get => _blockOwner;
            set => _blockOwner = value;
        }

        public IReadOnlyList<IBlock> Blocks
        {
            get
            {
                if (_legacyBlocks == null || _lookup == null || _lookup.Count != _legacyBlocks.Count)
                {
                    Refresh();
                }

                return _legacyBlocks;
            }
        }

        private Dictionary<ushort, IBlock> _lookup = new Dictionary<ushort, IBlock>();

        public void Refresh()
        {
            _legacyBlocks ??= new List<Block>();
            _lookup ??= new Dictionary<ushort, IBlock>();
            _lookup.Clear();

            for (int i = _legacyBlocks.Count - 1; i >= 0; i--)
            {
                if (_legacyBlocks[i] == null)
                {
                    _legacyBlocks.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < _legacyBlocks.Count; i++)
            {
                Block current = _legacyBlocks[i];
                EnsureValidIdFor(current);
                _lookup[current.ItemId] = current;
            }
        }

        private void EnsureValidIdFor(IBlock block)
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

        private bool IsRegistered(IBlock block)
        {
            if (block == null || _lookup == null)
            {
                return false;
            }

            if (_lookup.TryGetValue(block.ItemId, out IBlock registered))
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
            _legacyBlocks ??= new List<Block>();
            _lookup ??= new Dictionary<ushort, IBlock>();

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
            bool anyRemoved = _legacyBlocks.Count > 0;
            while (_legacyBlocks.Count > 0)
            {
                Block toRemove = _legacyBlocks[0];
                if (!Remove(toRemove, triggerSignals))
                {
                    // Safety fallback to avoid infinite loops on stale/invalid registrations.
                    _legacyBlocks.RemoveAt(0);

                    if (toRemove != null)
                    {
                        if (_lookup.TryGetValue(toRemove.ItemId, out IBlock registered) &&
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

        public bool Contains(IBlock block)
        {
            if (block == null)
            {
                return false;
            }

            if (_legacyBlocks != null && _legacyBlocks.Contains(block as Block))
            {
                return true;
            }

            if (_lookup != null && _lookup.TryGetValue(block.ItemId, out IBlock registered))
            {
                return ReferenceEquals(registered, block);
            }

            return false;
        }

        public IBlock GetBlock(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            foreach (Block block in _legacyBlocks)
            {
                if (block != null && block.BlockName == name)
                {
                    return block;
                }
            }
            return null;
        }

        public IBlock GetBlock(ushort id)
        {
            _lookup.TryGetValue(id, out IBlock result);
            return result;
        }

        public bool Add(IBlock block, bool triggerSignals = true)
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

        private void AddToCaches(IBlock toAdd, bool triggerSignals = true)
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

            if (toAdd is Block legBlock)
            {
                legBlock.Owner = _blockOwner;
                _legacyBlocks.Add(legBlock);
            }
            
            _lookup[toAdd.ItemId] = toAdd;

            if (triggerSignals)
            {
                MarkOwnerAsDirty();
                BlockAdded(toAdd);
            }
        }

        public event Action<IBlock> PreBlockAdded = delegate { };
        public event Action<IBlock> BlockAdded = delegate { };

        public bool Remove(IBlock block, bool triggerSignals = true)
        {
            bool result = RemoveFromCaches(block, triggerSignals);
            return result;
        }

        private bool RemoveFromCaches(IBlock toRemove, bool triggerSignals)
        {
            if (toRemove == null)
            {
                return false;
            }

            if (toRemove is not Block legBlock)
            {
                string errorMessage = $"Haven't migrated to poco Blocks yet.";
                Debug.LogError(errorMessage);
                return false;
            }

            bool removedFromList = _legacyBlocks.Remove(legBlock);

            bool removedFromLookup = false;
            if (_lookup.TryGetValue(toRemove.ItemId, out IBlock registered) &&
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

        public event Action<IBlock> PreBlockRemoved = delegate { };

        private void MarkOwnerAsDirty()
        {
#if UNITY_EDITOR
            if (_blockOwner is UnityObj ownerUnityObj)
            {
                EditorUtility.SetDirty(ownerUnityObj);
            }
#endif
        }

        public event Action<IBlock> BlockRemoved = delegate { };

        public bool RemoveBlockWithId(ushort id, bool triggerSignals)
        {
            _lookup.TryGetValue(id, out IBlock blockToRemove);
            if (blockToRemove == null)
            {
                return false;
            }

            return Remove(blockToRemove, triggerSignals);
        }

        private bool RemoveLookupEntryByReference(IBlock block)
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
            _legacyBlocks?.Clear();
            _lookup?.Clear();
            _blockOwner = null;
        }

        public string Name
        {
            get
            {
                string ownerName = _blockOwner != null ?
                    _blockOwner.name :
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
        public int BlockCount => _legacyBlocks.Count;
        // ^This may seem unnecessary now, but we'll expand this after we implement poco versions of
        // Blocks. 

    }
}