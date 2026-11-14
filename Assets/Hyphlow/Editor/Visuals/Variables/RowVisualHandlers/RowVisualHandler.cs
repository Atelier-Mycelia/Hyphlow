using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// What a bound row reports its edits to. Handlers call this directly instead of going
    /// through static signals, so several variable panels can be open without hearing each
    /// other's rows.
    /// </summary>
    public interface IRowEditHost
    {
        void KeyEdited(IVariable variable, string newKey);
        void ScopeEdited(IVariable variable, AccessScope newScope);
        void ValueEdited(IVariable variable, object newValue);
        void RemoveRequested(IVariable variable);

        /// <summary>
        /// For edits to a variable's own settings that aren't its value (like
        /// NumericMuscariable's whole-numbers toggle), so they get the same undo and dirty
        /// handling. Hosts that don't care can rely on this default, which just applies it.
        /// </summary>
        void PropertyEdited(IVariable variable, string description, Action<IVariable> applyChange)
        {
            applyChange?.Invoke(variable);
        }
    }

    public interface IRowVisualHandler : IDisposable
    {
        /// <summary>
        /// Null until the first Bind, since the visuals are built lazily.
        /// </summary>
        VisualElement RowRoot { get; }
        IVariable Variable { get; }
        Type VarContentType { get; }
        void Bind(IVariable variable, IRowEditHost host);
        void Unbind();
        void RefreshValue();
    }

    /// <summary>
    /// Owns the UI for one variable row. The visuals are built once from a UXML template and
    /// then reused across Bind/Unbind cycles, which is what makes pooling worthwhile.
    ///
    /// Template contract: elements named KeyInput (TextField), Scope (EnumField),
    /// RemoveButton (Button) and ValueField. The base class handles everything but the value
    /// part; custom types only override <see cref="BuildValueEditor"/> and <see cref="BindValue"/>.
    /// </summary>
    public abstract class RowVisualHandler : IRowVisualHandler
    {
        public abstract Type VarContentType { get; }
        public IVariable Variable { get; private set; }
        public VisualElement RowRoot { get; private set; }

        public virtual void Bind(IVariable variable, IRowEditHost host)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            EnsureBuilt();
            if (Variable != null)
            {
                Unbind();
            }

            Variable = variable;
            _host = host;
            RowRoot.name = $"{variable.ContentType.Name}_Row";

            _keyField?.SetValueWithoutNotify(variable.Key);
            _scopeField?.SetValueWithoutNotify(variable.Scope);
            // Variables owned by ScriptableObjects (VariableSets) are global-only
            _scopeField?.SetEnabled(variable.Owner is not ScriptableObject);

            if (variable is Muscariable muscariable)
            {
                muscariable.OnValueChanged += OnModelValueChanged;
            }

            RefreshValue();
        }

        public virtual void Unbind()
        {
            if (Variable is Muscariable muscariable)
            {
                muscariable.OnValueChanged -= OnModelValueChanged;
            }

            Variable = null;
            _host = null;
        }

        /// <summary>
        /// Pushes the variable's current value into the value field without raising edit events.
        /// </summary>
        public void RefreshValue()
        {
            if (Variable == null || !_canShowValue)
            {
                return;
            }

            _isApplyingModel = true;
            try
            {
                BindValue(Variable);
            }
            finally
            {
                _isApplyingModel = false;
            }
        }

        /// <summary>
        /// Resolves the row's UXML. Override for code-only rows, or use
        /// <see cref="RowTemplates.Override"/> to restyle a built-in row without subclassing.
        /// </summary>
        protected virtual VisualTreeAsset LoadTemplate()
        {
            return RowTemplates.Resolve(GetType());
        }

        /// <summary>
        /// Called once, when the visuals are built. Wire up the value field's change
        /// events here and call <see cref="ReportValueEdited"/> when the user edits it.
        /// </summary>
        protected abstract void BuildValueEditor(VisualElement valueField);

        /// <summary>
        /// Called on every bind and whenever the model's value changes. Must not
        /// raise edit events (use SetValueWithoutNotify).
        /// </summary>
        protected abstract void BindValue(IVariable variable);

        /// <summary>
        /// False for rows that only show a key and scope, such as the generic fallback.
        /// </summary>
        protected virtual bool RequiresValueField => true;

        protected void ReportValueEdited(object newValue)
        {
            if (_isApplyingModel || Variable == null)
            {
                return;
            }

            _host?.ValueEdited(Variable, newValue);
        }

        protected void ReportPropertyEdited(string description, Action<IVariable> applyChange)
        {
            if (_isApplyingModel || Variable == null)
            {
                return;
            }

            _host?.PropertyEdited(Variable, description, applyChange);
        }

        private IRowEditHost _host;
        private bool _isApplyingModel;
        private bool _canShowValue;
        private TextField _keyField;
        private EnumField _scopeField;

        private void EnsureBuilt()
        {
            if (RowRoot != null)
            {
                return;
            }

            VisualTreeAsset template = LoadTemplate();
            if (template == null)
            {
                RowRoot = new UnityEngine.UIElements.Label($"No row template for {GetType().Name}");
                return;
            }

            RowRoot = template.CloneTree();
            WireStandardFields();
            BuildValueEditorIfPresent();
        }

        private void WireStandardFields()
        {
            _keyField = RowRoot.Q<TextField>("KeyInput");
            if (_keyField == null)
            {
                Debug.LogWarning($"{GetType().Name}: row template is missing a TextField named 'KeyInput'.");
            }
            else
            {
                _keyField.isDelayed = true;
                _keyField.multiline = false;
                _keyField.RegisterValueChangedCallback(evt =>
                {
                    if (Variable != null) _host?.KeyEdited(Variable, evt.newValue);
                });
            }

            _scopeField = RowRoot.Q<EnumField>("Scope");
            if (_scopeField == null)
            {
                Debug.LogWarning($"{GetType().Name}: row template is missing an EnumField named 'Scope'.");
            }
            else
            {
                _scopeField.Init(AccessScope.Private);
                // Registered after Init so that it can't be mistaken for a user edit
                _scopeField.RegisterValueChangedCallback(evt =>
                {
                    if (Variable != null) _host?.ScopeEdited(Variable, (AccessScope)evt.newValue);
                });
            }

            Button removeButton = RowRoot.Q<Button>("RemoveButton");
            if (removeButton == null)
            {
                Debug.LogWarning($"{GetType().Name}: row template is missing a Button named 'RemoveButton'.");
            }
            else
            {
                removeButton.clicked += () =>
                {
                    if (Variable != null) _host?.RemoveRequested(Variable);
                };
            }
        }

        private void BuildValueEditorIfPresent()
        {
            VisualElement valueField = RowRoot.Q("ValueField");
            if (valueField == null)
            {
                if (RequiresValueField)
                {
                    Debug.LogWarning($"{GetType().Name}: row template is missing an element named 'ValueField'.");
                }
                return;
            }

            BuildValueEditor(valueField);
            _canShowValue = true;
        }

        private void OnModelValueChanged(Muscariable changed)
        {
            RefreshValue();
        }

        public virtual void Dispose()
        {
            Unbind();
            RowRoot?.RemoveFromHierarchy();
            RowRoot = null;
            _keyField = null;
            _scopeField = null;
            _canShowValue = false;
        }
    }

    /// <summary>
    /// Handles the common case out of the box: the template's ValueField is either an
    /// INotifyValueChanged&lt;T&gt; (float, int, string, bool, Color, vectors...) or an
    /// ObjectField for UnityEngine.Object types. Most built-in handlers are just an
    /// attribute on a subclass with an empty body.
    /// </summary>
    public abstract class RowVisualHandler<TVarContentType> : RowVisualHandler
    {
        public override Type VarContentType => _varContentType;
        protected static readonly Type _varContentType = typeof(TVarContentType);

        protected override void BuildValueEditor(VisualElement valueField)
        {
            bool isObjectType = typeof(UnityObj).IsAssignableFrom(_varContentType);
            if (isObjectType && valueField is ObjectField objectField)
            {
                objectField.objectType = _varContentType;
                objectField.RegisterValueChangedCallback(evt => ReportValueEdited(evt.newValue));
                _applyToField = boxed => objectField.SetValueWithoutNotify(boxed as UnityObj);
                return;
            }

            if (valueField is INotifyValueChanged<TVarContentType> typedField)
            {
                if (valueField is TextInputBaseField<TVarContentType> textField)
                {
                    textField.isDelayed = true; // So changes only fire on enter or focus lost
                }

                typedField.RegisterValueChangedCallback(evt => ReportValueEdited(evt.newValue));
                _applyToField = boxed => typedField.SetValueWithoutNotify(
                    boxed is TVarContentType value ? value : default);
                return;
            }

            Debug.LogError($"{GetType().Name} could not use the element named 'ValueField' in its " +
                $"UXML template as an editor for {_varContentType.Name}. Check your UXML.");
        }

        protected override void BindValue(IVariable variable)
        {
            _applyToField?.Invoke(variable.BoxedValue);
        }

        /// <summary>
        /// Set by BuildValueEditor; subclasses that build their own editor can assign it too.
        /// </summary>
        protected Action<object> _applyToField;

        public override void Dispose()
        {
            base.Dispose();
            _applyToField = null;
        }
    }

    [RowVisualHandler("Hidden", typeof(object), "Generic",
        "Editor/Uxml/VarRows/_VariableRowTemplate")]
    public class DefaultRowVisualHandler : RowVisualHandler<object>
    {
        protected override bool RequiresValueField => false;
    }
}
