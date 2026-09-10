using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Base class for all simple Unary
    /// </summary>
    [AddComponentMenu("")]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public abstract class BaseUnaryMathCommand : Command
    {
        [Tooltip("Value to be passed in to the function.")]
        [SerializeField]
        [FormerlySerializedAs("inValue")]
        protected FloatData _inValue;

        [Tooltip("Where the result of the function is stored.")]
        [SerializeField]
        [FormerlySerializedAs("outValue")]
        protected FloatData _outValue;
        
        public override Color GetButtonColor()
        {
            return CommandColors.Flow;
        }

        public override string GetSummary()
        {
            string inValueStr = _inValue.VarRef != null ? 
                _inValue.VarRef.Key : 
                _inValue.Value.ToString();
            string outValueStr = _outValue.VarRef != null ? 
                _outValue.VarRef.Key : 
                _outValue.Value.ToString();
            string result = $"in: {inValueStr}, out: {outValueStr}";
            return result;
        }

        public override bool HasReference(IVariable variable)
        {
            return ReferenceEquals(variable, _inValue.VarRef) || 
                ReferenceEquals(variable, _outValue.VarRef);
        }
    }
}
