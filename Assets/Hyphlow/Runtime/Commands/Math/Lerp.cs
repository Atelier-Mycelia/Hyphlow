using UnityEngine.Serialization;
using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Linearly Interpolate from A to B
    /// </summary>
    [CommandInfo("Math",
                 "Lerp",
                 "Linearly Interpolate from A to B")]
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Lerp : Command
    {
        public enum Mode
        {
            Lerp,
            LerpUnclamped,
            LerpAngle
        }
        
        [SerializeField]
[FormerlySerializedAs("mode")]
        protected Mode mode = Mode.Lerp;

        //[Tooltip("LHS Value ")]
        [SerializeField]
[FormerlySerializedAs("a")]
[FormerlySerializedAs("b")]
[FormerlySerializedAs("percentage")]
        protected FloatData a = new FloatData(0), b = new FloatData(1), percentage;

        //[Tooltip("Where the result of the function is stored.")]
        [SerializeField]
[FormerlySerializedAs("outValue")]
        protected FloatData outValue;

        protected override void RefreshVariableDataCache()
        {
            base.RefreshVariableDataCache();
            _variableDataCache.Add(a);
            _variableDataCache.Add(b);
            _variableDataCache.Add(percentage);
            _variableDataCache.Add(outValue);
        }

        public override void OnEnter()
        {
            switch (mode)
            {
                case Mode.Lerp:
                    outValue.Value = Mathf.Lerp(a.Value, b.Value, percentage.Value);
                    break;
                case Mode.LerpUnclamped:
                    outValue.Value = Mathf.LerpUnclamped(a.Value, b.Value, percentage.Value);
                    break;
                case Mode.LerpAngle:
                    outValue.Value = Mathf.LerpAngle(a.Value, b.Value, percentage.Value);
                    break;
                default:
                    break;
            }

            Continue();
        }

        public override string GetSummary()
        {
            return mode.ToString() + " [" + a.Value.ToString() + "-" + b.Value.ToString() + "]";
        }

        public override bool HasReference(IVariable variable)
        {
            return ReferenceEquals(a.VarRef, variable) || 
                ReferenceEquals(b.VarRef, variable) || 
                ReferenceEquals(percentage.VarRef, variable) ||
                   ReferenceEquals(outValue.VarRef, variable);
        }

        public override Color GetButtonColor()
        {
            return CommandColors.Flow;
        }

    }
}
