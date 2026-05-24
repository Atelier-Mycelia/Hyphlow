using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    /// <summary>
    /// Backwards-compatible wrapper for older call sites.
    /// Prefer IStringVarSubstitutor with StringVarSubstitutionService.Shared.
    /// </summary>
    public class StringVarSubstituter : IStringVarSubstitutor
    {
        private readonly IStringVarSubstitutor _inner;

        public const string SubstituteVariableRegexString = StringVarSubstitutionService.SubstituteVariableRegexString;

        public StringVarSubstituter()
            : this(StringVarSubstitutionService.Shared)
        {
        }

        public StringVarSubstituter(IStringVarSubstitutor inner)
        {
            _inner = inner ?? StringVarSubstitutionService.Shared;
        }

        public string SubstituteVariables(string input, IVariableSource variableSource)
        {
            return _inner.SubstituteVariables(input, variableSource);
        }

        /// <summary>
        /// Finds all variables in the input string that match the format {$VarName} and
        /// adds them to the provided list.
        /// </summary>
        public void DetermineSubstitutionVariables(string input, IVariableSource variableSource, 
            IList<IVariable> holdsResults)
        {
            _inner.DetermineSubstitutionVariables(input, variableSource, holdsResults);
        }
    }
}