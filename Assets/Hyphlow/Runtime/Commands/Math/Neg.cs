using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Negate a float
    /// </summary>
    [CommandInfo("Math",
                 "Negate",
                 "Negate a float")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Neg : BaseUnaryMathCommand
    {
        public override void OnEnter()
        {
            _outValue.Value = -(_inValue.Value);

            Continue();
        }
    }
}
