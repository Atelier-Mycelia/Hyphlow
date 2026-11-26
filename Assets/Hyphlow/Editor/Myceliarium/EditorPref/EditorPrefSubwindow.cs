using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public sealed class EditorPrefSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml =>
            "Editor/Uxml/Myceliarium/EditorPrefSubmenu";

        /// <summary>
        /// The fields are bound to the working copy, so they apply the user's edits
        /// themselves; this only says that the user may be done with one.
        /// </summary>
        internal event Action StateMayHaveChanged = delegate { };

        // The element name of each field, and the serialized field in EditorPrefData of
        // what it edits
        private static readonly (string elementName, string propertyPath)[] BoundFields =
        {
            ("HideMushroom", "_hideMushroomInHierarchy"),
            ("UseLegacyMenus", "_useLegacyMenus"),
            ("UseGridSnap", "_useGridSnap"),
            ("FlowchartBlockTint", "_flowchartBlockTint"),
            ("CommandListTint", "_commandListTint"),
            ("SuppressHelpBoxes", "_suppressHelpBoxes"),
            ("NavigateWithArrows", "_navigateCmdListWithArrows"),
        };

        protected override void RegisterVisualElements()
        {
            foreach (var (elementName, propertyPath) in BoundFields)
            {
                var field = Root.Q<VisualElement>(elementName);
                if (field is IBindable)
                {
                    _fields.Add((field, propertyPath));
                }
            }
        }

        private readonly List<(VisualElement field, string propertyPath)> _fields =
            new List<(VisualElement, string)>();

        protected override void ConfigureVisualElements()
        {
            // No need for further config
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
        internal void ShowState(EditorPrefData workingState)
        {
            Unbind();
            _workingState = workingState;
            Bind();
        }

        private EditorPrefData _workingState;

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
