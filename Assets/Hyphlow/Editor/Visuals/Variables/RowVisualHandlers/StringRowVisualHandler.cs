using UnityEngine.UIElements;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    [RowVisualHandler(menuName: "Graphics",
        contentType: typeof(string),
        typeDisplayName: "String",
        pathToTemplate: "Editor/Uxml/VarRows/StringVariableRow")]
    public class StringRowVisualHandler : RowVisualHandler<object>
    {
        protected override void RegisterVisualElements()
        {
            base.RegisterVisualElements();
            _textValueField = ValueField as TextField;

            if (_textValueField == null)
            {
                Debug.LogError($"StringRowVisualHandler could not find a TextField named in the UXML template. Check your UXML.");
                return;
            }

            _textValueField.isDelayed = true; // This way, the change events only fire when the user presses enter
            _textValueField.multiline = true;
            
        }

        protected TextField _textValueField;
        protected override void ToggleValueChangeSubs(bool on)
        {
            base.ToggleValueChangeSubs(on);
            if (_textValueField == null) return;
            if (on)
            {
                _textValueField.RegisterValueChangedCallback(OnTextFieldChanged);
                _textValueField.RegisterCallback<AttachToPanelEvent>(OnTextFieldAttachedToPanel); //
            }
            else
            {
                _textValueField.UnregisterValueChangedCallback(OnTextFieldChanged);
                _textValueField.UnregisterCallback<AttachToPanelEvent>(OnTextFieldAttachedToPanel); 
            }
        }

        protected virtual void OnTextFieldChanged(ChangeEvent<string> evt)
        {
            TriggerValueFieldChanged(evt.newValue);
        }

        protected virtual void OnTextFieldAttachedToPanel(AttachToPanelEvent evt)
        {
            ApplyVarValueToValueField();
        }

        protected override void ApplyVarValueToValueField()
        {
            _textValueField.schedule.Execute(() =>
            {
                if (_textValueField == null) return;
                string textToApply = (string)_currentVariable.BoxedValue;
                _textValueField.SetValueWithoutNotify(textToApply);
                _textValueField.MarkDirtyRepaint();
            }).ExecuteLater(1); // Delay by 1 frame to avoid UITK binding issues
        }

    }
}