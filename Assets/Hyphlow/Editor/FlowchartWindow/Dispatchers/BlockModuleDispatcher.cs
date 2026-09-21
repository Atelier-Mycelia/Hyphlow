using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    public sealed class BlockModuleDispatcher : IModuleDispatcher<IFlowchartWindowModule>
    {
        private readonly List<IFlowchartWindowModule> _modules = new List<IFlowchartWindowModule>();
        private readonly Dictionary<Type, IList> _responderBuckets = new Dictionary<Type, IList>();

        public void ToggleSubs(bool on)
        {
            if (on)
            {
                BlockSignals.BlockCreated += NotifyBlockCreated;

                BlockSignals.BlockLeftClicked += NotifyBlockClicked;
                BlockSignals.BlockSelected += NotifyBlockSelected;
                BlockSignals.MultiBlocksSelected += NotifyMultiBlocksSelected;

                BlockSignals.BlockDeselected += NotifyBlockDeselected;
                BlockSignals.MultiBlocksDeselected += NotifyMultiBlocksDeselected;

                BlockSignals.PreBlockDelete += NotifyPreBlockDeleted;
                BlockSignals.PostBlockDelete += NotifyPostBlockDeleted;
                BlockSignals.PreMultiBlockDelete += NotifyPreMultiBlockDeleted;
                BlockSignals.PostMultiBlockDelete += NotifyPostMultiBlockDeleted;

                BlockSignals.BlocksCopied += NotifyBlocksCopied;

                BlockSignals.PreBlockCut += NotifyPreBlockCut;
                BlockSignals.PostBlockCut += NotifyPostBlockCut;
                BlockSignals.PreMultiBlockCut += NotifyPreMultiBlockCut;
                BlockSignals.PostMultiBlockCut += NotifyPostMultiBlockCut;
            }
            else
            {
                BlockSignals.BlockCreated -= NotifyBlockCreated;

                BlockSignals.BlockLeftClicked -= NotifyBlockClicked;
                BlockSignals.BlockSelected -= NotifyBlockSelected;
                BlockSignals.MultiBlocksSelected -= NotifyMultiBlocksSelected;

                BlockSignals.BlockDeselected -= NotifyBlockDeselected;
                BlockSignals.MultiBlocksDeselected -= NotifyMultiBlocksDeselected;

                BlockSignals.PreBlockDelete -= NotifyPreBlockDeleted;
                BlockSignals.PostBlockDelete -= NotifyPostBlockDeleted;
                BlockSignals.PreMultiBlockDelete -= NotifyPreMultiBlockDeleted;
                BlockSignals.PostMultiBlockDelete -= NotifyPostMultiBlockDeleted;

                BlockSignals.BlocksCopied -= NotifyBlocksCopied;

                BlockSignals.PreBlockCut -= NotifyPreBlockCut;
                BlockSignals.PostBlockCut -= NotifyPostBlockCut;
                BlockSignals.PreMultiBlockCut -= NotifyPreMultiBlockCut;
                BlockSignals.PostMultiBlockCut -= NotifyPostMultiBlockCut;
            }
        }


        public void AddModule(object module)
        {
            if (module is not IFlowchartWindowModule flowchartModule)
            {
                throw new ArgumentException($"Module must implement {nameof(IFlowchartWindowModule)}", nameof(module));
            }
            AddModule((IFlowchartWindowModule)module);
        }

        public void RemoveModule(object module)
        {
            if (module is not IFlowchartWindowModule flowchartModule)
            {
                throw new ArgumentException($"Module must implement {nameof(IFlowchartWindowModule)}", nameof(module));
            }

            RemoveModule((IFlowchartWindowModule)module);
        }

        public void AddModule(IFlowchartWindowModule module)
        {
            _modules.Add(module);

            #region Block Events
            AddResponder<IBlockCreatedResponder>(module);
            AddResponder<IPreBlockDeletionResponder>(module);
            AddResponder<IPostBlockDeletionResponder>(module);
            AddResponder<IPostMultiBlockDeletionResponder>(module);

            AddResponder<IBlockClickResponder>(module);
            AddResponder<IBlockSelectionResponder>(module);
            AddResponder<IMultiBlockSelectionResponder>(module);
            AddResponder<IBlockDeselectionResponder>(module);
            AddResponder<IMultiBlockDeselectionResponder>(module);

            AddResponder<IPreBlockCutResponder>(module);
            AddResponder<IPreMultiBlockCutResponder>(module);
            AddResponder<IPostBlockCutResponder>(module);
            AddResponder<IPostMultiBlockCutResponder>(module);

            #endregion
        }

        public void RemoveModule(IFlowchartWindowModule module)
        {
            _modules.Remove(module);

            #region Block Events
            RemoveResponder<IBlockCreatedResponder>(module);
            RemoveResponder<IPreBlockDeletionResponder>(module);
            RemoveResponder<IPostBlockDeletionResponder>(module);
            RemoveResponder<IPostMultiBlockDeletionResponder>(module);

            RemoveResponder<IBlockClickResponder>(module);
            RemoveResponder<IBlockSelectionResponder>(module);
            RemoveResponder<IMultiBlockSelectionResponder>(module);
            RemoveResponder<IBlockDeselectionResponder>(module);
            RemoveResponder<IMultiBlockDeselectionResponder>(module);

            RemoveResponder<IPreBlockCutResponder>(module);
            RemoveResponder<IPreMultiBlockCutResponder>(module);
            RemoveResponder<IPostBlockCutResponder>(module);
            RemoveResponder<IPostMultiBlockCutResponder>(module);

            #endregion

            module.Dispose();
        }

        public void ClearModules()
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].Dispose();
            }

            _modules.Clear();
            _responderBuckets.Clear();
        }

        #region Notifiers

        #region Block Notifiers

        public void NotifyPreBlockCut(IBlock block) =>
            Broadcast<IPreBlockCutResponder>(res => res.OnPreBlockCut(block));

        public void NotifyPreMultiBlockCut(IList<IBlock> blocks) =>
            Broadcast<IPreMultiBlockCutResponder>(res => res.OnPreMultiBlockCut(blocks));

        public void NotifyPostBlockCut(byte blockId) =>
            Broadcast<IPostBlockCutResponder>(res => res.OnPostBlockCut(blockId));

        public void NotifyPostMultiBlockCut(IList<byte> blockIds) =>
            Broadcast<IPostMultiBlockCutResponder>(res => res.OnPostMultiBlockCut(blockIds));

        public void NotifyBlockCreated(IBlock block) =>
            Broadcast<IBlockCreatedResponder>(res => res.OnBlockCreated(block));

        public void NotifyPreBlockDeleted(IBlock block) =>
            Broadcast<IPreBlockDeletionResponder>(res => res.OnPreBlockDeletion(block));

        public void NotifyPreMultiBlockDeleted(IList<IBlock> blocks) =>
            Broadcast<IPreBlockDeletionResponder>(res => res.OnPreBlockDeletion(blocks));

        public void NotifyPostBlockDeleted(byte blockId) =>
            Broadcast<IPostBlockDeletionResponder>(res => res.OnPostBlockDeletion(blockId));

        public void NotifyPostMultiBlockDeleted(IList<byte> blockIds) =>
            Broadcast<IPostMultiBlockDeletionResponder>(res => res.OnPostMultiBlockDeletion(blockIds));

        public void NotifyBlockClicked(IBlock block, Event evt) =>
            Broadcast<IBlockClickResponder>(res => res.OnBlockClicked(block, evt));

        public void NotifyBlockSelected(IBlock block) =>
            Broadcast<IBlockSelectionResponder>(res => res.OnBlockSelected(block));

        public void NotifyMultiBlocksSelected(IList<IBlock> blocks) =>
            Broadcast<IMultiBlockSelectionResponder>(res => res.OnMultiBlocksSelected(blocks));

        public void NotifyBlockDeselected(IBlock block) =>
            Broadcast<IBlockDeselectionResponder>(res => res.OnBlockDeselected(block));

        public void NotifyMultiBlocksDeselected(IList<IBlock> blocks) =>
            Broadcast<IMultiBlockDeselectionResponder>(res => res.OnMultiBlocksDeselected(blocks));

        public void NotifyBlocksCopied(IList<IBlock> copiedBlocks) =>
            Broadcast<IBlocksCopiedResponder>(res => res.OnBlocksCopied(copiedBlocks));
        #endregion

        #endregion

        private void AddResponder<TResponder>(IFlowchartWindowModule module)
            where TResponder : class
        {
            if (module is not TResponder responder)
            {
                return;
            }

            List<TResponder> bucket = GetOrCreateBucket<TResponder>();
            bucket.Add(responder);
        }

        private void RemoveResponder<TResponder>(IFlowchartWindowModule module)
            where TResponder : class
        {
            if (module is not TResponder responder)
            {
                return;
            }

            Type key = typeof(TResponder);
            if (!_responderBuckets.TryGetValue(key, out IList bucket))
            {
                return;
            }

            List<TResponder> typedBucket = (List<TResponder>)bucket;
            typedBucket.Remove(responder);

            if (typedBucket.Count == 0)
            {
                _responderBuckets.Remove(key);
            }
        }

        private List<TResponder> GetOrCreateBucket<TResponder>()
            where TResponder : class
        {
            Type key = typeof(TResponder);
            if (!_responderBuckets.TryGetValue(key, out IList bucket))
            {
                var newBucket = new List<TResponder>();
                _responderBuckets[key] = newBucket;
                return newBucket;
            }

            return (List<TResponder>)bucket;
        }

        private void Broadcast<TResponder>(Action<TResponder> action)
            where TResponder : class
        {
            if (!_responderBuckets.TryGetValue(typeof(TResponder), out IList bucket))
            {
                return;
            }

            List<TResponder> typedBucket = (List<TResponder>)bucket;
            for (int i = 0; i < typedBucket.Count; i++)
            {
                action(typedBucket[i]);
            }
        }
    }
}