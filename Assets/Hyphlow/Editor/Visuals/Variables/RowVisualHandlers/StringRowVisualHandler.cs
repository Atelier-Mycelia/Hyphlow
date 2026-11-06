using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    [RowVisualHandler(menuName: "Graphics",
        contentType: typeof(string),
        typeDisplayName: "String",
        pathToTemplate: "Editor/Uxml/VarRows/StringVariableRow")]
    public class StringRowVisualHandler : RowVisualHandler<string>
    {
        protected override void BuildValueEditor(VisualElement valueField)
        {
            // The base class already makes the field delayed, so change events only fire
            // when the user presses enter or leaves the field
            base.BuildValueEditor(valueField);

            if (valueField is TextField textField)
            {
                textField.multiline = true;
                // A text field that got its value before reaching a panel can show up empty
                textField.RegisterCallback<AttachToPanelEvent>(_ => RefreshValue());
            }
        }
    }
}
