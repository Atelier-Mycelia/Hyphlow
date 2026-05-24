using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Collider variable type.
    /// </summary>
    [VariableInfo("Physics/ThreeD", "Collider", typeof(Collider), false)]
    [AddComponentMenu("")]
    [System.Serializable]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class ColliderVariable : VariableBase<Collider>
    { }

    

}