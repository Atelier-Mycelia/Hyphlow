using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Marks a position in the command list for execution to jump to.
    /// </summary>
    [CommandInfo("Flow", 
                 "Label", 
                 "Marks a position in the command list for execution to jump to.")]
    [AddComponentMenu("")]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Label : Command, IHasKey
    {
        [Tooltip("Display name for the label")]
        [SerializeField] protected StringData _key = new StringData("");

        public virtual string Key
        {
            get => _key.Value;
            set => _key.Value = value;
        }

        public override bool SkipExecution => true;

        public override void OnEnter()
        {
            Continue();
        }

        public override string GetSummary()
        {
            string result = _key.Value;
            if (_key.RepresentingVar)
            {
                result = $"{_key.VarRef.Key} ({_key.Value})";
            }
            return result;
        }

        public override Color GetButtonColor()
        {
            return CommandColors.Label;
        }

        public override void ApplyBackwardsCompatibility()
        {
            base.ApplyBackwardsCompatibility();
            if (!string.IsNullOrEmpty(key))
            {
                _key.Value = key;
                key = "";
            }
        }

        [HideInInspector]
        [SerializeField] protected string key = "";
    }
}
