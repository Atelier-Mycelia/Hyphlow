using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// String variable type.
    /// </summary>
    [VariableInfo("Graphic", "String", typeof(string), false)]
    [AddComponentMenu("")]
    [System.Serializable]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class StringVariable : VariableBase<string>
    {
    }

}
