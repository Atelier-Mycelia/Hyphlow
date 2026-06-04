using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Material variable type.
    /// </summary>
    [VariableInfo("Graphic", "Material", typeof(Material), false)]
    [AddComponentMenu("")]
    [System.Serializable]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class MaterialVariable : VariableBase<Material>
    {
    }

}
