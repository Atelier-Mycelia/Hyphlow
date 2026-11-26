using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// A small square showing a Texture2D property; clicking it opens Unity's object
    /// picker. Edits go straight into the tracked property, so it should belong to a
    /// working copy rather than to a real asset.
    /// </summary>
    internal sealed class IconSquare : VisualElement
    {
        /// <summary>
        /// Raised after the property was changed by the user.
        /// </summary>
        public event Action Edited = delegate { };

        public IconSquare(string tooltipText)
        {
            AddToClassList("icon-square");
            tooltip = tooltipText;

            _preview = new VisualElement { pickingMode = PickingMode.Ignore };
            _preview.AddToClassList("icon-square__preview");
            Add(_preview);

            _emptyMark = new UitkLabel("+") { pickingMode = PickingMode.Ignore };
            _emptyMark.AddToClassList("icon-square__empty-mark");
            Add(_emptyMark);

            RegisterCallback<ClickEvent>(OnClicked);
            this.AddManipulator(new ContextualMenuManipulator(OnContextMenu));
        }

        private readonly VisualElement _preview;
        private readonly UitkLabel _emptyMark;
        private SerializedProperty _property;

        public void Track(SerializedProperty property)
        {
            StopPicking();
            _property = property;
            Refresh();
        }

        public void Release()
        {
            StopPicking();
            _property = null;
            Refresh();
        }

        private void Refresh()
        {
            var texture = _property?.objectReferenceValue as Texture2D;
            _preview.style.backgroundImage = texture != null ?
                new StyleBackground(texture) :
                new StyleBackground(StyleKeyword.None);
            _emptyMark.style.display = texture != null ? DisplayStyle.None : DisplayStyle.Flex;
            EnableInClassList("icon-square--empty", texture == null);
            SetEnabled(_property != null);
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            bool hasTexture = _property?.objectReferenceValue != null;
            evt.menu.AppendAction("Clear", _ => Assign(null),
                hasTexture ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        #region Picking
        // The picker reports back through IMGUI commands, which UI Toolkit doesn't route
        // to elements, so the picker is polled for as long as it's ours.
        private const long PollIntervalMs = 30;

        private void OnClicked(ClickEvent evt)
        {
            if (_property == null || evt.button != 0)
            {
                return;
            }

            _pickerId = GetHashCode();
            EditorGUIUtility.ShowObjectPicker<Texture2D>(_property.objectReferenceValue, false, "",
                _pickerId);
            _poll?.Pause();
            _poll = schedule.Execute(PollPicker).Every(PollIntervalMs);
        }

        private int _pickerId;
        private IVisualElementScheduledItem _poll;

        private void PollPicker()
        {
            if (_property == null || EditorGUIUtility.GetObjectPickerControlID() != _pickerId)
            {
                StopPicking(); // Closed, or another picker took over
                return;
            }

            var picked = EditorGUIUtility.GetObjectPickerObject() as Texture2D;
            if (picked != _property.objectReferenceValue)
            {
                Assign(picked);
            }
        }

        private void StopPicking()
        {
            _poll?.Pause();
            _poll = null;
        }
        #endregion

        private void Assign(Texture2D texture)
        {
            _property.objectReferenceValue = texture;
            _property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Refresh();
            Edited();
        }
    }
}
