using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt
{
    [RowVisualHandler(menuName: "UnityGeneral",
        contentType: typeof(GameObject),
        typeDisplayName: "GameObject",
        pathToTemplate: "Editor/Uxml/VarRows/UnityGeneral/GameObjectVariableRow")]
    public class GameObjectRowVisualHandler : RowVisualHandler<GameObject>
    {
    }

    [RowVisualHandler(menuName: "UnityGeneral",
        contentType: typeof(Transform),
        typeDisplayName: "Transform",
        pathToTemplate: "Editor/Uxml/VarRows/UnityGeneral/TransformVariableRow")]
    public class TransformRowVisualHandler : RowVisualHandler<Transform>
    {
    }

    [RowVisualHandler(menuName: "UnityGeneral",
        contentType: typeof(UnityObject),
        typeDisplayName: "UnityObject",
        pathToTemplate: "Editor/Uxml/VarRows/UnityGeneral/UnityObjectVariableRow")]
    public class UnityObjectRowVisualHandler : RowVisualHandler<UnityObject>
    {
    }

}