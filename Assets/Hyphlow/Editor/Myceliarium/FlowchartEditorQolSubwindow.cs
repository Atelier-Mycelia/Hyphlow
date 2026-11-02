using AtMycelia.Myceliarium;
using AtMycelia.Hyphlow.EditorExt;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Subwindow for managing Flowchart Editor QoL assets.
    /// Focuses on UI element setup and callback wiring.
    /// </summary>
    public sealed class FlowchartEditorQolSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml =>
            "Editor/Uxml/Myceliarium/FlowchartEditorQolSubmenu";

        private VisualTreeAsset _itemTemplate;
        private static readonly string _itemTemplatePath =
            "Editor/Uxml/Myceliarium/FlowchartEditorQolItem";

        // The logic's working-state list, handed over by the entry
        private List<FlowchartEditorQol> _workingBuffer = new List<FlowchartEditorQol>();

        internal Action<string> CreateRequested { get; set; } = delegate { };
        internal Action<int, string> RenameRequested { get; set; } = delegate { };
        internal Action<int> DeleteRequested { get; set; } = delegate { };
        internal Action<int> SelectRequested { get; set; } = delegate { };

        /// <summary>
        /// The properties are bound to the working copies, so the fields apply the user's
        /// edits themselves; this only says that the user may be done with one.
        /// </summary>
        internal event Action StateMayHaveChanged = delegate { };

        /// <summary>
        /// The list stays the same instance for as long as its logic lives, so this is
        /// called just once per initialization.
        /// </summary>
        internal void UseWorkingList(List<FlowchartEditorQol> workingList)
        {
            _workingBuffer = workingList ?? new List<FlowchartEditorQol>();
            _listView.itemsSource = _workingBuffer;
        }

        protected override void RegisterVisualElements()
        {
            _newAssetNameField = Root.Q<TextField>("NewAssetNameField");
            _createButton = Root.Q<Button>("CreateButton");
            _listView = Root.Q<ListView>("QolAssetsListView");

            LoadItemTemplate();
            InitListView();
        }

        private TextField _newAssetNameField;
        private Button _createButton;
        private ListView _listView;

		protected override void ConfigureVisualElements()
		{
			// None needed atm
		}

        private void LoadItemTemplate()
        {
            _itemTemplate = Resources.Load<VisualTreeAsset>(_itemTemplatePath);

            if (_itemTemplate == null)
            {
                Debug.LogError($"Failed to load QoL item template at {_itemTemplatePath}");
            }
        }

        private void InitListView()
        {
            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;
            _listView.itemsSource = _workingBuffer;
            _listView.selectionType = SelectionType.Single;
            _listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        }

        private VisualElement MakeItem()
        {
            QolAssetRow row = CreateAssetRow();
            WireUpAssetRowCallbacks(row);
            return row.Root;
        }

        private QolAssetRow CreateAssetRow()
        {
            VisualElement root = _itemTemplate.CloneTree();

            Foldout foldout = root.Q<Foldout>("QolItemFoldout");
            VisualElement props = root.Q<VisualElement>("PropertiesContainer");
            TextField nameField = root.Q<TextField>("NameField");
            PropertyField commandsList = root.Q<PropertyField>("CommandsToHideField");
            Button selectButton = root.Q<Button>("SelectButton");
            Button deleteButton = root.Q<Button>("DeleteButton");

            QolAssetRow row = new QolAssetRow(foldout, props, nameField, commandsList,
                selectButton, deleteButton);
            root.userData = row;
            return row;
        }

        private void WireUpAssetRowCallbacks(QolAssetRow row)
        {
            // The stuff we can't put in SetSubs due to needing the row
            // reference for the callbacks.
            var nameField = row.NameField;

            nameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                bool doneEditing = evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter;
                if (doneEditing)
                {
                    RaiseRenameRequested(row);
                    evt.StopPropagation();
                }
            });

            nameField.RegisterCallback<FocusOutEvent>(_ => RaiseRenameRequested(row));
            row.DeleteButton.clicked += () => RaiseDeleteRequested(row);
            row.SelectButton.clicked += () => RaiseSelectRequested(row);
        }

        private void BindItem(VisualElement element, int index)
        {
            if (element.userData is not QolAssetRow row)
            {
                return;
            }

            bool validIndex = index >= 0 && index < _workingBuffer.Count;
            if (!validIndex)
            {
                return;
            }

            FlowchartEditorQol wState = _workingBuffer[index];

            row.Index = index;
            row.Foldout.text = wState.name;
            row.Asset = wState;
        }

        /// <summary>
        /// Called by the entry whenever working-state is refreshed.
        /// </summary>
        public override void Refresh()
        {
            _listView.RefreshItems();
        }

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            _interactionWatcher?.Dispose();
            _interactionWatcher = null;

            if (wantsSubsActive)
            {
                _createButton.clicked += RaiseCreateRequested;
                _interactionWatcher = new InteractionWatcher(Root, RaiseStateMayHaveChanged);
            }
            else
            {
                _createButton.clicked -= RaiseCreateRequested;
            }
        }

        private InteractionWatcher _interactionWatcher;

        private void RaiseStateMayHaveChanged()
        {
            StateMayHaveChanged();
        }

        private void RaiseCreateRequested()
        {
            string name = _newAssetNameField.value?.Trim();
            CreateRequested?.Invoke(name);
        }

        private void RaiseRenameRequested(QolAssetRow row)
        {
            if (!IsValidIndex(row.Index))
            {
                return;
            }

            string newName = row.NameField.value?.Trim() ?? "";
            RenameRequested?.Invoke(row.Index, newName);
        }

        private void RaiseDeleteRequested(QolAssetRow row)
        {
            if (!IsValidIndex(row.Index))
            {
                return;
            }

            if (!row.Asset.IsDeletable)
            {
                string logMessage = $"Attempted to delete non-deletable asset: {row.Asset.name}";
                Debug.LogWarning(logMessage);
                return;
            }

            DeleteRequested?.Invoke(row.Index);
        }

        private void RaiseSelectRequested(QolAssetRow row)
        {
            if (!IsValidIndex(row.Index))
            {
                return;
            }

            SelectRequested?.Invoke(row.Index);
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < _workingBuffer.Count;
        }

        public override void Dispose()
        {
            SetSubs(false);
            base.Dispose();
        }

        internal sealed class QolAssetRow
        {
            public QolAssetRow(Foldout foldout, VisualElement props,
                TextField nameField, PropertyField commandsList,
                Button selectButton, Button deleteButton)
            {
                Foldout = foldout;
                PropertiesContainer = props;
                NameField = nameField;
                CommandsListView = commandsList;
                SelectButton = selectButton;
                DeleteButton = deleteButton;
            }

            public VisualElement Root => Foldout.parent;

            public Foldout Foldout { get; }
            public VisualElement PropertiesContainer { get; }
            public TextField NameField { get; }
            public PropertyField CommandsListView { get; }
            public Button SelectButton { get; }
            public Button DeleteButton { get; }
            public int Index { get; set; }

            public FlowchartEditorQol Asset
            {
                get => _asset;
                set
                {
                    _asset = value;
                    UnbindFields();
                    BindFields();
                }
            }

            private FlowchartEditorQol _asset;

            private void UnbindFields()
            {
                CommandsListView.Unbind();
                PropertiesContainer.Unbind();
            }

            private void BindFields()
            {
                if (_asset != null)
                {
                    SerializedObject serializedObject = new SerializedObject(_asset);
                    CommandsListView.Bind(serializedObject);
                    PropertiesContainer.Bind(serializedObject);//
                    NameField.value = _asset.name; 
                    // ^Not bound to serialized property, since we want to let 
                    // the entry logic validate it
                }
            }
        }
    }
}
