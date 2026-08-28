using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Multiplicative Inverse of a float (1/f)
    /// </summary>
    [CommandInfo("Math",
                 "Inverse",
                 "Multiplicative Inverse of a float (1/f)")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Inv : BaseUnaryMathCommand
    {
        public override void OnEnter()
        {
            var v = _inValue.Value;

            _outValue.Value = v != 0 ? (1.0f / _inValue.Value) : 0.0f;

            Continue();
        }
    }
}
