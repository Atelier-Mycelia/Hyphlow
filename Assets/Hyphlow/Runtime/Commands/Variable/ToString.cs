using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Stores the result of a ToString on given variable in a string.
    /// </summary>
    [CommandInfo("Variable",
                 "To String",
                 "Stores the result of a ToString on given variable in a string.")]
    [AddComponentMenu("")]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class ToString : Command
    {
        [Tooltip("Target variable to get a string ver of.")]
        [SerializeField] protected VariableReference _targetVariable;

        [Tooltip("Where the string ver gets stored.")]
        [SerializeField] [ContentTypeConstraint(typeof(string))]
        protected VariableReference _outValue;

        [Tooltip("Optional formatting (especially useful for numerics).")]
        [SerializeField] protected StringData _format;

        public override void OnEnter()
        {
            ValidateInputs(out bool success);
            if (!success)
            {
                Continue();
                return;
            }

            string targVal = DecideTargValStr();
            _outValue.SetValue(targVal);

            Continue();
        }

        private void ValidateInputs(out bool success)
        {
            success = false;
            bool validVarInputs = _targetVariable.Variable != null && _outValue.Variable != null;
            if (!validVarInputs)
            {
                string warningMessage = $"ToString Command requires both a target" +
                    $"variable and an output variable to be set. Target variable: " +
                    $"{_targetVariable.Variable?.Key ?? "null"},\nOutput variable: " +
                    $"{_outValue.Variable?.Key ?? "null"}";
                Debug.LogWarning(warningMessage);
                Continue();
                return;
            }
            success = true;
        }

        private string DecideTargValStr()
        {
            var realTargVar = _targetVariable.Variable;
            var targVal = realTargVar.BoxedValue;
            string result;

            bool shouldFormat = _format != null && !string.IsNullOrEmpty(_format.Value);
            if (shouldFormat)
            {
                result = targVal != null ? 
                    string.Format("{0:" + _format.Value + "}", realTargVar.BoxedValue) : 
                    "null";
            }
            else
            {
                result = targVal != null ?
                            targVal.ToString() :
                            "null";
            }

            return result;
        }

        public override string GetSummary()
        {
            if (_targetVariable.Variable == null)
            {
                return "Error: Target Variable not selected";
            }

            if (_outValue.Variable == null)
            {
                return "Error: outValue not set";
            }

            string result = $"{_targetVariable.VarKey}.ToString into {_outValue.VarKey}";
            return result;
        }

        public override bool HasReference(IVariable variable)
        {
            return ReferenceEquals(variable, this._targetVariable.Variable) ||
                ReferenceEquals(_outValue.Variable, variable);
        }

        public override Color GetButtonColor()
        {
            return CommandColors.Variables;
        }

        #region Backwards Compatibility
        public override void ApplyBackwardsCompatibility()
        {
            base.ApplyBackwardsCompatibility();
            if (_oldVariable != null)
            {
                _targetVariable.Variable = _oldVariable;
            }
            if (_oldOutValue != null)
            {
                _outValue.Variable = _oldOutValue;
            }

            _oldVariable = null;
            _oldOutValue = null;
        }

        [SerializeField]
        [FormerlySerializedAs("variable")]
        protected Variable _oldVariable;

        [SerializeField]
        [FormerlySerializedAs("outValue")]
        protected StringVariable _oldOutValue;
        #endregion
    }
}
