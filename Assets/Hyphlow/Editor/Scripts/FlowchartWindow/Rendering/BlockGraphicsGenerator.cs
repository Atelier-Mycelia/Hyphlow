using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    public class BlockGraphicsGenerator : IBlockGraphicsGenerator
    {
        public virtual BlockGraphics GenerateFor(IBlock block)
        {
            var graphics = new BlockGraphics();

            blockGraphicsUniqueListWorkSpace.Clear();
            blockGraphicsConnectedWorkSpace.Clear();
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
                block.RefreshConnectedBlockCache(ref blockGraphicsConnectedWorkSpace);
                foreach (var connectedBlock in blockGraphicsConnectedWorkSpace)
                {
                    if (connectedBlock == block ||
                        blockGraphicsUniqueListWorkSpace.Contains(connectedBlock))
                    {
                        continue;
                    }
                    blockGraphicsUniqueListWorkSpace.Add(connectedBlock);
                }

                if (blockGraphicsUniqueListWorkSpace.Count > 1)
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

        static protected IList<IBlock> blockGraphicsUniqueListWorkSpace = new List<IBlock>();
        static protected IList<IBlock> blockGraphicsConnectedWorkSpace = new List<IBlock>();
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