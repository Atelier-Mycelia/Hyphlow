using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Command to execute and store the result of a Sign
    /// </summary>
    [CommandInfo("Math",
                 "Sign",
                 "Command to execute and store the result of a Sign")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Sign : BaseUnaryMathCommand
    {
        public override void OnEnter()
        {
            _outValue.Value = Mathf.Sign(_inValue.Value);

            Continue();
        }
    }
}
