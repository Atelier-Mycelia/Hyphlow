using System;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public static class VarSetSignals
    {
        /// <summary>
        /// Should execute right before a variable is to be added to a VSA.
        /// </summary>
        public static Action<VariableSet, IVariable> PreVariableAdded = delegate { };

        /// <summary>
        /// Should execute right before a variable is to be removed from a VSA.
        /// </summary>
        public static Action<VariableSet, IVariable> PreVariableRemoved = delegate { };


        public static Action<VariableSet, IVariable> VariableAdded = delegate { };
        public static Action<VariableSet, IVariable> VariableRemoved = delegate { };

        public static Action<VariableSet> VsaEnabled = delegate { };
        public static Action<VariableSet> VsaDisabled = delegate { };

        /// <summary>
        /// Should trigger when a VSA is destroyed. The first string param is the name
        /// of the VSA, and the second is its Uid.
        /// </summary>
        public static Action <string, string> VsaDestroyed = delegate { };
    }
}