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
        public override string PathToUxml => "Editor/Uxml/Myceliarium/FlowchartEditorQolSubmenu";

        // UI elements
        private TextField _newAssetNameField;
        private Button _createButton;
        private ListView _listView;

        private VisualTreeAsset _itemTemplate;
        private static readonly string _itemTemplatePath =
            "Editor/Uxml/Myceliarium/FlowchartEditorQolItem";

        // Working-state list injected by the entry
        private readonly List<FlowchartEditorQol> _workingBuffer;

        internal Action<string> CreateRequested { get; set; } = delegate { };
        internal Action<int, string> RenameRequested { get; set; } = delegate { };
        internal Action<int> DeleteRequested { get; set; } = delegate { };
        internal Action<int> SelectRequested { get; set; } = delegate { };

        internal FlowchartEditorQolSubwindow(List<FlowchartEditorQol> workingBuffer)
        {
            _workingBuffer = workingBuffer;
        }

        protected override void RegisterVisualElements()
        {
            _newAssetNameField = Root.Q<TextField>("NewAssetNameField");
            _createButton = Root.Q<Button>("CreateButton");
            _listView = Root.Q<ListView>("QolAssetsListView");

            LoadItemTemplate();
            InitListView();
            ToggleSubs(true);
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
            // The stuff we can't put in ToggleSubs due to needing the row
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

        private void ToggleSubs(bool on)
        {
            if (on)
            {
                _createButton.clicked += RaiseCreateRequested;
            }
            else
            {
                _createButton.clicked -= RaiseCreateRequested;
            }
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
            ToggleSubs(false);
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
