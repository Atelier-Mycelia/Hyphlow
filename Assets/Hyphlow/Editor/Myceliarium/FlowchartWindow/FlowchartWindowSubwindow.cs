using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public sealed class FlowchartWindowSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml =>
            "Editor/Uxml/Myceliarium/FlowchartWindowSubmenu";

        /// <summary>
        /// The fields are bound to the working copy, so they apply the user's edits
        /// themselves; this only says that the user may be done with one.
        /// </summary>
        internal event Action StateMayHaveChanged = delegate { };

        // The element name of each field, and the serialized path in FlowchartWindowConfig
        // of what it edits
        private static readonly (string elementName, string propertyPath)[] BoundFields =
        {
            ("WindowTitle", "_flowchartWindowTitle"),
            ("WindowMinSize", "_windowMinSize"),
            ("MinZoom", "_minZoom"),
            ("MaxZoom", "_maxZoom"),
            ("ZoomStep", "_zoomStep"),
            ("DefaultZoom", "_defaultZoom"),
            ("SnapBlocksToGrid", "_snapBlocksToGrid"),
            ("GridLineSpacing", "_gridDrawConfig._gridLineSpacingSize"),
            ("GridLineColor", "_gridDrawConfig._gridLineColor"),
            ("BlockUxml", "_blockUxml"),
            ("BlockStyleSheet", "_blockStyleSheet"),
            ("SelectedBlockStyleSheet", "_selectedBlockStyleSheet"),
        };

        protected override void RegisterVisualElements()
        {
            _controls = Root.Q<VisualElement>("Controls");
            foreach (var (elementName, propertyPath) in BoundFields)
            {
                var field = Root.Q<VisualElement>(elementName);
                if (field is IBindable)
                {
                    _fields.Add((field, propertyPath));
                }
            }
        }

        private VisualElement _controls;
        private readonly List<(VisualElement field, string propertyPath)> _fields =
            new List<(VisualElement, string)>();

        protected override void ConfigureVisualElements()
        {
            // Bound object fields only accept the type they're told about
            Root.Q<ObjectField>("BlockUxml").objectType = typeof(VisualTreeAsset);
            Root.Q<ObjectField>("BlockStyleSheet").objectType = typeof(StyleSheet);
            Root.Q<ObjectField>("SelectedBlockStyleSheet").objectType = typeof(StyleSheet);

            // Disabling the container also disables the fields inside it
            _controls.SetEnabled(HyphlowRepoGuard.IsHyphlowRepo());
        }

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            _interactionWatcher?.Dispose();
            _interactionWatcher = null;
            if (wantsSubsActive)
            {
                _interactionWatcher = new InteractionWatcher(Root, RaiseStateMayHaveChanged);
            }
        }

        private InteractionWatcher _interactionWatcher;

        private void RaiseStateMayHaveChanged()
        {
            StateMayHaveChanged();
        }

        /// <summary>
        /// Shows the working copy the user is editing. Binding it is what makes the
        /// fields write to it.
        /// </summary>
        internal void ShowState(FlowchartWindowConfig workingState)
        {
            Unbind();
            _workingState = workingState;
            Bind();
        }

        private FlowchartWindowConfig _workingState;

        public override void Bind()
        {
            if (_workingState == null)
            {
                return;
            }

            var so = new SerializedObject(_workingState);
            foreach (var (field, propertyPath) in _fields)
            {
                ((IBindable)field).BindProperty(so.FindProperty(propertyPath));
            }
        }

        public override void Unbind()
        {
            foreach (var (field, _) in _fields)
            {
                field.Unbind();
            }
        }

        public override void Dispose()
        {
            _interactionWatcher?.Dispose();
            _interactionWatcher = null;
            base.Dispose();
        }
    }
}
