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
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/NumericVariableRow")]
    public class NumericRowVisualHandler : RowVisualHandler<double>
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

    [RowVisualHandler(menuName: "Numeric",
        contentType: typeof(Vector4),
        typeDisplayName: "VectorFour",
        pathToTemplate: "Editor/Uxml/VarRows/Numeric/VectorFourVariableRow")]
    public class VectorFourVisualHandler : RowVisualHandler<Vector4>
    {
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
