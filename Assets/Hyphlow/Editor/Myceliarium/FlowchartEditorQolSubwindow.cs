using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Subwindow for managing Flowchart Editor QoL assets in the Control Panel.
    /// </summary>
    public sealed class FlowchartEditorQolSubwindow : ControlPanelSubwindow
    {
        private const string ResourcesPath = "Assets/Resources/AtMycelia/Hyphlow";
        private const string ItemUxmlPath = "Editor/UIToolkitTemplates/FlowchartEditorQolItem";
        private const string CommandsToHidePropertyName = "_commandsToHide";

        public override string PathToUxml => 
            "Editor/UIToolkitTemplates/Myceliarium/FlowchartEditorQolSubmenu";

        protected override void RegisterVisualElements()
        {
            base.RegisterVisualElements();

            _newAssetNameField = Root.Q<TextField>("NewAssetNameField");
            _createButton = Root.Q<Button>("CreateButton");
            _qolAssetsListView = Root.Q<ListView>("QolAssetsListView");

            bool allFound = _newAssetNameField != null && _createButton != null 
                && _qolAssetsListView != null;
            if (!allFound)
            {
                string logMessage = "Failed to find required UI elements in" +
                    "FlowchartEditorQolSubmenu.uxml.\n" +
                    $"NewAssetNameField: {_newAssetNameField != null},\n" +
                    $"CreateButton: {_createButton != null},\n" +
                    $"QolAssetsListView: {_qolAssetsListView != null}";
                throw new InvalidOperationException(logMessage);
            }

            LoadItemTemplate();
            InitializeListView();
            ToggleSubs(true);
        }

        private TextField _newAssetNameField;
        private Button _createButton;
        private ListView _qolAssetsListView;
        private VisualTreeAsset _itemTemplate;
        private readonly List<FlowchartEditorQol> _qolAssetsBuffer = new List<FlowchartEditorQol>();
        private readonly Dictionary<QolAssetRow, FlowchartEditorQol> _rowsInWindow = 
            new Dictionary<QolAssetRow, FlowchartEditorQol>();

        private void LoadItemTemplate()
        {
            _itemTemplate = Resources.Load<VisualTreeAsset>(ItemUxmlPath);
            if (_itemTemplate == null)
            {
                Debug.LogError($"Failed to load UXML template from Resources at path: {ItemUxmlPath}");
            }
        }

        private void InitializeListView()
        {
            _qolAssetsListView.makeItem = MakeQolAssetItem;
            _qolAssetsListView.bindItem = BindQolAssetItem;
            _qolAssetsListView.itemsSource = _qolAssetsBuffer;
            _qolAssetsListView.selectionType = SelectionType.Single;
            _qolAssetsListView.reorderable = false;
            _qolAssetsListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            _qolAssetsListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _qolAssetsListView.style.minHeight = 300f;
            _qolAssetsListView.style.flexGrow = 1f;

            LoadAllQolAssets();
        }

        private void ToggleSubs(bool on)
        {
            if (on)
            {
                _createButton.clicked += OnCreateNewQolAsset;
            }
            else
            {
                _createButton.clicked -= OnCreateNewQolAsset;
            }
        }

        public void LoadAllQolAssets()
        {
            _qolAssetsBuffer.Clear();

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FlowchartEditorQol)}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                FlowchartEditorQol qol = AssetDatabase.LoadAssetAtPath<FlowchartEditorQol>(path);
                if (qol != null)
                {
                    _qolAssetsBuffer.Add(qol);
                }
            }

            _qolAssetsBuffer.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            RefreshListView();
        }

        private void RefreshListView()
        {
            _rowsInWindow.Clear();
            _qolAssetsListView?.RefreshItems();
        }

        private VisualElement MakeQolAssetItem()
        {
            if (_itemTemplate == null)
            {
                Debug.LogError("Item template is null. Cannot create QoL asset item.");
                return new VisualElement();
            }

            VisualElement root = _itemTemplate.CloneTree();

            Foldout foldout = root.Q<Foldout>("QolItemFoldout");
            VisualElement propertiesContainer = root.Q<VisualElement>("PropertiesContainer");
            PropertyField commandsToHideField = root.Q<PropertyField>("CommandsToHideField");
            Button selectButton = root.Q<Button>("SelectButton");
            Button deleteButton = root.Q<Button>("DeleteButton");
            TextField nameField = root.Q<TextField>("NameField");

            if (foldout == null || propertiesContainer == null || commandsToHideField == null ||
                selectButton == null || deleteButton == null || nameField == null)
            {
                Debug.LogError("Failed to find required elements in UXML template.");
                return root;
            }

            commandsToHideField.bindingPath = string.Empty;

            QolAssetRow rowData = new QolAssetRow(foldout, propertiesContainer, commandsToHideField,
                selectButton, deleteButton, nameField);
            root.userData = rowData;

            nameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    CommitNameEdit(rowData);
                    evt.StopPropagation();
                }
            });
            nameField.RegisterCallback<FocusOutEvent>(_ => CommitNameEdit(rowData));

            selectButton.clicked += () => OnSelectQolAsset(rowData);
            deleteButton.clicked += () => OnDeleteQolAsset(rowData);

            return root;
        }

        private void BindQolAssetItem(VisualElement element, int index)
        {
            if (element.userData is not QolAssetRow rowData)
            {
                return;
            }

            rowData.Index = index;
            FlowchartEditorQol qol = index >= 0 && index < _qolAssetsBuffer.Count
                ? _qolAssetsBuffer[index]
                : null;

            rowData.Foldout.text = qol != null ? qol.name : "<None>";
            rowData.PropertiesContainer.Unbind();
            rowData.CommandsToHideField.Unbind();

            if (qol == null)
            {
                rowData.Foldout.value = false;
                rowData.PropertiesContainer.SetEnabled(false);
                return;
            }

            rowData.BoundQolAsset = qol;
            rowData.PropertiesContainer.SetEnabled(true);
            rowData.Foldout.value = false;

            SerializedObject serializedObject = new SerializedObject(qol);
            rowData.PropertiesContainer.Bind(serializedObject);

            SerializedProperty commandsToHideProp = serializedObject.FindProperty(CommandsToHidePropertyName);
            if (commandsToHideProp != null)
            {
                rowData.CommandsToHideField.BindProperty(commandsToHideProp);
            }
            else
            {
                Debug.LogWarning($"Could not find '{CommandsToHidePropertyName}' on {qol.name}.");
            }

            rowData.IsUpdatingName = true;
            rowData.NameField.value = qol.name;
            rowData.IsUpdatingName = false;

            _rowsInWindow[rowData] = qol;
        }

        private void OnCreateNewQolAsset()
        {
            string assetName = _newAssetNameField.value;
            if (string.IsNullOrWhiteSpace(assetName))
            {
                EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid asset name.", "OK");
                return;
            }

            string fileName = assetName;
            if (!fileName.EndsWith(".asset"))
            {
                fileName += ".asset";
            }

            if (!Directory.Exists(ResourcesPath))
            {
                Directory.CreateDirectory(ResourcesPath);
            }

            string fullPath = Path.Combine(ResourcesPath, fileName).Replace("\\", "/");

            if (File.Exists(fullPath))
            {
                EditorUtility.DisplayDialog("File Exists", 
                    $"An asset with the name '{fileName}' already exists.", "OK");
                return;
            }

            FlowchartEditorQol newQol = ScriptableObject.CreateInstance<FlowchartEditorQol>();
            AssetDatabase.CreateAsset(newQol, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadAllQolAssets();

            Selection.activeObject = newQol;
            EditorGUIUtility.PingObject(newQol);
        }

        private void OnSelectQolAsset(QolAssetRow rowData)
        {
            if (!TryGetQolAssetIndex(rowData, out int index))
            {
                return;
            }

            FlowchartEditorQol qol = _qolAssetsBuffer[index];
            if (qol == null)
            {
                return;
            }

            Selection.activeObject = qol;
            EditorGUIUtility.PingObject(qol);
        }

        private void OnDeleteQolAsset(QolAssetRow rowData)
        {
            if (!TryGetQolAssetIndex(rowData, out int index))
            {
                return;
            }

            FlowchartEditorQol qol = _qolAssetsBuffer[index];
            if (qol == null)
            {
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog("Delete Asset",
                $"Are you sure you want to delete '{qol.name}'? This action cannot be undone.",
                "Delete",
                "Cancel");

            if (confirmed)
            {
                string path = AssetDatabase.GetAssetPath(qol);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                LoadAllQolAssets();
            }
        }

        private bool TryGetQolAssetIndex(QolAssetRow rowData, out int index)
        {
            index = rowData.Index;
            return index >= 0 && index < _qolAssetsBuffer.Count;
        }

        private void CommitNameEdit(QolAssetRow rowData)
        {
            if (!TryGetQolAssetIndex(rowData, out int index))
            {
                return;
            }

            FlowchartEditorQol qol = _qolAssetsBuffer[index];
            if (qol == null || rowData.IsUpdatingName)
            {
                return;
            }

            string newName = rowData.NameField.value?.Trim() ?? string.Empty;
            string currentName = qol.name;

            if (string.IsNullOrEmpty(newName) || newName == currentName)
            {
                rowData.IsUpdatingName = true;
                rowData.NameField.value = currentName;
                rowData.IsUpdatingName = false;
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(qol);
            string err = AssetDatabase.RenameAsset(assetPath, newName);
            if (!string.IsNullOrEmpty(err))
            {
                EditorUtility.DisplayDialog("Rename Failed", err, "OK");
                rowData.IsUpdatingName = true;
                rowData.NameField.value = currentName;
                rowData.IsUpdatingName = false;
                return;
            }

            rowData.Foldout.text = newName;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = qol;
            EditorGUIUtility.PingObject(qol);
        }

        public override void Dispose()
        {
            ToggleSubs(false);
            _rowsInWindow.Clear();
            _qolAssetsBuffer.Clear();
            base.Dispose();
        }

        private sealed class QolAssetRow
        {
            public QolAssetRow(Foldout foldout, VisualElement propertiesContainer,
                PropertyField commandsToHideField, Button selectButton, Button deleteButton, 
                TextField nameField)
            {
                Foldout = foldout;
                PropertiesContainer = propertiesContainer;
                CommandsToHideField = commandsToHideField;
                SelectButton = selectButton;
                DeleteButton = deleteButton;
                NameField = nameField;
            }

            public Foldout Foldout { get; }
            public VisualElement PropertiesContainer { get; }
            public PropertyField CommandsToHideField { get; }
            public Button SelectButton { get; }
            public Button DeleteButton { get; }
            public TextField NameField { get; }
            public bool IsUpdatingName { get; set; }
            public int Index { get; set; }
            public FlowchartEditorQol BoundQolAsset { get; set; }
        }
    }
}
