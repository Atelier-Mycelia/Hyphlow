using UnityEngine;
using UnityEngine.Serialization;
using UnityObj = UnityEngine.Object;

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
        [FormerlySerializedAs("block")]
        [SerializeField] [HideInInspector] private Block _block;
        [SerializeField] private ushort _itemId = Block.InvalidId;
        [SerializeField] private UnityObj _owningSource;

        public ushort ItemId
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
                _block = null;
            }
        }

        public Block Block
        {
            get
            {
                RefreshOwner();
                if (_itemId == Block.InvalidId || _blockOwner == null)
                {
                    return null;
                }

                return _blockOwner.GetBlock(_itemId);
            }
            set
            {
                if (value == null)
                {
                    _itemId = Block.InvalidId;
                    BlockOwner = null;
                }
                else
                {
                    _itemId = value.ItemId;
                    BlockOwner = value.GetFlowchart();
                }
            }
        }

        public void Refresh()
        {
            RefreshOwner();
        }

        private void RefreshOwner()
        {
            _blockOwner = null;

            if (IsUnityObjectNull(_owningSource))
            {
                if (!IsUnityObjectNull(_block))
                {
                    _itemId = _block.ItemId;
                    _owningSource = _block.GetFlowchart();
                    _block = null;
                }
            }

            _blockOwner ??= _owningSource as Flowchart;
        }

        private IBlockSource _blockOwner;

        private static bool IsUnityObjectNull(UnityObj unityObj)
        {
            bool isRealNull = ReferenceEquals(unityObj, null);
            bool isFakeUnityNull = !isRealNull && unityObj == null;
            return isFakeUnityNull;
        }

        public void Execute()
        {
            if (Block == null)
            {
                string errorMessage = $"Tried to execute block reference, but block was null. ItemId: " +
                    $"{_itemId}, OwningSource: {_owningSource}";
                Debug.LogError(errorMessage);
                return;
            }

            Block.StartExecution();
        }
    }

}