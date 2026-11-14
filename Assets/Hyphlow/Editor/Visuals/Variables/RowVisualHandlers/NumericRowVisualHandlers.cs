using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    // The base class wires up whichever INotifyValueChanged<T> the template's ValueField is,
    // so these only need to say which template to use.
    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(float),
        typeDisplayName: "Float",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/FloatVariableRow")]
    public class FloatRowVisualHandler : RowVisualHandler<float>
    {

    }

    /// <summary>
    /// Takes over the double content type from the old Double row (the registry keeps one
    /// handler per content type). Adds a toggle for NumericMuscariable's whole-numbers setting.
    /// </summary>
    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(double),
        typeDisplayName: "Number",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/NumberVariableRow")]
    public class NumberRowVisualHandler : RowVisualHandler<double>
    {
        protected override void BuildValueEditor(VisualElement valueField)
        {
            base.BuildValueEditor(valueField);

            _wholeNumsToggle = RowRoot.Q<Toggle>("WholeNumsOnly");
            if (_wholeNumsToggle == null)
            {
                Debug.LogWarning($"{GetType().Name}: row template is missing a Toggle named 'WholeNumsOnly'.");
                return;
            }

            _wholeNumsToggle.RegisterValueChangedCallback(evt =>
                ReportPropertyEdited("Whole Nums Only", edited =>
                {
                    if (edited is NumberMuscariable numeric)
                    {
                        numeric.WholeNumsOnly = evt.newValue;
                    }
                }));
        }

        protected override void BindValue(IVariable variable)
        {
            base.BindValue(variable);

            if (_wholeNumsToggle == null)
            {
                return;
            }

            // Variables saved before NumericMuscariable existed can still be DoubleMuscariables
            bool isNumeric = variable is NumberMuscariable;
            _wholeNumsToggle.SetEnabled(isNumeric);
            _wholeNumsToggle.SetValueWithoutNotify(isNumeric && ((NumberMuscariable)variable).WholeNumsOnly);
        }

        public override void Dispose()
        {
            base.Dispose();
            _wholeNumsToggle = null;
        }

        private Toggle _wholeNumsToggle;
    }

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(int),
        typeDisplayName: "Integer",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/IntVariableRow")]
    public class IntRowVisualHandler : RowVisualHandler<int>
    {

    }

    // Bools work off toggles, not text value fields; same deal.
    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(bool),
        typeDisplayName: "Boolean",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/BoolVariableRow")]
    public class BoolRowVisualHandler : RowVisualHandler<bool>
    {
    }

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(Vector2),
        typeDisplayName: "VectorTwo",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/VectorTwoVariableRow")]
    public class VectorTwoRowVisualHandler : RowVisualHandler<Vector2>
    {
    }

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(Vector3),
        typeDisplayName: "VectorThree",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/VectorThreeVariableRow")]
    public class VectorThreeRowVisualHandler : RowVisualHandler<Vector3>
    {
    }

    /// <summary>
    /// Takes over the Vector4 content type from the old VectorFour row (the registry keeps one
    /// handler per content type). The template's ValueField is an empty holder; the
    /// <see cref="VectorField"/> goes in it from code, since it isn't a UXML-instantiable element.
    /// </summary>
    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(Vector4),
        typeDisplayName: "Vector",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/VectorVariableRow")]
    public class VectorRowVisualHandler : RowVisualHandler<Vector4>
    {
        protected override void BuildValueEditor(VisualElement valueField)
        {
            _vectorField = new VectorField();
            _vectorField.AddToClassList("vectorVarField");
            valueField.Add(_vectorField);

            _vectorField.RegisterValueChangedCallback(evt => ReportValueEdited(evt.newValue));
            _vectorField.UserChangedDimension += dimension =>
                ReportPropertyEdited("Vector Dimension", edited =>
                {
                    if (edited is VectorMuscariable vectorVar)
                    {
                        vectorVar.Dimension = dimension;
                    }
                });
        }

        protected override void BindValue(IVariable variable)
        {
            Vector4 value = variable.BoxedValue is Vector4 vector ? vector : default;

            // The field's dimension is view state that outlives a bind (rows are pooled), so it
            // has to be set every time. Variables saved before VectorMuscariable existed don't
            // store one, so for those we show the fewest components that fit the value.
            _vectorField.Dimension = variable is VectorMuscariable vectorVar ?
                vectorVar.Dimension :
                VectorMuscariable.DimensionNeededFor(value);

            _vectorField.SetValueWithoutNotify(value);
        }

        public override void Dispose()
        {
            base.Dispose();
            _vectorField = null;
        }

        private VectorField _vectorField;
    }

    /// <summary>
    /// Matrices are a grid of FloatFields, which the field controller presents as one value.
    /// </summary>
    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(Matrix4x4),
        typeDisplayName: "MatrixFourByFour",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/MatrixFourByFourVariableRow")]
    public class MatrixFourByFourVisualHandler : RowVisualHandler<Matrix4x4>
    {
        protected override void BuildValueEditor(VisualElement valueField)
        {
            _fieldController.Init(RowRoot);
            if (!_fieldController.IsValid)
            {
                Debug.LogError($"MatrixFourByFourRowVisualHandler could not find a Matrix4x4Field " +
                    $"named in the UXML template. Check your UXML.");
                return;
            }

            _fieldController.ValueChanged += (_, current) => ReportValueEdited(current);
            _applyToField = boxed => _fieldController.SetValueWithoutNotify(
                boxed is Matrix4x4 matrix ? matrix : Matrix4x4.identity);
        }

        private readonly MatrixFourByFourFieldController _fieldController = new MatrixFourByFourFieldController();
    }
}
