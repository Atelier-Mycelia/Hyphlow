using UnityEngine.Serialization;
using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Resets the state of all commands and variables in the Flowchart.
    /// </summary>
    [CommandInfo("Variable", 
                 "Reset", 
                 "Resets the state of all commands and variables in the Flowchart.")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Reset : Command
    {   
        [Tooltip("Reset state of all commands in the script")]
        [SerializeField] [FormerlySerializedAs("resetCommands")]
protected bool resetCommands = true;

        [Tooltip("Reset variables back to their default values")]
        [SerializeField] [FormerlySerializedAs("resetVariables")]
protected bool resetVariables = true;

        #region Public members

        public override void OnEnter()
        {
            GetFlowchart().ResetFlowchart(resetCommands, resetVariables);
            Continue();
        }

        public override Color GetButtonColor()
        {
            return CommandColors.Flow;
        }

        #endregion
    }
}
