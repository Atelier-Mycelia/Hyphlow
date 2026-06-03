using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObj = UnityEngine.Object;
using AtMycelia.Collections;
using AtMycelia.Hyphlow.EditorExt;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AtMycelia.Hyphlow
{
	public interface IBlockManager : IBlockSource, IRefreshable, ICommandResetter
	{
		UnityObj BlockOwner { get; set; }
		byte NextValidId();

	}

	[Serializable]
	public sealed class BlockManager : IBlockManager, IDisposable, IBlockCreator
	{
		[SerializeField] private List<Block> _legacyBlocks = new List<Block>();
		[SerializeField] private byte _nextValidBlockId = 1;
		// ^Note that item ids for blocks and Commands no longer draw from the same pool;
		// they are set up relative to their owners. Hence the use of byte here;
		// if an actual prod Flowchart needs anywhere close to 255 Blocks,
		// the user needs to break it up into multiple Flowcharts. This is a reasonable
		// limitation, and it keeps the data size down. Same logic applies
		// to Blocks and their Command-counts.
		[SerializeField] private UnityObj _blockOwner;

		/// <summary>
		/// Optional owner for naming/context (e.g., Flowchart or BlockLogicManagerComponent).
		/// </summary>
		public UnityObj BlockOwner
		{
			get => _blockOwner;
			set
			{
				_blockOwner = value;
			}
		}

		public IReadOnlyList<IBlock> Blocks
		{
			get
			{
				if (_legacyBlocks == null || _lookup == null || 
					_lookup.Count != _legacyBlocks.Count)
				{
					Refresh();
				}

				return _legacyBlocks;
			}
		}

		private IDictionary<byte, IBlock> _lookup = new Dictionary<byte, IBlock>();

		public void Refresh()
		{
			_legacyBlocks ??= new List<Block>();
			_legacyBlocks.RemoveAll(block => block == null);

			EnsureValidIdsForAllOurBlocks();
			EnsureBlocksHaveValidSizes();
			RefreshLookup();
		}

		private void EnsureValidIdsForAllOurBlocks()
		{
			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				Block current = _legacyBlocks[i];
				EnsureValidIdFor(current);
			}
		}

		private void EnsureValidIdFor(IBlock block)
		{
			if (block == null)
			{
				Debug.LogError("Cannot ensure valid Block ID for a null Block.");
				return;
			}

			while (block.ItemId == Block.InvalidId || _lookup.ContainsKey(block.ItemId))
			{
				block.ItemId = NextValidBlockId();
			}
		}

		private void RefreshLookup()
		{
			_lookup ??= new Dictionary<byte, IBlock>();
			_lookup.Clear();

			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				Block current = _legacyBlocks[i];
				if (current != null)
				{
					_lookup[current.ItemId] = current;
				}
			}
		}

		private void EnsureBlocksHaveValidSizes()
		{
			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				var currentBlock = _legacyBlocks[i];
				Rect nodeRect = currentBlock._NodeRect;
				if (nodeRect.size.Equals(Vector2.zero))
				{
					string logMessage = $"Fixing the size of Block {currentBlock.BlockName}. " +
						$"There may be an underlying problem.";
                   Debug.LogWarning(logMessage);
					Rect fixedRect = new Rect(nodeRect.position, DefaultConfig.BlockSize);
					currentBlock._NodeRect = fixedRect;
				}
			}
		}

		public byte NextValidId()
		{
			return NextValidBlockId();
		}

		/// <summary>
		/// Returns the next valid block ID, ensuring it does not conflict with existing blocks.
		/// </summary>
		private byte NextValidBlockId()
		{
			byte result = _nextValidBlockId;
			_nextValidBlockId++;

			if (_nextValidBlockId == Block.InvalidId)
			{
				_nextValidBlockId = 1;
			}

			return result;
		}

		public IReadOnlyList<ICommand> Commands
		{
			get
			{
				IList<ICommand> result = new List<ICommand>();
				for (int i = 0; i < _legacyBlocks.Count; i++)
				{
					Block block = _legacyBlocks[i];
					if (block != null)
					{
						result.AddRange(block.CommandList);
					}
				}
				return (IReadOnlyList<ICommand>)result;
			}
		}

		public bool Contains(ICommand cmd)
		{
			bool result = false;
			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				Block block = _legacyBlocks[i];
				if (block != null && block.Contains(cmd))
				{
					result = true;
					break;
				}
			}
			return result;
		}

		public ICommand GetCommandWithId(byte id)
		{
			ICommand result = null;
			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				Block block = _legacyBlocks[i];
				if (block != null)
				{
					result = block.GetCommandWithId(id);
					if (result != null)
					{
						break;
					}
				}
			}
			return result;
		}
		public bool RemoveCommandWithId(byte id)
		{
			bool anyRemoved = false;
			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				Block block = _legacyBlocks[i];
				if (block != null)
				{
					bool removedFromBlock = block.RemoveCommandWithId(id);
					anyRemoved |= removedFromBlock;
				}
			}
			return anyRemoved;
		}
		public bool RemoveAllCommands(bool triggerSignals = true)
		{
			bool anyRemoved = false;
			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				Block block = _legacyBlocks[i];
				if (block != null)
				{
					bool removedFromBlock = block.RemoveAllCommands(triggerSignals);
					anyRemoved |= removedFromBlock;
				}
			}
			return anyRemoved;
		}

		public void Initialize(bool clearExisting = false)
		{
			_legacyBlocks ??= new List<Block>();
			_lookup ??= new Dictionary<byte, IBlock>();

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

			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				Block block = _legacyBlocks[i];
				if (block != null && block.BlockName == name)
				{
					return block;
				}
			}
			return null;
		}

		public IBlock GetBlock(byte id)
		{
			_lookup.TryGetValue(id, out IBlock result);
			return result;
		}

		public bool AddRange(ICollection<Block> blocks, bool triggerSignals = true)
		{
			if (blocks == null)
			{
				Debug.LogError("Cannot add null collection of Blocks to BlockManager.");
				return false;
			}
			bool anyAdded = false;
			List<Block> blocksList = blocks as List<Block> ?? new List<Block>(blocks);
			for (int i = 0; i < blocksList.Count; i++)
			{
				Block block = blocksList[i];
				bool added = Add(block, triggerSignals);
				anyAdded |= added;
			}
			return anyAdded;
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
			if (toAdd == null || DefaultConfig == null)
			{
              Debug.LogError("Failed to ensure valid ID for Block being added to caches, or default config is missing.");
				Debug.Break();
				return;
			}
			toAdd.Key = UniqueKeyGenerator.GetUniqueKeyFor(toAdd.Key, _legacyBlocks, toAdd,
				DefaultConfig.NewBlockName);
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

      private static FlowchartGlobalDefaults DefaultConfig
		{
			get
			{
				FlowchartGlobalDefaults config = FlowchartGlobalDefaults.S;
				if (config)
				{
					return config;
				}

				if (!_didLogMissingDefaultConfig)
				{
					Debug.LogWarning("FlowchartGlobalDefaults could not be loaded. Using in-memory defaults for this session.");
					_didLogMissingDefaultConfig = true;
				}

				_fallbackDefaultConfig ??= ScriptableObject.CreateInstance<FlowchartGlobalDefaults>();
				_fallbackDefaultConfig.hideFlags = HideFlags.HideAndDontSave;
				return _fallbackDefaultConfig;
			}
		}

		private static FlowchartGlobalDefaults _fallbackDefaultConfig;
		private static bool _didLogMissingDefaultConfig;
		public event Action<IBlock> PreBlockAdded = delegate { };
		public event Action<IBlock> BlockAdded = delegate { };

		public bool Remove(IBlock block, bool triggerSignals = true)
		{
			bool result = RemoveFromCaches(block, triggerSignals);
			return result;
		}

		public bool Remove(ICommand cmd, bool triggerSignals = true)
		{
			if (cmd == null)
			{
				return false;
			}
			bool anyRemoved = false;
			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				Block block = _legacyBlocks[i];
				if (block != null && block.Contains(cmd))
				{
					anyRemoved = true;
					break;
				}
			}
			return anyRemoved;
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

		public bool RemoveBlockWithId(byte id, bool triggerSignals)
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
			byte keyToRemove = 0;
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

		public bool RemoveCommandWithId(byte id, bool triggerSignals)
		{
			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				Block block = _legacyBlocks[i];
				if (block != null)
				{
					bool removedFromBlock = block.RemoveCommandWithId(id);
					if (removedFromBlock)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void ResetCommands()
		{
			for (int i = 0; i < _legacyBlocks.Count; i++)
			{
				IBlock block = _legacyBlocks[i];
				block?.ResetCommands();
			}
		}

		public IBlock CreateBlock(Vector2 position, string blockName = null,
			bool triggerSignals = true)
		{
			#region Initialization
			Component ownerAsComp = (Component)_blockOwner;
			Block created = ownerAsComp.gameObject.AddComponent<Block>();
			ApplyDefaultConfigTo(created, position);
			#endregion

			BlockSignals.BlockCreated(created);
			Add(created, triggerSignals);

			return created;
		}

		private void ApplyDefaultConfigTo(IBlock newlyCreatedBlock, Vector2 position = default)
		{
			#region Give it a default name/key
			bool isFirstBlock = BlockCount == 0;
			string suggestedName = isFirstBlock ?
				DefaultConfig.FirstBlockName :
				DefaultConfig.NewBlockName;
			newlyCreatedBlock.Key = UniqueKeyGenerator.GetUniqueKeyFor(newlyCreatedBlock.Key, 
				_legacyBlocks, newlyCreatedBlock, suggestedName);
			// ^Needed for when we already have a block named NewBlockName, which is
			// common when creating multiple blocks in a row.
			#endregion

			newlyCreatedBlock.Scope = DefaultConfig.NewBlockScope;
#if UNITY_EDITOR
			newlyCreatedBlock._NodeRect = new Rect(position, DefaultConfig.BlockSize);
#endif
		}

		public IList<IBlock> CreateMultiBlocks(IList<Vector2> positions)
		{
			IList<IBlock> result = new List<IBlock>();

			for (int i = 0; i < positions.Count; i++)
			{
				Vector2 pos = positions[i];
				IBlock created = CreateBlock(pos);
				result.Add(created);
			}

			return result;
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


	}
}