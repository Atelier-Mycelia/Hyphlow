using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Scope types for variables, Blocks, anything that is meant to have its
    /// access and visibility controlled. The exact meaning of these scopes
    /// can vary based on the context in which they are used, but in general...
    /// Private = only accessible within the local context (e.g., a specific Flowchart).
    /// Public = accessible from any context that can refer to the container of
    /// whatever has a VScriptScope. For example, if a Flowchart has a public variable,
    /// then any other Flowchart in the scene should be able to access that variable
    /// by referring to the Flowchart that contains it.
    /// Global = accessible from anywhere, but in practice, all global IVariables
    /// should be stored in a VariableSourceAsset.
    /// Null = not used for variables, but included for completeness and
    /// potential future use.
    /// </summary>
    [MovedFrom(true, "AtMycelia.Hyphlow", "AtMycelia.Amanita.Core", "VScriptScope")]
    public enum AccessScope
    {
        /// <summary>
        /// Can only be accessed by the local space of the execution context (for example, 
        /// a particular Flowchart).
        /// </summary>
        Private = 0,

        /// <summary>
        /// Allowed to be accessed from anything that can refer to a data structure
        /// that has a VScriptScope.
        /// </summary>
        Public = 1,

        /// <summary> 
        /// As far as variables are concerned, this is only here for legacy reasons.
        /// In practice, all IVariables should be in a VariableSourceAsset.
        /// </summary>
        Global = 2,

        /// <summary>
        /// We might at some point implement the option to make Flowcharts that inherit from other
        /// Flowcharts, and this would be the scope for things that are meant to be inherited but
        /// otherwise not accessed from other contexts.
        /// </summary>
        Protected = 3, 

        /// <summary>
        /// This is for things that are meant to be accessed from other contexts, but not modified.
        /// </summary>
        ReadOnly = 4,

        /// <summary>
        /// In case we ever need it for something. This is not used anywhere in the codebase
        /// (at the time of this writing), but it is here for completeness.
        /// </summary>
        Null = 99,
    }
}