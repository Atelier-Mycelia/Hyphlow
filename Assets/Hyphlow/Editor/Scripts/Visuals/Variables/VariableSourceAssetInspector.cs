using AtMycelia.EditorUtils;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.EditorUtils
{
    [CustomEditor(typeof(VariableSourceAsset))]
    public class VariableSourceAssetInspector : Editor
    {
        protected virtual void OnEnable()
        {
            var target = (VariableSourceAsset)this.target;
            target.Refresh(); // Ensure variable ownership is properly asserted
            PrepGUI();
            ToggleSubs(false);
            ToggleSubs(true);
        }

        protected virtual void PrepGUI()
        {
            _manager = new VariableRowManager();
            var visualHandlerLookup = RowVisualHandlerRegistry.VisualHandlerLookup;
            handlerPool ??= new RowVisualHandlerPool(_resolver, visualHandlerLookup);
            rowPool ??= new VariableRowPool();
            uxml = Resources.Load<VisualTreeAsset>(pathToUxml);

            rootElement = new VisualElement();

            inspectorRoot = uxml.CloneTree();
            rootElement.Add(inspectorRoot);
            BuildManager(inspectorRoot);
            AddGlobalSourceButtons(inspectorRoot);
        }

        protected VariableRowManager _manager;
        protected RowVisualHandlerPool handlerPool;
        protected VariableRowPool rowPool;
        protected VisualTreeAsset uxml;
        protected readonly string pathToUxml = "Editor/UIToolkitTemplates/VariableDisplayEditor";
        protected VisualElement rootElement;
        protected TemplateContainer inspectorRoot;
        protected Button _registerGlobalButton;
        protected Button _unregisterGlobalButton;
        protected DropdownField _registryConfigDropdown;
        protected IReadOnlyList<VariableRegistryConfig> _registryConfigs;
        protected readonly List<string> _registryConfigLabels = new List<string>();
        protected int _selectedRegistryConfigIndex = -1;

        protected void BuildManager(VisualElement rootElem)
        {
            var varSource = (VariableSourceAsset)target;
            if (varSource == null)
                return;

            var holder = rootElem;

            PrepFactory();
            void PrepFactory()
            {
                _factoryInitArgs.Holder = holder;
                _factoryInitArgs.HandlerPool = handlerPool;
                _factoryInitArgs.RowPool = rowPool;
                _rowFactory.Init(_factoryInitArgs);
            }

            VariableListView view;
            Button addBtn;
            PrepVarListView();
            void PrepVarListView()
            {
                var list = rootElem.Q<ListView>("rowList");
                var count = rootElem.Q<UitkLabel>("varCountLabel");
                addBtn = rootElem.Q<Button>("addVarButton");

                var listViewArgs = new VariableListViewInitArgs()
                {
                    List = list,
                    CountLabel = count,
                    RowFactory = _rowFactory,
                    VariableSource = varSource,
                    AssetResolver = new DefaultEditorAssetResolver(),
                };
                view = new VariableListView(listViewArgs);
            }

            InitManager();
            void InitManager()
            {
                VRowManagerInitArgs managerInitArgs = new VRowManagerInitArgs
                {
                    Root = rootElem,
                    AddButton = addBtn,
                    VariableSource = varSource,
                    VariableListView = view,
                };

                _manager.Init(managerInitArgs);
            }
        }

        protected VariableRowFactoryInitArgs _factoryInitArgs = new VariableRowFactoryInitArgs();
        protected VariableRowFactory _rowFactory = new VariableRowFactory();

        protected virtual void ToggleSubs(bool on)
        {
            var source = (VariableSourceAsset)target;
            if (on)
            {
                HyphlowEditorSignals.VarRowControlLostFocus += OnVarRowControlLostFocus;
                source.VariableAdded += OnVariableAdded;
                source.VariableRemoved += OnVariableRemoved;
                source.VariablesReordered += UpdateSourceAssetFile;
                source.Refreshed += UpdateSourceAssetFile;
            }
            else
            {
                HyphlowEditorSignals.VarRowControlLostFocus -= OnVarRowControlLostFocus;
                source.VariableAdded -= OnVariableAdded;
                source.VariableRemoved -= OnVariableRemoved;
                source.VariablesReordered -= UpdateSourceAssetFile;
                source.Refreshed -= UpdateSourceAssetFile;
            }
        }

        protected virtual void OnVarRowControlLostFocus(FocusOutEvent evt)
        {
            UpdateSourceAssetFile();
        }

        protected virtual void UpdateSourceAssetFile()
        {
            if (target is VariableSourceAsset source)
            {
                EditorUtility.SetDirty(source);
                AssetDatabase.SaveAssetIfDirty(source);
                Debug.Log($"VariableSourceInspector: Updated source asset file");
            }
        }

        private void OnVariableRemoved(IVariable variable)
        {
            UpdateSourceAssetFile();
        }

        private void OnVariableAdded(IVariable variable)
        {
            UpdateSourceAssetFile();
        }

        protected readonly IRowVisualHandlerResolver _resolver = new RowVisualHandlerResolver();
        
        // This executes twice in a row when the asset is clicked, and then once again when you click some
        // other asset
        public override VisualElement CreateInspectorGUI()
        {
            return rootElement;
        }

        protected virtual void OnDisable()
        {
            _manager?.Dispose();
            _manager = null;
            inspectorRoot = null;
            rootElement = null;
            ToggleSubs(false);
        }

        protected void AddGlobalSourceButtons(VisualElement rootElem)
        {
            var container = new VisualElement();

            _registryConfigDropdown = new DropdownField("Registry Config");
            _registryConfigDropdown.RegisterValueChangedCallback(OnRegistryConfigDropdownChanged);

            _registerGlobalButton = new Button(OnRegisterGlobalSourceClicked)
            {
                text = "Register as Global Source",
            };

            _unregisterGlobalButton = new Button(OnUnregisterGlobalSourceClicked)
            {
                text = "UNregister as Global Source",
            };

            _registerGlobalButton.style.marginTop = _registerGlobalButton.style.marginBottom = 
                _unregisterGlobalButton.style.marginTop = _unregisterGlobalButton.style.marginBottom =
                _regButtonTopBotMargins;

            _registerGlobalButton.style.height = _unregisterGlobalButton.style.height =  _regButtonHeight;

            _registerGlobalButton.style.fontSize = _unregisterGlobalButton.style.fontSize = 14;

            container.Add(_registryConfigDropdown);
            container.Add(_registerGlobalButton);
            container.Add(_unregisterGlobalButton);
            rootElem.Add(container);

            RefreshRegistryConfigDropdown();
            RefreshGlobalSourceButtons();
        }

        private static readonly int _regButtonTopBotMargins = 5;
        private static readonly int _regButtonHeight = 30;

        protected void OnRegisterGlobalSourceClicked()
        {
            UpdateGlobalSourceRegistration(true);
        }

        protected void OnUnregisterGlobalSourceClicked()
        {
            UpdateGlobalSourceRegistration(false);
        }

        protected void UpdateGlobalSourceRegistration(bool register)
        {
            var source = (VariableSourceAsset)target;
            if (source == null)
            {
                return;
            }

            RefreshRegistryConfigDropdown();

            VariableRegistryConfig config = GetSelectedRegistryConfig();
            if (config == null)
            {
                Debug.LogWarning("VariableSourceInspector: VariableRegistryConfig not found in Resources.");
                RefreshGlobalSourceButtons();
                return;
            }

            List<VariableSourceAsset> sources = new List<VariableSourceAsset>(config.GlobalSources);
            bool isRegistered = sources.Contains(source);

            if (register && !isRegistered)
            {
                sources.Add(source);
                ApplyGlobalSources(config, sources);
            }
            else if (!register && isRegistered)
            {
                sources.Remove(source);
                ApplyGlobalSources(config, sources);
            }

            RefreshGlobalSourceButtons();
        }

        protected void ApplyGlobalSources(VariableRegistryConfig config, List<VariableSourceAsset> sources)
        {
            config.SetGlobalSources(sources);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
        }

        protected void RefreshGlobalSourceButtons()
        {
            var source = (VariableSourceAsset)target;
            if (source == null)
            {
                return;
            }

            RefreshRegistryConfigDropdown();

            VariableRegistryConfig config = GetSelectedRegistryConfig();
            bool isRegistered = false;

            if (config != null)
            {
                List<VariableSourceAsset> sources = new List<VariableSourceAsset>(config.GlobalSources);
                isRegistered = sources.Contains(source);
            }

            _registerGlobalButton?.SetEnabled(config != null && !isRegistered);

            _unregisterGlobalButton?.SetEnabled(config != null && isRegistered);
        }

        private void RefreshRegistryConfigDropdown()
        {
            _registryConfigs = VariableRegistryService.LoadDefaultConfig();
            _registryConfigLabels.Clear();

            if (_registryConfigs != null)
            {
                for (int i = 0; i < _registryConfigs.Count; i++)
                {
                    _registryConfigLabels.Add(GetRegistryConfigLabel(_registryConfigs[i], i));
                }
            }

            if (_registryConfigDropdown == null)
            {
                return;
            }

            _registryConfigDropdown.choices = _registryConfigLabels;

            if (_registryConfigLabels.Count == 0)
            {
                _selectedRegistryConfigIndex = -1;
                _registryConfigDropdown.SetValueWithoutNotify(string.Empty);
                return;
            }

            if (_selectedRegistryConfigIndex < 0 || _selectedRegistryConfigIndex >= _registryConfigLabels.Count)
            {
                _selectedRegistryConfigIndex = 0;
            }

            _registryConfigDropdown.SetValueWithoutNotify(_registryConfigLabels[_selectedRegistryConfigIndex]);
        }

        private void OnRegistryConfigDropdownChanged(ChangeEvent<string> evt)
        {
            int index = _registryConfigLabels.IndexOf(evt.newValue);
            _selectedRegistryConfigIndex = index;
            RefreshGlobalSourceButtons();
        }

        private VariableRegistryConfig GetSelectedRegistryConfig()
        {
            if (_registryConfigs == null || _registryConfigs.Count == 0)
            {
                return null;
            }

            if (_selectedRegistryConfigIndex < 0 || _selectedRegistryConfigIndex >= _registryConfigs.Count)
            {
                return null;
            }

            return _registryConfigs[_selectedRegistryConfigIndex];
        }

        private static string GetRegistryConfigLabel(VariableRegistryConfig config, int index)
        {
            return config != null ? config.name : $"Missing Config {index + 1}";
        }
    }
}