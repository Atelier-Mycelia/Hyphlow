using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.EditorExt
{
    public sealed class FlowchartEditorQolManagerWindow : EditorWindow
    {
        private const string ResourcesPath = "Assets/Resources/AtMycelia/Hyphlow";
        private const string UxmlPath = "Editor/UIToolkitTemplates/FlowchartEditorQolItem";
        private const string CommandsToHidePropertyName = "_commandsToHide";

        [MenuItem("Window/Atelier Mycelia/Hyphlow/Manage Flowchart Editor Qol")]
        public static void Open()
        {
            FlowchartEditorQolManagerWindow window = GetWindow<FlowchartEditorQolManagerWindow>();
            window.titleContent = new GUIContent("Flowchart Editor QoL Manager");
            window.minSize = window.maxSize = _windowSize;
            window.Show();
        }

        private static Vector2 _windowSize = new Vector2(600f, 800f);

        private void OnEnable()
        {
            if (_s != null && _s != this)
            {
                _s.Focus();
                Close();
                return;
            }

            _s = this;
            LoadItemTemplate();
            LoadAllQolAssets();
        }

        private static FlowchartEditorQolManagerWindow _s;
        private VisualTreeAsset _itemTemplate;
        
        private void LoadItemTemplate()
        {
            _itemTemplate = Resources.Load<VisualTreeAsset>(UxmlPath);
            if (_itemTemplate == null)
            {
                Debug.LogError($"Failed to load UXML template from Resources at path: {UxmlPath}");
            }
        }

        private void OnDisable()
        {
            if (_s == this)
            {
                _s = null;
            }
        }

        private void OnFocus()
        {
            LoadAllQolAssets();
            RefreshListView();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            VisualElement content = new VisualElement();
            content.style.paddingLeft = 8f;
            content.style.paddingRight = 8f;
            content.style.paddingTop = 8f;
            content.style.paddingBottom = 8f;

            UitkLabel titleLabel = new UitkLabel("Flowchart Editor QoL Assets");
            titleLabel.style.fontSize = 14;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 8f;

            VisualElement addNewSection = new VisualElement();
            addNewSection.style.flexDirection = FlexDirection.Row;
            addNewSection.style.marginBottom = 8f;

            _newAssetNameField = new TextField("New Asset Name")
            {
                value = "FlowchartEditorQOL"
            };
            _newAssetNameField.style.flexGrow = 1f;

            _addNewButton = new Button(OnAddNewQolAsset)
            {
                text = "Create New"
            };
            _addNewButton.style.marginLeft = 4f;

            addNewSection.Add(_newAssetNameField);
            addNewSection.Add(_addNewButton);

            _qolAssetsListView = new ListView(_qolAssetsBuffer,
                -1,
                MakeQolAssetItem,
                BindQolAssetItem)
            {
                selectionType = SelectionType.Single,
                reorderable = false,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            _qolAssetsListView.style.minHeight = 200f;
            _qolAssetsListView.style.flexGrow = 1f;

            content.Add(titleLabel);
            content.Add(addNewSection);
            content.Add(_qolAssetsListView);

            rootVisualElement.Add(content);

            RefreshListView();
        }

        private TextField _newAssetNameField;
        private Button _addNewButton;
        private ListView _qolAssetsListView;
        private readonly List<FlowchartEditorQol> _qolAssetsBuffer = new List<FlowchartEditorQol>();

        private void LoadAllQolAssets()
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

            _qolAssetsBuffer.Sort(SortQolAssets);
        }

        private int SortQolAssets(FlowchartEditorQol first, FlowchartEditorQol second)
        {
            return string.Compare(first.name, second.name, System.StringComparison.Ordinal);
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

            bool foundAllRequiredElements = foldout != null && propertiesContainer != null &&
                commandsToHideField != null && selectButton != null &&
                deleteButton != null;
            if (!foundAllRequiredElements)
            {
                Debug.LogError("Failed to find required elements in UXML template.");
                return root;
            }

            if (commandsToHideField == null)
            {
                Debug.LogError("Failed to find CommandsToHideField in UXML template.");
                return root;
            }

            if (nameField == null)
            {
                Debug.LogError("Failed to find NameField in UXML template.");
                return root;
            }

            // Keep list binding fully programmatic for this field.
            commandsToHideField.bindingPath = string.Empty;

            QolAssetRow rowData = new QolAssetRow(foldout, propertiesContainer, commandsToHideField,
                selectButton, deleteButton, nameField);
            root.userData = rowData;

            // register commit handlers (Enter and focus out). Use rowData index at bind time.
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

        private readonly IDictionary<QolAssetRow, FlowchartEditorQol> _rowsInWindow = 
            new Dictionary<QolAssetRow, FlowchartEditorQol>();

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

            rowData.Foldout.text = qol != null ? 
                qol.name : 
                "<None>";
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
            
            // Binds Slider/Toggle fields declared in UXML by their binding-path.
            rowData.PropertiesContainer.Bind(serializedObject);

            // List<string> field is bound programmatically.
            SerializedProperty commandsToHideProp = serializedObject.FindProperty(CommandsToHidePropertyName);
            if (commandsToHideProp != null)
            {
                rowData.CommandsToHideField.BindProperty(commandsToHideProp);
            }
            else
            {
                Debug.LogWarning($"Could not find '{CommandsToHidePropertyName}' on {qol.name}.");
            }

            // set the NameField value programmatically using the guard:
            rowData.IsUpdatingName = true;
            rowData.NameField.value = qol.name;
            rowData.IsUpdatingName = false;

            _rowsInWindow[rowData] = qol;
        }

        private void RefreshFoldoutTexts()
        {
            foreach (var kvp in _rowsInWindow)
            {
                QolAssetRow rowData = kvp.Key;
                FlowchartEditorQol qol = kvp.Value;
                if (rowData != null && qol != null)
                {
                    rowData.Foldout.text = qol.name;
                }
            }
        }

        private void OnAddNewQolAsset()
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
                EditorUtility.DisplayDialog("File Exists", $"An asset with the name " +
                    $"'{fileName}' already exists.", "OK");
                return;
            }

            FlowchartEditorQol newQol = CreateInstance<FlowchartEditorQol>();
            AssetDatabase.CreateAsset(newQol, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            LoadAllQolAssets();
            RefreshListView();

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

        private void OnAssetRenamed()
        {
            RefreshFoldoutTexts();
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
                RefreshListView();
            }
        }

        private bool TryGetQolAssetIndex(QolAssetRow rowData, out int index)
        {
            index = rowData.Index;
            return index >= 0 && index < _qolAssetsBuffer.Count;
        }

        // Add CommitNameEdit helper and small guard to avoid triggering on programmatic updates:
        private void CommitNameEdit(QolAssetRow rowData)
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

            // Prevent reacting if we're currently updating the field programmatically
            if (rowData.IsUpdatingName)
            {
                return;
            }

            string newName = rowData.NameField.value?.Trim() ?? string.Empty;
            string currentName = qol.name;

            if (string.IsNullOrEmpty(newName) || newName == currentName)
            {
                // restore programmatic value to ensure UI shows canonical name in case of invalid input
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
                // restore old name on failure
                rowData.IsUpdatingName = true;
                rowData.NameField.value = currentName;
                rowData.IsUpdatingName = false;
                return;
            }
            else
            {
                rowData.Foldout.text = newName;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // optional: select and ping the asset
            Selection.activeObject = qol;
            EditorGUIUtility.PingObject(qol);
        }

        private sealed class QolAssetRow
        {
            public QolAssetRow(Foldout foldout, VisualElement propertiesContainer,
                PropertyField commandsToHideField, Button selectButton, Button deleteButton, TextField nameField)
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
            public bool IsUpdatingName { get; set; } = false;
            public int Index { get; set; }
            public FlowchartEditorQol BoundQolAsset { get; set; }
        }
    }
}
