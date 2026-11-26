using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public sealed class EditorSysAssetsSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml =>
            "Editor/Uxml/Myceliarium/EditorSysAssetsSubmenu";

        /// <summary>
        /// The fields are bound to the working copy, so they apply the user's edits
        /// themselves; this only says that the user may be done with one.
        /// </summary>
        internal event Action StateMayHaveChanged = delegate { };

        // Serialized field names in HyphlowEditorSysAssets, not display names
        private static readonly (string label, string property)[] IconFields =
        {
            ("Hyphlow Logo", "_hyphlow_logo"),
            ("Mushroom Icon", "_mushroomIcon"),
            ("Add", "_add"),
            ("Add (Small)", "_add_small"),
            ("Delete", "_delete"),
            ("Duplicate", "_duplicate"),
            ("Up", "_up"),
            ("Down", "_down"),
            ("Play (Big)", "_play_big"),
            ("Play (Small)", "_play_small"),
            ("Command Background", "_command_background"),
        };

        private const string FreeSubproperty = "_free";
        private const string ProSubproperty = "_pro";

        protected override void RegisterVisualElements()
        {
            _iconsContainer = Root.Q<VisualElement>("IconsContainer");
            _fcwConfigField = Root.Q<ObjectField>("FcwConfig");
        }

        private VisualElement _iconsContainer;
        private ObjectField _fcwConfigField;

        protected override void ConfigureVisualElements()
        {
            _fcwConfigField.objectType = typeof(FlowchartWindowConfig);
            BuildIconRows();

            // Disabling the containers also disables the squares and field inside them
            bool editable = HyphlowRepoGuard.IsHyphlowRepo();
            _iconsContainer.SetEnabled(editable);
            _fcwConfigField.SetEnabled(editable);
        }

        private void BuildIconRows()
        {
            _iconSquares.Clear();
            _iconsContainer.Clear();

            foreach (var (label, property) in IconFields)
            {
                var row = new VisualElement();
                row.AddToClassList("icon-row");

                var rowLabel = new UitkLabel(label);
                rowLabel.AddToClassList("icon-row__label");
                row.Add(rowLabel);

                row.Add(MakeSlot("Free skin", $"{property}.{FreeSubproperty}"));
                row.Add(MakeSlot("Pro (dark) skin", $"{property}.{ProSubproperty}"));
                _iconsContainer.Add(row);
            }
        }

        private VisualElement MakeSlot(string caption, string propertyPath)
        {
            var slot = new VisualElement();
            slot.AddToClassList("icon-slot");

            var square = new IconSquare(caption);
            _iconSquares.Add((square, propertyPath));
            slot.Add(square);

            var slotLabel = new UitkLabel(caption);
            slotLabel.AddToClassList("icon-slot__label");
            slot.Add(slotLabel);
            return slot;
        }

        private readonly List<(IconSquare square, string propertyPath)> _iconSquares =
            new List<(IconSquare, string)>();

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            _interactionWatcher?.Dispose();
            _interactionWatcher = null;

            foreach (var (square, _) in _iconSquares)
            {
                square.Edited -= RaiseStateMayHaveChanged;
            }

            if (wantsSubsActive)
            {
                _interactionWatcher = new InteractionWatcher(Root, RaiseStateMayHaveChanged);
                foreach (var (square, _) in _iconSquares)
                {
                    square.Edited += RaiseStateMayHaveChanged;
                }
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
        internal void ShowState(HyphlowEditorSysAssets workingState)
        {
            Unbind();
            _workingState = workingState;
            Bind();
        }

        private HyphlowEditorSysAssets _workingState;

        public override void Bind()
        {
            if (_workingState == null)
            {
                return;
            }

            var so = new SerializedObject(_workingState);
            foreach (var (square, propertyPath) in _iconSquares)
            {
                square.Track(so.FindProperty(propertyPath));
            }

            _fcwConfigField.BindProperty(so.FindProperty("_fcwConfig"));
        }

        public override void Unbind()
        {
            foreach (var (square, _) in _iconSquares)
            {
                square.Release();
            }

            _fcwConfigField.Unbind();
        }

        public override void Dispose()
        {
            _interactionWatcher?.Dispose();
            _interactionWatcher = null;
            base.Dispose();
        }
    }
}
