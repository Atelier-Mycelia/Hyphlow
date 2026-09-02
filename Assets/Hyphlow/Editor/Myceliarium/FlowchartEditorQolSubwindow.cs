using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Subwindow for managing Flowchart Editor QoL assets WITHOUT auto-applying changes.
    /// All edits are made to working-state copies injected by the entry.
    /// </summary>
    public sealed class FlowchartEditorQolSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml => "Editor/Uxml/Myceliarium/FlowchartEditorQolSubmenu";

        // Mapping UI rows → working-state objects
        private readonly Dictionary<QolAssetRow, FlowchartEditorQol> _rows =
            new Dictionary<QolAssetRow, FlowchartEditorQol>();

        // UI elements
        private TextField _newAssetNameField;
        private Button _createButton;
        private ListView _listView;

        private VisualTreeAsset _itemTemplate;
        private static readonly string _itemTemplatePath =
            "Editor/Uxml/Myceliarium/FlowchartEditorQolItem";

        internal FlowchartEditorQolSubwindow(List<FlowchartEditorQol> workingBuffer)
        {
            _workingBuffer = workingBuffer;
        }

        // Working-state list injected by the entry
        private readonly List<FlowchartEditorQol> _workingBuffer;

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
            _listView.itemsSource = (System.Collections.IList)_workingBuffer;
            _listView.selectionType = SelectionType.Single;
            _listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        }

        private VisualElement MakeItem()
        {
            QolAssetRow row = CreateAssetRow();//
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
            Button deleteButton = root.Q<Button>("DeleteButton");

            QolAssetRow row = new QolAssetRow(foldout, props, nameField, commandsList, deleteButton);
            root.userData = row;
            return row;//
        }

        private void WireUpAssetRowCallbacks(QolAssetRow row)
        {
            var nameField = row.NameField;

            nameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    CommitNameEdit(row);
                    evt.StopPropagation();
                }
            });

            nameField.RegisterCallback<FocusOutEvent>(_ => CommitNameEdit(row));
            row.DeleteButton.clicked += () => DeleteWorkingAsset(row);
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
            _rows[row] = wState;

            row.Index = index;
            row.Foldout.text = wState.name;
            row.NameField.value = wState.name;

            // Bind commands list to working-state list
            row.CommandsListView.Unbind();
            SerializedObject serializedObject = new SerializedObject(wState);
            row.CommandsListView.Bind(serializedObject);

        }

        /// <summary>
        /// Called by the entry whenever working-state is refreshed.
        /// </summary>
        public void RefreshFromWorkingState()
        {
            _rows.Clear();
            _listView.RefreshItems();
        }

        
        private void CommitNameEdit(QolAssetRow row)
        {
            if (!_rows.TryGetValue(row, out var wState))
                return;

            string newName = row.NameField.value?.Trim() ?? "";
            if (string.IsNullOrEmpty(newName))
            {
                row.NameField.value = wState.name;
                return;
            }

            wState.name = newName;
            row.Foldout.text = newName;

            _workingBuffer.Sort((a, b) =>
                string.Compare(a.name, b.name, StringComparison.Ordinal));
            _listView.RefreshItems();
        }

        private void DeleteWorkingAsset(QolAssetRow row)
        {
            if (!_rows.TryGetValue(row, out var wState))
                return;

            _workingBuffer.Remove(wState);
            _rows.Remove(row);
            _listView.RefreshItems();
        }

        private void ToggleSubs(bool on)
        {
            if (on)
            {
                _createButton.clicked += CreateWorkingAsset;
            }
            else
            {
                _createButton.clicked -= CreateWorkingAsset;
            }
        }

        private void CreateWorkingAsset()
        {
            string name = _newAssetNameField.value?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                EditorUtility.DisplayDialog("Invalid Name",
                    "Please enter a valid asset name.", "OK");
                return;
            }

            var wState = ScriptableObject.CreateInstance<FlowchartEditorQol>();
            wState.name = name;
            _workingBuffer.Add(wState);

            #region Sort by name
            _workingBuffer.Sort((first, second) =>
                string.Compare(first.name, second.name, StringComparison.Ordinal));
            #endregion

            _listView.RefreshItems();
        }

        public override void Dispose()
        {
            ToggleSubs(false);
            _rows.Clear();
            // DO NOT clear _workingBuffer (the entry as a whole owns it)
            base.Dispose();
        }

        internal sealed class QolAssetRow
        {
            public QolAssetRow(Foldout foldout, VisualElement props,
                TextField nameField, PropertyField commandsList, Button deleteButton)
            {
                Foldout = foldout;
                PropertiesContainer = props;
                NameField = nameField;
                CommandsListView = commandsList;
                DeleteButton = deleteButton;
            }

            public VisualElement Root => Foldout.parent;

            public Foldout Foldout { get; }
            public VisualElement PropertiesContainer { get; }
            public TextField NameField { get; }
            public PropertyField CommandsListView { get; }
            public Button DeleteButton { get; }
            public int Index { get; set; }
        }
    }
}
