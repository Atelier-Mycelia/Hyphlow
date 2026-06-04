using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Stop executing the Block that contains this command.
    /// </summary>
    [CommandInfo("Flow", 
                 "Stop", 
                 "Stop executing the Block that contains this command.")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Stop : Command
    {
        #region Public members

        public override void OnEnter()
        {
            OnExit();
            ParentBlock.Stop();
        }

        public override Color GetButtonColor()
        {
            return CommandColors.Flow;
        }

        #endregion
    }
}
