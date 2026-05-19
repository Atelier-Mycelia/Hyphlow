using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow
{
	[DisallowMultipleComponent]
	[ExecuteInEditMode]
	public class BlockManagerComponent : MonoBehaviour, IBlockManager, IDisposable, IBlockCreator
	{
		[SerializeField, HideInInspector] private BlockManager _blockManager = new BlockManager();
		[SerializeField, HideInInspector] private Flowchart _cachedFlowchart;

		protected virtual void Awake()
		{
			Refresh();
		}

		public virtual void Refresh()
		{
			EnsureOwner();

			_blockManager.ClearBlocks(false);

			RegisterOurBlocksIntoTheManager();
			void RegisterOurBlocksIntoTheManager()
			{
				// At some point, we'll have to change this to take the poco vers into account,
				// but for now, we just want to make sure that the legacy Blocks on this
				// GameObject are registered.
				IList<Block> blocksOnGameObject = GetComponents<Block>();
				for (int i = 0; i < blocksOnGameObject.Count; i++)
				{
					Block block = blocksOnGameObject[i];
					if (block == null)
					{
						continue;
					}

					_blockManager.Add(block, false);
				}
			}

		}

		protected virtual void EnsureOwner()
		{
			if (Owner == this)
			{
				Owner = null;
			}

			if (Owner != null)
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

		public virtual UnityObj Owner
		{
			get => _blockManager.BlockOwner;
			set => _blockManager.BlockOwner = value;
		}

		protected virtual void OnEnable()
		{
			Refresh();
		}

		#region Most delegations to underlying manager
		public virtual IReadOnlyList<IBlock> Blocks => _blockManager.Blocks;
		
		public virtual bool Contains(IBlock block) => 
			_blockManager.Contains(block);
		public virtual bool Add(IBlock block, bool triggerSignals) => 
			_blockManager.Add(block, triggerSignals);
		public virtual bool Remove(IBlock block, bool triggerSignals) => 
			_blockManager.Remove(block, triggerSignals);
		public virtual bool RemoveBlockWithId(byte id, bool triggerSignals) => 
			_blockManager.RemoveBlockWithId(id, triggerSignals);
		public virtual bool ClearBlocks(bool triggerSignals) => 
			_blockManager.ClearBlocks(triggerSignals);

		public IBlock GetBlock(byte id) => _blockManager.GetBlock(id);
		public IBlock GetBlock(string name) => _blockManager.GetBlock(name);
		public bool Contains(ICommand cmd) => _blockManager.Contains(cmd);
		public ICommand GetCommandWithId(byte id) => _blockManager.GetCommandWithId(id);

		public bool Remove(ICommand cmd, bool triggerSignals = true) => 
			_blockManager.Remove(cmd, triggerSignals);
		public bool RemoveAllCommands(bool triggerSignals = true) => _blockManager.RemoveAllCommands(triggerSignals);
		public bool RemoveCommandWithId(byte id, bool triggerSignals) =>
			_blockManager.RemoveCommandWithId(id, triggerSignals);

		public int BlockCount => _blockManager.Blocks.Count;

		public UnityObj BlockOwner { get => _blockManager.BlockOwner; set => _blockManager.BlockOwner = value; }

		public IReadOnlyList<ICommand> Commands => _blockManager.Commands;
		#endregion

		public virtual void Dispose()
		{
			Owner = null;
			_cachedFlowchart = null;
			_blockManager?.Dispose();
		}

		protected virtual void OnDestroy()
		{
			Dispose();
		}

		public IBlock CreateBlock(Vector2 position, string blockName = null, bool triggerSignals = true)
		{
			bool creatingFirstBlock = _blockManager.BlockCount == 0;

			DecideOnBlockName();
			void DecideOnBlockName()
			{
				if (creatingFirstBlock)
				{
					blockName ??= DefaultConfig.FirstBlockName;
				}
				else
				{
					blockName ??= DefaultConfig.NewBlockName;
				}
			}

			Block created = gameObject.AddComponent<Block>();
#if UNITY_EDITOR
			created._NodeRect = new Rect(position, DefaultConfig.BlockSize);
#endif
			created.Scope = DefaultConfig.NewBlockScope;

			_blockManager.Add(created, triggerSignals);

			if (creatingFirstBlock)
			{
				ApplyConfiguredEventHandlerToFirstBlock(created);
			}

			BlockSignals.BlockCreated(created);
			return created;
		}

		private void ApplyConfiguredEventHandlerToFirstBlock(IBlock block)
		{
			// TODO: Set up the UI so the user can make a proper choice without
			// needing to get too technical. For now, do nothing.
		}

		protected static FlowchartGlobalDefaults DefaultConfig => FlowchartGlobalDefaults.S;

		public IList<IBlock> CreateMultiBlocks(IList<Vector2> positions)
		{
			throw new NotImplementedException();
		}

		public byte NextValidId()
		{
			return ((IBlockManager)_blockManager).NextValidId();
		}

		public void ResetCommands()
		{
			_blockManager.ResetCommands();
		}

		public virtual string Name
		{
			get => name;
			set => name = value;
		}
	}
}