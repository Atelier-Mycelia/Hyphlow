using UnityEngine.Serialization;
using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Stops executing the named Block.
    /// </summary>
    [CommandInfo("Flow", 
                 "Stop Block", 
                 "Stops executing the named Block")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class StopBlock : Command, IBlockCaller
    {
        [Tooltip("Flowchart containing the Block. If none is specified, the parent Flowchart is used.")]
        [SerializeField] [FormerlySerializedAs("flowchart")]
protected Flowchart flowchart;

        [Tooltip("Name of the Block to stop")]
        [SerializeField] [FormerlySerializedAs("blockName")]
protected StringData blockName = new StringData("");

        public string GetLocationIdentifier()
        {
            return LocationIdentifier;
        }

        public string GetLocationIdentifier()
        {
            return LocationIdentifier;
        }

        #region Public members

        public override void OnEnter()
        {
            if (blockName.Value == "")
            {
                Continue();
            }

            if (flowchart == null)
            {
                flowchart = (Flowchart)GetFlowchart();
            }

            var block = flowchart.GetBlock(blockName.Value);
            if (block == null ||
                !block.IsExecuting)
            {
                Continue();
            }

            block.Stop();

            Continue();
        }

        public override string GetSummary()
        {
            return blockName;
        }
            
        public override Color GetButtonColor()
        {
            return new Color32(253, 253, 150, 255);
        }

        public override bool HasReference(IVariable variable)
        {
            return ReferenceEquals(blockName.VarRef, variable) || base.HasReference(variable);
        }

        public bool MayCallBlock(IBlock block)
        {
            if(flowchart != null)
                return block == flowchart.GetBlock(blockName.Value);
            return false;
        }

        #endregion
    }
}