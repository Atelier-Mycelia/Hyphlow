using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    [CustomEditor(typeof(VariableManagerComponent))]
    public class VariableManagerComponentInspector : Editor
    {
        protected VariableRowManager _manager;
        protected VisualTreeAsset _uxml;
        protected VisualElement _rootElement;
        protected TemplateContainer _inspectorRoot;

        protected readonly string _pathToUxml = "Editor/Uxml/VariableDisplayEditor";

        protected virtual void OnEnable()
        {
            _manager?.Dispose();
            PrepGUI();
        }

        protected virtual void OnDisable()
        {
            _manager?.Dispose();
            _manager = null;
            _inspectorRoot = null;
            _rootElement = null;
        }

        public override VisualElement CreateInspectorGUI()
        {
            return _rootElement;
        }

        protected virtual void PrepGUI()
        {
            _uxml = Resources.Load<VisualTreeAsset>(_pathToUxml);

            _rootElement = new VisualElement();
            _inspectorRoot = _uxml.CloneTree();
            _inspectorRoot.style.marginTop = 5;
            _rootElement.Add(_inspectorRoot);

            BuildManager(_inspectorRoot);
            AddMigrationButton(_inspectorRoot);
            AddRefreshButton(_inspectorRoot);
        }

        protected void BuildManager(VisualElement rootElem)
        {
            VariableManagerComponent component = (VariableManagerComponent)target;
            if (component == null)
            {
                return;
            }

            _manager = VariableRowManager.CreateFor(rootElem, component);
        }

        protected void AddMigrationButton(VisualElement rootElem)
        {
            Button migrateButton = new Button(OnMigrateClicked)
            {
                text = "Migrate from Flowchart",
            };

            migrateButton.style.height = 30;
            migrateButton.style.fontSize = 14;
            migrateButton.style.marginTop = 6;
            rootElem.Add(migrateButton);
        }

        private void OnMigrateClicked()
        {
            VariableManagerComponent component = (VariableManagerComponent)target;
            component.MigrateFromFlowchart(out _);
        }

        protected void AddRefreshButton(VisualElement rootElem)
        {
            Button refreshButton = new Button(OnRefreshClicked)
            {
                text = "Refresh",
                tooltip = "Might need to use this if you're getting errors about null vars on this GameObject."
            };

            refreshButton.style.height = 30;
            refreshButton.style.fontSize = 14;
            refreshButton.style.marginTop = 6;
            rootElem.Add(refreshButton);
        }

        private void OnRefreshClicked()
        {
            VariableManagerComponent component = (VariableManagerComponent)target;
            component.Refresh();
        }
    }
}
