using UnityEngine;

using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Type of log message. Maps directly to Unity's log types.
    /// </summary>
    public enum DebugLogType
    {
        /// <summary> Informative log message. </summary>
        Info,
        /// <summary> Warning log message. </summary>
        Warning,
        /// <summary> Error log message. </summary>
        Error
    }

    /// <summary>
    /// Writes a log message to the debug console.
    /// </summary>
    [CommandInfo("Scripting", 
                 "Debug Log", 
                 "Writes a log message to the debug console.")]
    [AddComponentMenu("")]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class DebugLog : Command 
    {
        [Tooltip("Display type of debug log info")]
        [FormerlySerializedAs("logType")]
        [SerializeField] protected DebugLogType _logType;

        [Tooltip("Text to write to the debug log. Supports variable substitution, e.g. {$Myvar}")]
        [FormerlySerializedAs("logMessage")]
        [SerializeField] protected StringDataMulti _logMessage = new StringDataMulti();

        #region Public members

        public override void OnEnter()
        {
            var flowchart = GetFlowchart();
            string message = _logMessage.Value;

            if (flowchart != null)
            {
                message = StringVarSubstituter.SubstituteVariables(message, flowchart);
            }

            switch (_logType)
            {
                case DebugLogType.Info:
                    Debug.Log(message);
                    break;
                case DebugLogType.Warning:
                    Debug.LogWarning(message);
                    break;
                case DebugLogType.Error:
                    Debug.LogError(message);
                    break;
            }

            Continue();
        }

        public override string GetSummary()
        {
            return _logMessage.GetDescription();
        }

        public override Color GetButtonColor()
        {
            return CommandColors.Flow;
        }

        public override bool HasReference(IVariable variable)
        {
            return ReferenceEquals(_logMessage.VarRef, variable) || base.HasReference(variable);
        }

        #endregion

        #region Editor caches
#if UNITY_EDITOR
        protected override void RefreshVariableCache()
        {
            base.RefreshVariableCache();
            if (ParentBlock == null)
            {
                return;
            }

            var fc = ParentBlock.ParentFlowchart;
            if (fc == null)
            {
                return;
            }

            StringVarSubstituter.DetermineSubstitutionVariables(_logMessage.Value, fc, _referencedVariables);
        }
#endif
        #endregion Editor caches

    }
}
