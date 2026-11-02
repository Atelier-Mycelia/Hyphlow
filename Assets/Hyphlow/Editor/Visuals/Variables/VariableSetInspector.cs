using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt
{
    [CustomEditor(typeof(VariableSet))]
    public class VariableSetInspector : Editor
    {
        protected virtual void OnEnable()
        {
            var target = (VariableSet)this.target;
            target.Refresh(); // Ensure variable ownership is properly asserted
            PrepGUI();
            SetSubs(false);
            SetSubs(true);
        }

        protected virtual void PrepGUI()
        {
            _uxml = Resources.Load<VisualTreeAsset>(_pathToUxml);

            _rootElement = new VisualElement();

            _inspectorRoot = _uxml.CloneTree();
            _rootElement.Add(_inspectorRoot);
            _inspectorRoot.Bind(serializedObject);
            BuildManager(_inspectorRoot);
        }

        protected VariableRowManager _manager;
        protected VisualTreeAsset _uxml;
        protected static readonly string _pathToUxml =
            "Editor/Uxml/VariableSetInspector";
        protected VisualElement _rootElement;
        protected TemplateContainer _inspectorRoot;

        protected void BuildManager(VisualElement rootElem)
        {
            var varSource = (VariableSet)target;
            if (varSource == null)
                return;

            _manager?.Dispose();
            _manager = VariableRowManager.CreateFor(rootElem, varSource);
        }

        protected virtual void SetSubs(bool on)
        {
            var source = (VariableSet)target;
            if (on)
            {
                source.VariableAdded += OnVariableAdded;
                source.VariableRemoved += OnVariableRemoved;
                source.VariablesReordered += UpdateSourceAssetFile;
                source.Refreshed += UpdateSourceAssetFile;
            }
            else
            {
                source.VariableAdded -= OnVariableAdded;
                source.VariableRemoved -= OnVariableRemoved;
                source.VariablesReordered -= UpdateSourceAssetFile;
                source.Refreshed -= UpdateSourceAssetFile;
            }
        }

        protected virtual void UpdateSourceAssetFile()
        {
            // Detached copies are what the Control Panel stages edits on; they only reach
            // disk when it saves them
            if (target is VariableSet source && !source.IsDetached)
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

        // This executes twice in a row when the asset is clicked, and then once again when you click some
        // other asset
        public override VisualElement CreateInspectorGUI()
        {
            return _rootElement;
        }

        protected virtual void OnDisable()
        {
            _manager?.Dispose();
            _manager = null;
            _inspectorRoot = null;
            _rootElement = null;
            SetSubs(false);
        }
    }
}
