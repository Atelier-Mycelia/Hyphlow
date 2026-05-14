using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Scope types for Variables.
    /// </summary>
    [MovedFrom(true, "AtMycelia.Hyphlow", "AtMycelia.Amanita.Core", "VariableScope")]
    public enum VScriptScope
    {
        /// <summary> Can only be accessed by commands in the same Flowchart. </summary>
        Private = 0,

        /// <summary> Can be accessed from any command in any Flowchart. </summary>
        Public = 1,

        /// <summary> 
        /// Only here for legacy reasons. We have global variables handled 
        /// as anything in a VariableSourceAsset.
        /// </summary>
        Global = 2,

        /// <summary>
        /// In case we ever need it for something. This is not used anywhere in the codebase
        /// (at the time of this writing), but it is here for completeness.
        /// </summary>
        Null = 99,
    }
}