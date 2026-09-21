using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    [RowVisualHandler(menuName: "Graphics",
        contentType: typeof(Color),
        typeDisplayName: "Color",
        pathToTemplate: "Editor/Uxml/VarRows/Graphic/ColorVariableRow")]
    public class ColorRowVisualHandler : RowVisualHandler<Color>
    {
        protected override void RegisterVisualElements()
        {
            base.RegisterVisualElements();
            _colorValueField = ValueField as ColorField;

            if (_colorValueField == null)
            {
                Debug.LogError($"ColorVariableRow could not find a ColorField named in the UXML template " +
                    $"for {GetType().Name}. Check your UXML.");
                return;
            }
        }

        protected ColorField _colorValueField;

        protected override void ToggleValueChangeSubs(bool on)
        {
            base.ToggleValueChangeSubs(on);
            if (_colorValueField == null)
            {
                return;
            }

            if (on)
            {
                _colorValueField.RegisterValueChangedCallback(OnColorFieldChanged);
            }
            else
            {
                _colorValueField.UnregisterValueChangedCallback(OnColorFieldChanged);
            }
        }

        protected virtual void OnColorFieldChanged(ChangeEvent<Color> evt)
        {
            TriggerValueFieldChanged(evt.newValue);
        }

        protected override void ApplyVarValueToValueField()
        {
            if (_colorValueField == null || _currentVariable == null)
            {
                return;
            }

            Color currentCol = (Color)_currentVariable.BoxedValue;
            _colorValueField.SetValueWithoutNotify(currentCol);
            _colorValueField.MarkDirtyRepaint();
        }
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