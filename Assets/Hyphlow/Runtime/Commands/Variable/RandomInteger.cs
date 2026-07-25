using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Sets a numeric variable to a random value in the defined range.
    /// </summary>
    [CommandInfo("Variable",
                 "Random Integer",
                 "Sets a numeric variable to a random integer value" +
                 "in the defined range.")]
    [AddComponentMenu("")]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class RandomInteger : Command
    {
        [Tooltip("The variable that will get its value set. Can be a float or a double.")]
        [ContentTypeConstraint(typeof(float), typeof(double), typeof(int))]
        [SerializeField]
        protected VariableReference _variable;

        [Tooltip("Minimum value for random range")]
        [SerializeField]
        [FormerlySerializedAs("minValue")]
        protected IntegerData _minValue;

        [Tooltip("Maximum value for random range")]
        [SerializeField]
        [FormerlySerializedAs("maxValue")]
        protected IntegerData _maxValue;

        protected override void RefreshVariableDataCache()
        {
            base.RefreshVariableDataCache();
            _variableDataCache.Add(_minValue);
            _variableDataCache.Add(_maxValue);
        }

        public override void OnEnter()
        {
            if (_variable != null)
            {
                var valChosen = Random.Range(_minValue.Value, _maxValue.Value);
                _variable.SetValue(valChosen);
            }

            Continue();
        }

        public override string GetSummary()
        {
            string result = _variable == null ?
                "Error: Variable not selected" :
                $"Set {_variable.Variable.Key} between {_minValue.Value} and {_maxValue.Value}";

            return result;
        }

        public override bool HasReference(IVariable variable)
        {
            return ReferenceEquals(variable, this._variable) ||
                ReferenceEquals(_minValue.integerRef, variable) ||
                ReferenceEquals(_maxValue.integerRef, variable);
        }

        public override Color GetButtonColor()
        {
            return new Color32(253, 253, 150, 255);
        }

        public override void ApplyBackwardsCompatibility()
        {
            base.ApplyBackwardsCompatibility();
            if (_oldVariable != null)
            {
                _variable.Variable = _oldVariable;
                _oldVariable = null;
            }
        }

        [VariableProperty(typeof(IntegerVariable))]
        [FormerlySerializedAs("variable")]
        [HideInInspector]
        [SerializeField] protected IntegerVariable _oldVariable;

    }
}
