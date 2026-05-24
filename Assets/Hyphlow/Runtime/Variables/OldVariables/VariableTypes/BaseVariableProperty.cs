using UnityEngine;

using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    [AddComponentMenu("")]
[MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public abstract class BaseVariableProperty : Command
    {
        public enum GetSet
        {
            Get,
            Set,
        }

        public GetSet getOrSet = GetSet.Get;
    }
}
