using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    [RowVisualHandler(menuName: "Graphics",
        contentType: typeof(Color),
        typeDisplayName: "Color",
        pathToTemplate: "Editor/Uxml/VarRows/Graphic/ColorVariableRow")]
    public class ColorRowVisualHandler : RowVisualHandler<Color>
    {
    }

    [RowVisualHandler(menuName: "Graphics",
        contentType: typeof(Texture),
        typeDisplayName: "Texture",
        pathToTemplate: "Editor/Uxml/VarRows/Graphic/TextureVariableRow")]
    public class TextureRowVisualHandler : RowVisualHandler<Texture>
    {

    }

    [RowVisualHandler(menuName: "Graphics",
        contentType: typeof(Material),
        typeDisplayName: "Material",
        pathToTemplate: "Editor/Uxml/VarRows/Graphic/MaterialVariableRow")]
    public class MaterialRowVisualHandler : RowVisualHandler<Material>
    {

    }

    [RowVisualHandler(menuName: "Graphics",
        contentType: typeof(Sprite),
        typeDisplayName: "Sprite",
        pathToTemplate: "Editor/Uxml/VarRows/Graphic/SpriteVariableRow")]
    public class SpriteRowVisualHandler : RowVisualHandler<Sprite>
    {

    }

    [RowVisualHandler(menuName: "Graphics",
        contentType: typeof(Animator),
        typeDisplayName: "Animator",
        pathToTemplate: "Editor/Uxml/VarRows/Graphic/AnimatorVariableRow")]
    public class AnimatorRowVisualHandler : RowVisualHandler<Animator>
    {

    }
}
