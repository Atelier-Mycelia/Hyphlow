using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Command to execute and store the result of a Round
    /// </summary>
    [CommandInfo("Math",
                 "Round",
                 "Command to execute and store the result of a Round.")]
    [AddComponentMenu("")]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Round : BaseUnaryMathCommand
    {
        public enum Mode
        {
            Round,
            Floor,
            Ceil
        }

        [Tooltip("Mode; Round (closest), floor(smaller) or ceil(bigger).")]
        [SerializeField]
        [FormerlySerializedAs("function")]
        protected Mode _function = Mode.Round;

        [SerializeField]
        protected IntegerData _decimalPlaces = new IntegerData(0);

        [SerializeField]
        protected BooleanData _fixedDecimalPlaces = new BooleanData(false);

        [SerializeField]
        protected StringData _stringOutValue;

        public override void OnEnter()
        {
            float value = _inValue.Value;

            float scale = Mathf.Pow(10f, _decimalPlaces);
            float scaled = value * scale;
            float result;

            switch (_function)
            {
                case Mode.Round:
                    result = Mathf.Round(scaled);
                    break;
                case Mode.Floor:
                    result = Mathf.Floor(scaled);
                    break;
                case Mode.Ceil:
                    result = Mathf.Ceil(scaled);
                    break;
                default:
                    result = scaled;
                    break;
            }

            float finalValue = result / scale;
            _outValue.Value = finalValue;

            // --- STRING OUTPUT SUPPORT ---
            if (_stringOutValue != null)
            {
                if (_fixedDecimalPlaces)
                {
                    // Example: decimalPlaces = 2 → "F2"
                    _stringOutValue.Value = finalValue.ToString("F" + _decimalPlaces);
                }
                else
                {
                    // Normal float → string conversion
                    _stringOutValue.Value = finalValue.ToString();
                }
            }

            Continue();
        }

        public override string GetSummary()
        {
            string roundDir = GetRoundDirection();
            string result = $"Round {_inValue}{roundDir} to {_decimalPlaces} decimal places";
            return result;
        }

        private string GetRoundDirection()
        {
            return _function switch
            {
                // Spaces are the for the formatting of the summary string so that the
                // text is aligned in the inspector.
                Mode.Ceil => " UP",
                Mode.Floor => " DOWN",
                _ => ""
            };
        }
        public override bool HasReference(IVariable variable)
        {
            return base.HasReference(variable) || 
                ReferenceEquals(variable, _decimalPlaces.VarRef);
        }
    }
}
