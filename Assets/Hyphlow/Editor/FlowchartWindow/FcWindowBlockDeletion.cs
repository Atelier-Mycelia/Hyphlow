using System.Collections.Generic;
using UnityEditor;

namespace AtMycelia.Hyphlow.EditorExt
{
    public class FcWindowBlockDeletion
    {
        public void Execute(FlowchartContext ctx)
        {
            var selection = ctx.Selection;
            var selected = selection.Blocks;
            if (selected == null || selected.Count == 0)
                return;

            // We'll handle the deletion here instead of passing it to FcWindowEditing since we
            // want to be able to undo the deletion of multiple blocks as a single action.
            // That, and to keep the new flowchart window from needing to involve FcWindowEditing
            // (that class is for the legacy window only).
            Flowchart fChart = ctx.Flowchart;
            selection.ClearBlocks();
            selection.ClearCommands();
            
            if (selected.Count == 1)
            {
                IBlock toDelete = selected[0];

                fChart.Remove(toDelete);

                ushort id = toDelete.ItemId;

                DestroyThoroughly(toDelete);
            }
            else
            {
                fChart.RemoveMultiBlocks(selected);

                IList<byte> blockIds = new List<byte>();
                for (int i = 0; i < selected.Count; i++)
                {
                    blockIds.Add(selected[i].ItemId);
                }

                for (int i = 0; i < selected.Count; i++)
                {
                    var toDelete = selected[i];
                    DestroyThoroughly(toDelete);
                }

            }

            ctx.ForceRepaintCount++;
        }

        private void DestroyThoroughly(IBlock block)
        {
            // Destroy each command on the block
            var commands = block.CommandList;
            for (int i = 0; i < commands.Count; i++)
            {
                ICommand cmd = commands[i];
                if (cmd != null && cmd is UnityObjectMuscariable cmdUnityObj)
                    Undo.DestroyObjectImmediate(cmdUnityObj);
            }

            // Destroy any event handler
            if (block.EventHandler != null && block.EventHandler is UnityObjectMuscariable ehUnityObj)
                Undo.DestroyObjectImmediate(ehUnityObj);

            var fc = block.GetFlowchart();

            // Destroy the block itself
            Block legacyBlock = block as Block;
            if (legacyBlock != null)
            {
                Undo.DestroyObjectImmediate(legacyBlock);
            }
            
            Selection.activeGameObject = fc.gameObject;

        }

    }
}