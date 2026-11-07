using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Edits a Vector4 that the user can treat as a Vector2, Vector3 or Vector4. A
    /// dropdown picks how many components are shown. Picking fewer components zeroes the
    /// hidden ones, so the value always matches what's on screen.
    ///
    /// Layout (see VectorField.uxml): a non-empty label gets its own row on top; then a
    /// row of x, y, z, then a row with w and the dimension dropdown.
    /// </summary>
    public class VectorField : BaseField<Vector4>
    {
        public const string UxmlPath = "Editor/Uxml/CustomFields/VectorField";
        public const string UssPath = "Editor/Stylesheets/CustomFields/vectorField";

        /// <summary>
        /// Height of the two component rows, without any label row. Matches the USS.
        /// </summary>
        public const float ContentHeight = 58f;

        public VectorField() : this(null) { }

        public VectorField(string label) : this(label, new VisualElement()) { }

        // BaseField's own reference to the input element isn't accessible in 2022.3, so we
        // keep ours by creating it here and handing it to the base constructor.
        private VectorField(string label, VisualElement input) : base(label, input)
        {
            AddToClassList("vectorField");

            StyleSheet styleSheet = Resources.Load<StyleSheet>(UssPath);
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            VisualTreeAsset template = Resources.Load<VisualTreeAsset>(UxmlPath);
            if (template == null)
            {
                input.Add(new UnityEngine.UIElements.Label 
                { 
                    text = $"VectorField template not found at '{UxmlPath}'." }
                );
                return;
            }

            template.CloneTree(input);
            RegisterVisualElements(input, out bool success);
            if (!success)
            {
                return;
            }
            
            ConfigureVisualElements();
            SetSubs(true);
            ApplyDimension();
        }

        private void RegisterVisualElements(VisualElement input, out bool success)
        {
            success = false;

            _x = input.Q<FloatField>("XField");
            _y = input.Q<FloatField>("YField");
            _z = input.Q<FloatField>("ZField");
            _w = input.Q<FloatField>("WField");
            _dimensionField = input.Q<EnumField>("DimensionField");
            _numericFields = new FloatField[] { _x, _y, _z, _w };

            bool allFieldsReady = _x != null && _y != null && _z != null && 
            _w != null && _dimensionField != null;
            if (!allFieldsReady)
            {
                Debug.LogError($"{nameof(VectorField)}: the template needs FloatFields named " +
                    "XField, YField, ZField, WField and an EnumField named DimensionField.");
                return;
            }

            success = true;
        }

        private IList<FloatField> _numericFields;

        /// <summary>
        /// This is for stuff like setting isDelayed on all the numeric fields, which 
        /// is to cover for cases where the designer forgot to apply some setting
        /// in the uss or uxml. <br>
        /// This is NOT for setting up callbacks. Use SetSubs() for that.
        /// </summary>
        private void ConfigureVisualElements()
        {
            foreach (var field in _numericFields)
            {
                field.isDelayed = true;
            }

            _dimensionField.Init(_dimension);
        }

        protected virtual void SetSubs(bool wantsSubsActive)
        {
            foreach (var field in _numericFields)
            {
                field.isDelayed = true;
                if (wantsSubsActive)
                {
                    field.RegisterValueChangedCallback(OnComponentChanged);
                }
                else
                {
                    field.UnregisterValueChangedCallback(OnComponentChanged);
                }
            }

            _dimensionField.RegisterValueChangedCallback(OnDimensionChanged);
        }

        /// <summary>
        /// Raised only when the user picks a dimension in the dropdown, not when
        /// <see cref="Dimension"/> is set from code.
        /// </summary>
        public event System.Action<VectorDimension> UserChangedDimension;

        public VectorDimension Dimension
        {
            get => _dimension;
            set
            {
                if (_dimension == value)
                {
                    return;
                }

                _dimension = value;
                ApplyDimension();
            }
        }

        /// <summary>
        /// Never hides a non-zero component: if the new value needs more components than
        /// are showing, the dimension grows to fit. It never shrinks on its own.
        /// Unity auto-converts between Vector4s and Vector3s, so you can pass
        /// the latter into this func just fine.
        /// </summary>
        public override void SetValueWithoutNotify(Vector4 newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (_x == null)
            {
                return; // Still being constructed
            }

            _x.SetValueWithoutNotify(newValue.x);
            _y.SetValueWithoutNotify(newValue.y);
            _z.SetValueWithoutNotify(newValue.z);
            _w.SetValueWithoutNotify(newValue.w);

            VectorDimension needed = DimensionNeededFor(newValue);
            if (needed > _dimension)
            {
                Dimension = needed;
            }
        }

        // Deliberately not an overload of SetValueWithoutNotify: a Vector4 converts implicitly to
        // Vector2, and C# prefers a derived class's own applicable overloads over the Vector4
        // override, so callers (and this method itself) would land here and lose z and w.
        public virtual void SetVector2WithoutNotify(Vector2 newValue)
        {
            Vector4 toSet = new Vector4(newValue.x, newValue.y, 0f, 0f);
            SetValueWithoutNotify(toSet);
        }

        private VectorDimension _dimension = VectorDimension.FourD;
        private FloatField _x, _y, _z, _w;
        private EnumField _dimensionField;

        public static VectorDimension DimensionNeededFor(Vector4 vector)
        {
            return VectorMuscariable.DimensionNeededFor(vector);
        }

        private void ApplyDimension()
        {
            _dimensionField.SetValueWithoutNotify(_dimension);
            SetShown(_z, _dimension >= VectorDimension.ThreeD);
            SetShown(_w, _dimension >= VectorDimension.FourD);
        }

        private static void SetShown(VisualElement element, bool shown)
        {
            element.style.display = shown ? 
            DisplayStyle.Flex : 
            DisplayStyle.None;
        }

        private void OnComponentChanged(ChangeEvent<float> evt)
        {
            evt.StopPropagation(); // This field reports its own ChangeEvent<Vector4> instead
            value = new Vector4(_x.value, _y.value, _z.value, _w.value);
        }

        public Vector4 ToVector4() => new Vector4(_x.value, _y.value, _z.value, _w.value);
        public Vector3 ToVector3() => new Vector3(_x.value, _y.value, _z.value);
        public Vector2 ToVector2() => new Vector2(_x.value, _y.value);

        private void OnDimensionChanged(ChangeEvent<System.Enum> evt)
        {
            evt.StopPropagation();
            _dimension = (VectorDimension)evt.newValue;
            ApplyDimension();

            // Before the value is trimmed, so a listener that stores the dimension has already
            // trimmed the model by the time the value edit arrives
            UserChangedDimension?.Invoke(_dimension);

            Vector4 trimmed = value;
            if (_dimension < VectorDimension.FourD) 
            {
                trimmed.w = 0f;
            }
            if (_dimension < VectorDimension.ThreeD)
            {
                trimmed.z = 0f;
            }
            if (trimmed != value)
            {
                value = trimmed;
            }
        }
    }
}
