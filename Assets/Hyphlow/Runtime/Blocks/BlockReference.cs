using UnityEngine;
using UnityObj = UnityEngine.Object;
using LegacyBlock = AtMycelia.Hyphlow.Block;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// A simple struct wrapping a reference to an Amanita Block. Allows for BlockReferenceDrawer. 
    /// This is the recommended way to directly reference an Amanita block in external C# scripts,
    /// as it will give you an inspector field that gives a drop down of all the blocks on a 
    /// Flowchart, in a similar way to what you would expect from selecting a Block on a Command.
    /// 
    /// If you want to showup in the Callers section of the Block, ensure your MonoBehaviours 
    /// that have these also implement IBlockCaller.
    /// </summary>
    [System.Serializable]
    public class BlockReference
    {
        [SerializeField] private byte _itemId = _InvalidId;
        [SerializeField] private UnityObj _owningSource;

        private static readonly byte _InvalidId = LegacyBlock.InvalidId;
        public byte ItemId
        {
            get { return _itemId; }
        }

        public IBlockSource BlockOwner
        {
            get
            {
                RefreshOwner();
                return _blockOwner;
            }
            set
            {
                _blockOwner = value;
                _owningSource = value as UnityObj;
            }
        }

        protected virtual void RefreshOwner()
        {
            _blockOwner ??= _owningSource as IBlockSource;
        }

        private IBlockSource _blockOwner;

        public IBlock Block
        {
            get
            {
                RefreshOwner();
                if (_itemId == _InvalidId || _blockOwner == null)
                {
                    return null;
                }

                return _blockOwner.GetBlock(_itemId);
            }
            set
            {
                if (value == null)
                {
                    _itemId = _InvalidId;
                    BlockOwner = null;
                }
                else
                {
                    _itemId = value.ItemId;
                    BlockOwner = value.ParentFlowchart;
                }
            }
        }

        public void Refresh()
        {
            RefreshOwner();
        }

    }

}