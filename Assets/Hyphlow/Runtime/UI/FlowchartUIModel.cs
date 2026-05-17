using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace AtMycelia.Hyphlow.UI
{
    /// <summary>
    /// Model for Flowchart editor window. Stores information about selected blocks and
    /// commands, scroll position, zoom level, etc.
    /// </summary>
    [System.Serializable]
    public class FlowchartUIModel : IFlowchartUIModel
    {
        [FormerlySerializedAs("_selectedBlocks")]
        [SerializeField] protected List<Block> _selectedLegacyBlocks = new List<Block>();
        [FormerlySerializedAs("_selectedCommands")]
        [SerializeField] protected List<Command> _selectedLegacyCommands = new List<Command>();

        [SerializeField]
        private GameObject _owner;

        public virtual GameObject Owner
        {
            get => _owner;
            set => _owner = value;
        }

        [field: SerializeField] public Vector2 ScrollPos { get; set; } = Vector2.zero;

        /// <summary>
        /// Scroll position of Flowchart variables window.
        /// </summary>
        [field: SerializeField] public virtual Vector2 VariablesScrollPos { get; set; }

        /// <summary>
        /// Whether or not to show the variables pane.
        /// </summary>
        [field: SerializeField] public virtual bool VariablesExpanded { get; set; }

        /// <summary>
        /// Zoom level of Flowchart editor window.
        /// </summary>
        [field: SerializeField] public float Zoom { get; set; } = 1f;

        /// <summary>
        /// Height of Command block view in inspector.
        /// </summary>
        [field: SerializeField] public virtual float BlockViewHeight { get; set; } = 400;

        /// <summary>
        /// Scrollable area for Flowchart editor window.
        /// </summary>
        [field: SerializeField] public virtual Rect ScrollViewRect { get; set; }   
        
        public virtual IBlock SelectedBlock
        {
            get
            {
                if (_selectedLegacyBlocks.Count == 0)
                {
                    return null;
                }

                return _selectedLegacyBlocks[0];
            }
            set
            {
                ClearSelectedBlocks();
                AddToSelection(value);
            }
        }

        public IList<IBlock> SelectedBlocks
        {
            get => new List<IBlock>(_selectedLegacyBlocks);
            set
            {
                ClearSelectedBlocks();
                AddRangeToSelection(value);
            }
        }

        public ICommand SelectedCommand
        {
            get
            {
                if (_selectedLegacyCommands.Count == 0)
                {
                    return null;
                }

                return _selectedLegacyCommands[0];
            }
            set
            {
                ClearSelectedCommands();
                AddToSelection(value);
            }
        }

        public IList<ICommand> SelectedCommands
        {
            get => new List<ICommand>(_selectedLegacyCommands);
            set
            {
                ClearSelectedCommands();
                AddRangeToSelection(value);
            }
        }

        public virtual void ClearSelectedBlocks()
        {
            int amountToClear = _selectedLegacyBlocks.Count;
            if (amountToClear == 0)
            {
                return;
            }

            Block firstBlock = _selectedLegacyBlocks[0];
            IList<IBlock> blocksToDeselect = new List<IBlock>(_selectedLegacyBlocks);
            foreach (var blockEl in _selectedLegacyBlocks)
            {
                if (blockEl == null)
                {
                    continue;
                }
                blockEl.IsSelected = false;
            }
            _selectedLegacyBlocks.Clear();

            if (amountToClear > 1)
            {
                BlockSignals.MultiBlocksDeselected(blocksToDeselect);
            }
            else if (amountToClear == 1)
            {
                BlockSignals.BlockDeselected(firstBlock);
            }
        }

        public virtual void ClearSelectedCommands()
        {
            _selectedLegacyCommands.Clear();
        }

        public void AddRangeToSelection(IList<IBlock> toAdd)
        {
            foreach (var blockEl in toAdd)
            {
                // To avoid confusion, we don't want this to be able to trigger MultiBlocksSelected
                // and BlockSelected at the same time in the same call of this func
                AddToSelectionWithoutSignal(blockEl);
            }

            if (toAdd.Count > 0)
            {
                BlockSignals.MultiBlocksSelected(toAdd);
            }
            else if (toAdd.Count == 1)
            {
                BlockSignals.BlockSelected(toAdd[0]);
            }
        }

        public virtual void AddToSelection(IBlock block)
        {
            if (block != null && !_selectedLegacyBlocks.Contains(block as Block))
            {
                AddToSelectionWithoutSignal(block);
                BlockSignals.BlockSelected(block);
            }
        }

        protected virtual void AddToSelectionWithoutSignal(IBlock block)
        {
            Block legBlock = block as Block;
            if (block != null && !_selectedLegacyBlocks.Contains(legBlock))
            {
                block.IsSelected = true;
                _selectedLegacyBlocks.Add(legBlock);
            }
        }

        public virtual void AddRangeToSelection(IList<ICommand> toAdd)
        {
            foreach (var command in toAdd)
            {
                AddToSelection(command);
            }
        }

        public virtual void AddToSelection(ICommand toAdd)
        {
            Command legCommand = toAdd as Command;
            if (!_selectedLegacyCommands.Contains(legCommand))
            {
                _selectedLegacyCommands.Add(legCommand);
            }
        }

        public virtual void Deselect(ICommand toRemove)
        {
            _selectedLegacyCommands.Remove(toRemove as Command);
        }

        public virtual void Deselect(IBlock toDeselect)
        {
            DeselectWithoutSignal(toDeselect);
            BlockSignals.BlockDeselected(toDeselect);
        }

        public virtual void DeselectWithoutSignal(IBlock toDeselect)
        {
            toDeselect.IsSelected = false;
            _selectedLegacyBlocks.Remove(toDeselect as Block);
        }

        [field: SerializeField] public bool SelectedCommandsStale { get; set; }

        public virtual bool Contains(ICommand command)
        {
            return _selectedLegacyCommands.Contains(command as Command);
        }

        public virtual bool Contains(IBlock block)
        {
            return _selectedLegacyBlocks.Contains(block as Block);
        }

        public virtual int CommandCount { get { return _selectedLegacyCommands.Count; } }
        public virtual int BlockCount { get { return _selectedLegacyBlocks.Count; } }
        public virtual void CleanUp()
        {
            // To get rid of unreferenced Blocks and Commands, which should 
            // mean less memory leaks
            _selectedLegacyBlocks.RemoveAll(item => item == null);
            _selectedLegacyCommands.RemoveAll(item => item == null);
        }

    }
}
