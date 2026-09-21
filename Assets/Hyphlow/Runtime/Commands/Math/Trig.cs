using UnityEngine.Serialization;
using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Command to execute and store the result of basic trigonometry
    /// </summary>
    [CommandInfo("Math",
                 "Trig",
                 "Command to execute and store the result of basic trigonometry")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Trig : BaseUnaryMathCommand
    {
        public enum Function
        {
            Rad2Deg,
            Deg2Rad,
            ACos,
            ASin,
            ATan,
            Cos,
            Sin,
            Tan
        }
        
        [Tooltip("Trigonometric function to run.")]
        [SerializeField]
[FormerlySerializedAs("function")]
        protected Function function = Function.Sin;
        
        public override void OnEnter()
        {
            switch (function)
            {
                case Function.Rad2Deg:
                    _outValue.Value = _inValue.Value * Mathf.Rad2Deg;
                    break;
                case Function.Deg2Rad:
                    _outValue.Value = _inValue.Value * Mathf.Deg2Rad;
                    break;
                case Function.ACos:
                    _outValue.Value = Mathf.Acos(_inValue.Value);
                    break;
                case Function.ASin:
                    _outValue.Value = Mathf.Asin(_inValue.Value);
                    break;
                case Function.ATan:
                    _outValue.Value = Mathf.Atan(_inValue.Value);
                    break;
                case Function.Cos:
                    _outValue.Value = Mathf.Cos(_inValue.Value);
                    break;
                case Function.Sin:
                    _outValue.Value = Mathf.Sin(_inValue.Value);
                    break;
                case Function.Tan:
                    _outValue.Value = Mathf.Tan(_inValue.Value);
                    break;
                default:
                    break;
            }
            
            Continue();
        }

        public override string GetSummary()
        {
            return function.ToString() + " " + base.GetSummary();
        }
    }
}
