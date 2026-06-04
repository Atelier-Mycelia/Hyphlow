using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Base class for Flowchart nodes.
    /// </summary>
    [AddComponentMenu("")]
    [MovedFrom(true, sourceNamespace: "Fungus", sourceAssembly: "Fungus")]
    public class Node : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("nodeRect")]
protected Rect nodeRect = new Rect(0, 0, 120, 30);
        [SerializeField] [FormerlySerializedAs("tint")]
protected Color tint = Color.white;
        [SerializeField] [FormerlySerializedAs("useCustomTint")]
protected bool useCustomTint = false;

        #region Public members

        public virtual Rect _NodeRect { get { return nodeRect; } set { nodeRect = value; } }
        public virtual Color Tint { get { return tint; } set { tint = value; } }
        public virtual bool UseCustomTint { get { return useCustomTint; } set { useCustomTint = value; } }

        #endregion
    }
}