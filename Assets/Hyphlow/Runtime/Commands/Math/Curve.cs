using UnityEngine.Serialization;
using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Pass a value through an AnimationCurve
    /// </summary>
    [CommandInfo("Math",
                 "Curve",
                 "Pass a value through an AnimationCurve")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Curve : BaseUnaryMathCommand
    {
        [SerializeField]
[FormerlySerializedAs("curve")]
        protected AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        public override void OnEnter()
        {
            _outValue.Value = curve.Evaluate(_inValue.Value);

            Continue();
        }
    }
}
