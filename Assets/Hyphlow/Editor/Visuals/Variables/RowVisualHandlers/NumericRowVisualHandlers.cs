using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    public abstract class NumericRowVisualHandler<T> : RowVisualHandler<T>
    {
        protected override void RegisterVisualElements()
        {
            base.RegisterVisualElements();
            _numericField = ValueField as TextValueField<T>;
            
            if (_numericField == null)
            {
                Debug.LogError($"NumericRowVisualHandler could not find a TextValueField<{typeof(T).Name}> " +
                    $"named in the UXML template. Check your UXML.");
                return;
            }

            _numericField.isDelayed = true; // So changes only fire on enter or focus lost
        }

        protected TextValueField<T> _numericField;

        protected override void ToggleValueChangeSubs(bool on)
        {
            base.ToggleValueChangeSubs(on);
            if (on)
            {
                _numericField.RegisterValueChangedCallback(OnValueFieldChanged);
            }
            else
            {
                _numericField.UnregisterValueChangedCallback(OnValueFieldChanged);
            }
        }

        protected virtual void OnValueFieldChanged(ChangeEvent<T> evt)
        {
            TriggerValueFieldChanged(evt.newValue);
        }

        protected override void ApplyVarValueToValueField()
        {
            _numericField?.SetValueWithoutNotify((T)_currentVariable.BoxedValue);
            _numericField.MarkDirtyRepaint();
        }
    }

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(float), 
        typeDisplayName: "Float",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/FloatVariableRow")]
    public class FloatRowVisualHandler : NumericRowVisualHandler<float>
    {
        
    }

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(double),
        typeDisplayName: "Double",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/DoubleVariableRow")]
    public class DoubleRowVisualHandler : NumericRowVisualHandler<double>
    {

    }

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(int), 
        typeDisplayName: "Integer",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/IntVariableRow")]
    public class IntRowVisualHandler : NumericRowVisualHandler<int>
    {
        
    }

    // Bools work off toggles, not text value fields, so...
    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(bool),
        typeDisplayName: "Boolean",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/BoolVariableRow")]
    public class BoolRowVisualHandler : RowVisualHandler<bool>
    {
        protected override void RegisterVisualElements()
        {
            base.RegisterVisualElements();
            _toggleField = ValueField as Toggle;
            if (_toggleField == null)
            {
                Debug.LogError($"BoolRowVisualHandler could not find a Toggle named in the UXML template. Check your UXML.");
                return;
            }
        }

        protected Toggle _toggleField;

        protected override void ApplyVarValueToValueField()
        {
            _toggleField.SetValueWithoutNotify((bool)_currentVariable.BoxedValue);
            _toggleField.MarkDirtyRepaint();
        }

        protected override void ToggleValueChangeSubs(bool on)
        {
            base.ToggleValueChangeSubs(on);
            if (_toggleField == null)
            {
                return;
            }

            if (on)
            {
                _toggleField.RegisterValueChangedCallback(OnToggleFieldChanged);
            }
            else
            {
                _toggleField.UnregisterValueChangedCallback(OnToggleFieldChanged);
            }
        }

        private void OnToggleFieldChanged(ChangeEvent<bool> evt)
        {
            TriggerValueFieldChanged(evt.newValue);
        }
    }

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(Vector2), 
        typeDisplayName: "VectorTwo",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/VectorTwoVariableRow")]
    public class VectorTwoRowVisualHandler : RowVisualHandler<Vector2>
    {
        protected override void RegisterVisualElements()
        {
            base.RegisterVisualElements();
            _vector2Field = ValueField as Vector2Field;
            if (_vector2Field == null)
            {
                Debug.LogError($"VectorTwoRowVisualHandler could not find a Vector2Field named in the UXML template. Check your UXML.");
                return;
            }
        }

        protected Vector2Field _vector2Field;

        protected override void ApplyVarValueToValueField()
        {
            _vector2Field.SetValueWithoutNotify((Vector2)_currentVariable.BoxedValue);
            _vector2Field.MarkDirtyRepaint();
        }

        protected override void ToggleValueChangeSubs(bool on)
        {
            base.ToggleValueChangeSubs(on);
            if (_vector2Field == null)
            {
                return;
            }

            if (on)
            {
                _vector2Field.RegisterValueChangedCallback(OnVector2FieldChanged);
            }
            else
            {
                _vector2Field.UnregisterValueChangedCallback(OnVector2FieldChanged);
            }
        }

        private void OnVector2FieldChanged(ChangeEvent<Vector2> evt)
        {
            TriggerValueFieldChanged(evt.newValue);
        }
    }

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(Vector3), 
        typeDisplayName: "VectorThree",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/VectorThreeVariableRow")]
    public class VectorThreeRowVisualHandler : RowVisualHandler<Vector3>
    {
        protected override void RegisterVisualElements()
        {
            base.RegisterVisualElements();
            _vector3Field = ValueField as Vector3Field;
            if (_vector3Field == null)
            {
                Debug.LogError($"VectorThreeRowVisualHandler could not find a Vector3Field named in the UXML template. Check your UXML.");
                return;
            }
        }

        protected Vector3Field _vector3Field;

        protected override void ApplyVarValueToValueField()
        {
            _vector3Field.SetValueWithoutNotify((Vector3)_currentVariable.BoxedValue);
            _vector3Field.MarkDirtyRepaint();
        }

        protected override void ToggleValueChangeSubs(bool on)
        {
            base.ToggleValueChangeSubs(on);
            if (_vector3Field == null)
            {
                return;
            }
            if (on)
            {
                _vector3Field.RegisterValueChangedCallback(OnVector3FieldChanged);
            }
            else
            {
                _vector3Field.UnregisterValueChangedCallback(OnVector3FieldChanged);
            }
        }

        private void OnVector3FieldChanged(ChangeEvent<Vector3> evt)
        {
            TriggerValueFieldChanged(evt.newValue);
        }
    }


    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(Vector4),
        typeDisplayName: "VectorFour",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/VectorFourVariableRow")]
    public class VectorFourVisualHandler : RowVisualHandler<Vector4>//
    {
        protected override void RegisterVisualElements()
        {
            base.RegisterVisualElements();
            _vector4Field = ValueField as Vector4Field;
            if (_vector4Field == null)
            {
                Debug.LogError($"VectorThreeRowVisualHandler could not find a Vector4Field named in the UXML template. Check your UXML.");
                return;
            }
        }

        protected Vector4Field _vector4Field;

        protected override void ApplyVarValueToValueField()
        {
            _vector4Field.SetValueWithoutNotify((Vector4)_currentVariable.BoxedValue);
            _vector4Field.MarkDirtyRepaint();
        }

        protected override void ToggleValueChangeSubs(bool on)
        {
            base.ToggleValueChangeSubs(on);
            if (_vector4Field == null)
            {
                return;
            }
            if (on)
            {
                _vector4Field.RegisterValueChangedCallback(OnVector4FieldChanged);
            }
            else
            {
                _vector4Field.UnregisterValueChangedCallback(OnVector4FieldChanged);
            }
        }

        private void OnVector4FieldChanged(ChangeEvent<Vector4> evt)
        {
            TriggerValueFieldChanged(evt.newValue);
        }
    }

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(Matrix4x4),
        typeDisplayName: "MatrixFourByFour",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/MatrixFourByFourVariableRow")]
    public class MatrixFourByFourVisualHandler : RowVisualHandler<Matrix4x4>
    {
        protected override void RegisterVisualElements()
        {
            base.RegisterVisualElements();
            _fieldController.Init(this.RowRoot);
            if (!_fieldController.IsValid)
            {
                Debug.LogError($"MatrixFourByFourRowVisualHandler could not find a Matrix4x4Field " +
                    $"named in the UXML template. Check your UXML.");
                return;
            }
        }

        private MatrixFourByFourFieldController _fieldController = new MatrixFourByFourFieldController();

        protected override void ApplyVarValueToValueField()
        {
            _fieldController.SetValueWithoutNotify((Matrix4x4)_currentVariable.BoxedValue);
            _fieldController.MarkDirtyRepaint();
        }

        protected override void ToggleValueChangeSubs(bool on)
        {
            base.ToggleValueChangeSubs(on);
            if (_fieldController == null)
            {
                return;
            }
            if (on)
            {
                _fieldController.ValueChanged += OnFieldChanged;
            }
            else
            {
                _fieldController.ValueChanged -= OnFieldChanged;
            }
        }

        private void OnFieldChanged(Matrix4x4 prev, Matrix4x4 current)
        {
            TriggerValueFieldChanged(current);
        }
    }
}