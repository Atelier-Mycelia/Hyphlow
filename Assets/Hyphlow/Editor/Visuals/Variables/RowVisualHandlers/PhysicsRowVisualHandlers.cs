using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    [RowVisualHandler(menuName: "Physics",
        contentType: typeof(Collider2D),
        typeDisplayName: "ColliderTwoD",
        pathToTemplate: "Editor/Uxml/VarRows/Physics/ColliderTwoDVariableRow")]
    public class ColliderTwoDRowVisualHandler : RowVisualHandler<Collider2D>
    {
    }

    [RowVisualHandler(menuName: "Physics",
        contentType: typeof(Collider), 
        typeDisplayName: "ColliderThreeD",
        pathToTemplate: "Editor/Uxml/VarRows/Physics/ColliderThreeDVariableRow")]
    public class ColliderThreeDRowVisualHandler : RowVisualHandler<Collider>
    {
    }

    [RowVisualHandler(menuName: "Physics",
        contentType: typeof(Rigidbody2D),
        typeDisplayName: "RigidbodyTwoD",
        pathToTemplate: "Editor/Uxml/VarRows/Physics/RigidbodyTwoDVariableRow")]
    public class RigidbodyTwoDRowVisualHandler : RowVisualHandler<Rigidbody2D>
    {
    }

    [RowVisualHandler(menuName: "Physics",
        contentType: typeof(Rigidbody),
        typeDisplayName: "RigidbodyThreeD",
        pathToTemplate: "Editor/Uxml/VarRows/Physics/RigidbodyThreeDVariableRow")]
    public class RigidbodyThreeDRowVisualHandler : RowVisualHandler<Rigidbody>
    {
    }
}