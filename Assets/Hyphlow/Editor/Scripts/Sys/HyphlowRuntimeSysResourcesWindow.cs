using System.Collections.Generic;
using AtMycelia.AmaniTween;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;
using AtMycelia.Hyphlow.Sys;

namespace AtMycelia.Hyphlow.EditorUtils
{
    public sealed class HyphlowRuntimeSysResourcesWindow : EditorWindow
    {
        private const string ResourcesSubfolderPath = "Runtime";
        private const string AssetName = "HyphlowRuntimeSysAssets";
        private const float VariableRegistryConfigItemHeight = 22f;

        [MenuItem("Window/Atelier Mycelia/Hyphlow/Hyphlow Runtime Sys Resources")]
        public static void Open()
        {
            HyphlowRuntimeSysResourcesWindow window = GetWindow<HyphlowRuntimeSysResourcesWindow>();
            window.titleContent = new GUIContent("Hyphlow Runtime Sys Resources");
            window.Show();
        }

        private void OnEnable()
        {
            if (_s != null && _s != this)
            {
                _s.Focus();
                Close();
                return;
            }

            _s = this;
            EnsureResources();
        }

        private static HyphlowRuntimeSysResourcesWindow _s;

        private void EnsureResources()
        {
            if (_assets != null)
            {
                return;
            }

            _assets = SOUtils.EnsureSOExists<HyphlowRuntimeSysAssets>(ResourcesSubfolderPath, AssetName);
        }

        private HyphlowRuntimeSysAssets _assets;

        private void OnDisable()
        {
            if (_s == this)
            {
                _s = null;
            }
        }

        private void OnFocus()
        {
            RefreshFields();
        }

        public void CreateGUI()
        {
            EnsureResources();

            rootVisualElement.Clear();

            if (_assets == null)
            {
                rootVisualElement.Add(new HelpBox("HyphlowRuntimeSysAssets asset not found.", HelpBoxMessageType.Error));
                return;
            }

            VisualElement content = new VisualElement();
            content.style.paddingLeft = 8f;
            content.style.paddingRight = 8f;
            content.style.paddingTop = 8f;
            content.style.paddingBottom = 8f;

            _tweenAdapterField = new ObjectField("Tween Adapter")
            {
                objectType = typeof(DefaultTweenAdapter),
                allowSceneObjects = false
            };
            _tweenAdapterField.RegisterValueChangedCallback(OnTweenAdapterChanged);

            _addVariableRegistryConfigButton = new Button(OnAddVariableRegistryConfig)
            {
                text = "Add Registry Config"
            };

            _variableRegistryConfigListView = new ListView(_variableRegistryConfigBuffer,
                VariableRegistryConfigItemHeight,
                MakeVariableRegistryConfigItem,
                BindVariableRegistryConfigItem)
            {
                selectionType = SelectionType.Single
            };
            _variableRegistryConfigListView.style.minHeight = 120f;

            content.Add(_tweenAdapterField);
            content.Add(new UitkLabel("Variable Registry Configs"));
            content.Add(_addVariableRegistryConfigButton);
            content.Add(_variableRegistryConfigListView);

            rootVisualElement.Add(content);

            RefreshFields();
        }

        private ObjectField _tweenAdapterField;
        private ListView _variableRegistryConfigListView;
        private Button _addVariableRegistryConfigButton;
        private readonly List<VariableRegistryConfig> _variableRegistryConfigBuffer = new List<VariableRegistryConfig>();

        private void RefreshFields()
        {
            if (_assets == null)
            {
                return;
            }

            if (_tweenAdapterField != null)
            {
                _tweenAdapterField.SetValueWithoutNotify(_assets.TweenAdapter);
            }

            RefreshVariableRegistryConfigList();
        }

        private void RefreshVariableRegistryConfigList()
        {
            _variableRegistryConfigBuffer.Clear();

            if (_assets != null && _assets.VariableRegistryConfigs != null)
            {
                _variableRegistryConfigBuffer.AddRange(_assets.VariableRegistryConfigs);
            }

            _variableRegistryConfigListView?.Rebuild();
        }

        private VisualElement MakeVariableRegistryConfigItem()
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            ObjectField field = new ObjectField
            {
                objectType = typeof(VariableRegistryConfig),
                allowSceneObjects = false
            };
            field.style.flexGrow = 1f;

            Button editButton = new Button
            {
                text = "Edit"
            };

            Button removeButton = new Button
            {
                text = "Remove"
            };

            row.Add(field);
            row.Add(editButton);
            row.Add(removeButton);

            VariableRegistryConfigRow rowData = new VariableRegistryConfigRow(field, editButton, removeButton);
            row.userData = rowData;

            field.RegisterValueChangedCallback(evt => OnVariableRegistryConfigItemChanged(rowData, evt));
            editButton.clicked += () => OnEditVariableRegistryConfig(rowData);
            removeButton.clicked += () => OnRemoveVariableRegistryConfig(rowData);

            return row;
        }

        private void BindVariableRegistryConfigItem(VisualElement element, int index)
        {
            if (element.userData is not VariableRegistryConfigRow rowData)
            {
                return;
            }

            rowData.Index = index;
            VariableRegistryConfig config = index >= 0 && index < _variableRegistryConfigBuffer.Count
                ? _variableRegistryConfigBuffer[index]
                : null;

            rowData.Field.SetValueWithoutNotify(config);
        }

        private void OnAddVariableRegistryConfig()
        {
            if (_assets == null)
            {
                return;
            }

            _variableRegistryConfigBuffer.Add(null);
            ApplyVariableRegistryConfigs();
            _variableRegistryConfigListView.Rebuild();
        }

        private void OnVariableRegistryConfigItemChanged(VariableRegistryConfigRow rowData, ChangeEvent<Object> evt)
        {
            if (!TryGetVariableRegistryConfigIndex(rowData, out int index))
            {
                return;
            }

            _variableRegistryConfigBuffer[index] = evt.newValue as VariableRegistryConfig;
            ApplyVariableRegistryConfigs();
            _variableRegistryConfigListView.Rebuild();
        }

        private void OnEditVariableRegistryConfig(VariableRegistryConfigRow rowData)
        {
            if (!TryGetVariableRegistryConfigIndex(rowData, out int index))
            {
                return;
            }

            VariableRegistryConfig config = _variableRegistryConfigBuffer[index];
            if (config == null)
            {
                return;
            }

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        private void OnRemoveVariableRegistryConfig(VariableRegistryConfigRow rowData)
        {
            if (!TryGetVariableRegistryConfigIndex(rowData, out int index))
            {
                return;
            }

            _variableRegistryConfigBuffer.RemoveAt(index);
            ApplyVariableRegistryConfigs();
            _variableRegistryConfigListView.Rebuild();
        }

        private bool TryGetVariableRegistryConfigIndex(VariableRegistryConfigRow rowData, out int index)
        {
            index = rowData.Index;
            return _assets != null && index >= 0 && index < _variableRegistryConfigBuffer.Count;
        }

        private void ApplyVariableRegistryConfigs()
        {
            if (_assets == null)
            {
                return;
            }

            List<VariableRegistryConfig> configs = new List<VariableRegistryConfig>(_variableRegistryConfigBuffer);
            _assets.VariableRegistryConfigs = configs;
        }

        private void OnTweenAdapterChanged(ChangeEvent<Object> evt)
        {
            if (_assets == null)
            {
                return;
            }

            _assets.TweenAdapter = evt.newValue as DefaultTweenAdapter;
            _tweenAdapterField.SetValueWithoutNotify(_assets.TweenAdapter);
        }

        private sealed class VariableRegistryConfigRow
        {
            public VariableRegistryConfigRow(ObjectField field, Button editButton, Button removeButton)
            {
                Field = field;
                EditButton = editButton;
                RemoveButton = removeButton;
            }

            public ObjectField Field { get; }
            public Button EditButton { get; }
            public Button RemoveButton { get; }
            public int Index { get; set; }
        }
    }
}