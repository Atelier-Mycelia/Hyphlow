using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Command to execute and store the result of a Abs
    /// </summary>
    [CommandInfo("Math",
                 "Abs",
                 "Command to execute and store the result of a Abs")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Abs : BaseUnaryMathCommand
    {
        public override void OnEnter()
        {
            _outValue.Value = Mathf.Abs(_inValue.Value);

            Continue();
        }
    }
}
