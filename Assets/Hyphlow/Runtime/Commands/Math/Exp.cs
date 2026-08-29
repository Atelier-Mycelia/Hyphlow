using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Command to execute and store the result of a Exp
    /// </summary>
    [CommandInfo("Math",
                 "Exp",
                 "Command to execute and store the result of a Exp")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Exp : BaseUnaryMathCommand
    {
        public override void OnEnter()
        {
            _outValue.Value = Mathf.Exp(_inValue.Value);

            Continue();
        }
    }
}
