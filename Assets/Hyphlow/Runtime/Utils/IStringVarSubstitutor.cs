using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    public interface IStringVarSubstitutor
    {
        string SubstituteVariables(string input, IVariableSource variableSource);

        /// <summary>
        /// Determines which variables are being substituted in the input string, and 
        /// adds them to the holdsResults list.
        /// </summary>
        void DetermineSubstitutionVariables(string input, IVariableSource variableSource,
            IList<IVariable> holdsResults);
    }
}