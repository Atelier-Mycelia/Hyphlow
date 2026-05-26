using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    public class BlockGraphicsGenerator : IBlockGraphicsGenerator
    {
        public virtual BlockGraphics GenerateFor(IBlock block)
        {
            var graphics = new BlockGraphics();

            _blockGraphicsUniqueListWorkSpace.Clear();
            _blockGraphicsConnectedWorkSpace.Clear();
            Color defaultTint;
            if (block.EventHandler != null)
            {
                //graphics.offTexture = HyphlowEditorSysAssets.EventNodeOff;
                //graphics.onTexture = HyphlowEditorSysAssets.EventNodeOn;
                defaultTint = HyphlowConstants.DefaultEventBlockTint;
            }
            else
            {
                // Count the number of unique connections (excluding self references)
                block.RefreshConnectedBlockCache(ref _blockGraphicsConnectedWorkSpace);
                foreach (var connectedBlock in _blockGraphicsConnectedWorkSpace)
                {
                    if (connectedBlock == block ||
                        _blockGraphicsUniqueListWorkSpace.Contains(connectedBlock))
                    {
                        continue;
                    }
                    _blockGraphicsUniqueListWorkSpace.Add(connectedBlock);
                }

                if (_blockGraphicsUniqueListWorkSpace.Count > 1)
                {
                    //graphics.offTexture = HyphlowEditorSysAssets.ChoiceNodeOff;
                    //graphics.onTexture = HyphlowEditorSysAssets.ChoiceNodeOn;
                    defaultTint = HyphlowConstants.DefaultChoiceBlockTint;
                }
                else
                {
                    //graphics.offTexture = HyphlowEditorSysAssets.ProcessNodeOff;
                    //graphics.onTexture = HyphlowEditorSysAssets.ProcessNodeOn;
                    defaultTint = HyphlowConstants.DefaultProcessBlockTint;
                }
            }

            graphics.tint = block.UseCustomTint ? 
                block.Tint : 
                defaultTint * HyphlowEditorPreferences.flowchartBlockTint;

            return graphics;
        }

        static protected IList<IBlock> _blockGraphicsUniqueListWorkSpace = new List<IBlock>();
        static protected IList<IBlock> _blockGraphicsConnectedWorkSpace = new List<IBlock>();
    }

    public interface IBlockGraphicsGenerator
    {
        BlockGraphics GenerateFor(IBlock block);
    }

    public struct BlockGraphics
    {
        internal Color tint;
        internal Texture2D onTexture;
        internal Texture2D offTexture;
    }
}