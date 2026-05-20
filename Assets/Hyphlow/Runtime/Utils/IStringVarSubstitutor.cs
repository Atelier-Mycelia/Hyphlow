using System;

namespace AtMycelia.Hyphlow
{
    public interface IStringVarSubstitutor
    {
        string SubstituteVariables(string input, IVariableSource variableSource);
    }
}