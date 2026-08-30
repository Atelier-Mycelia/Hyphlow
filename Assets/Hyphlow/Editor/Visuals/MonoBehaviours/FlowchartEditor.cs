using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    [CustomEditor(typeof(Flowchart))]
    public class FlowchartEditor : Editor
    {
        private const bool _debugInspectorLifecycle = false;

        public static bool FlowchartDataStale { get; set; }

        protected virtual void OnEnable()
        {
            LogLifecycle(nameof(OnEnable), "Entered");

            if (EraseOrphanedInstance())
            {
                LogLifecycle(nameof(OnEnable), "Orphaned instance detected and destroyed");
                return;
            }

            _addTexture = HyphlowEditorSysAssets.AddSmall;
            ToggleSubs(true);
            _fcTarg = target as Flowchart;
            _fcTarg.UpdateHideFlags();
        }

        private Flowchart _fcTarg;

        private void ToggleSubs(bool on)
        {
            if (on)
            {
                FlowchartEditorQolRegistry.RegistryRefreshed += 
                    OnFlowchartEditorQolRegistryRefreshed;
            }
            else
            {
                FlowchartEditorQolRegistry.RegistryRefreshed -= 
                    OnFlowchartEditorQolRegistryRefreshed;
            }
        }

        private void OnFlowchartEditorQolRegistryRefreshed()
        {
            if (_popup == null || _popup.choices == null)
            {
                return;
            }
            FlowchartEditorQol usedByFc = _fcTarg.EditorQol;
            var updated = FlowchartEditorQolRegistry.GetAll().ToList();
            _popup.choices.Clear();
            _popup.choices.AddRange(updated);
            // Keep selection consistent if possible
            FlowchartEditorQol newSelection = usedByFc != null && updated.Contains(usedByFc) ? 
                usedByFc : 
                null;
            if (newSelection == null && updated.Count > 0)
            {
                // Since when possible, we want each Flowchart to have a valid EditorQol
                newSelection = updated[0];
            }
            _popup.SetValueWithoutNotify(newSelection);
            RebindEditorQolFields();
            _fcTarg.UpdateHideFlags();
        }


        private PopupField<FlowchartEditorQol> _popup;

        protected virtual void OnDisable()
        {
            ToggleSubs(false);
            LogLifecycle(nameof(OnDisable), "Entered");
            _popup = null;
        }

        /// <summary>
        /// When modifying custom editor code you can occasionally end up with orphaned editor instances.
        /// When this happens, you'll get a null exception error every time the scene serializes / deserialized.
        /// Once this situation occurs, the only way to fix it is to restart the Unity editor.
        /// As a workaround, this function detects if this editor is an orphan and deletes it. 
        /// </summary>
        protected virtual bool EraseOrphanedInstance()
        {
            try
            {
#pragma warning disable 0219
                SerializedObject so = serializedObject;
            }
            catch (System.NullReferenceException)
            {
                LogLifecycle(nameof(EraseOrphanedInstance), "NullReferenceException while reading serializedObject");
                DestroyImmediate(this);
                return true;
            }

            return false;
        }

        protected Texture2D _addTexture;

        public override VisualElement CreateInspectorGUI()
        {
            _fcTarg = target as Flowchart;
            LogLifecycle(nameof(CreateInspectorGUI), "Entered");

            #region Get Root online
            var rootElement = new VisualElement();
            var uxml = Resources.Load<VisualTreeAsset>(_pathToUxml);

            if (uxml == null)
            {
                LogLifecycle(nameof(CreateInspectorGUI), $"UXML not found at '{_pathToUxml}'");
                rootElement.Add(new HelpBox(
                    $"Flowchart inspector UXML not found at Resources path '{_pathToUxml}'.",
                    HelpBoxMessageType.Error));
                return rootElement;
            }

            var inspectorRoot = uxml.CloneTree();
            if (inspectorRoot == null)
            {
                LogLifecycle(nameof(CreateInspectorGUI), "CloneTree returned null");
                rootElement.Add(new HelpBox("Failed to build Flowchart inspector UI.", 
                    HelpBoxMessageType.Error));
                return rootElement;
            }
            #endregion

            #region Wire up the Open Flowchart Window button
            Button flowchartWindowButton = inspectorRoot.Q<Button>(_openFlowchartWindowButtonName);
            if (flowchartWindowButton == null)
            {
                string buttonName = _openFlowchartWindowButtonName;
                LogLifecycle(nameof(CreateInspectorGUI), $"Button '{buttonName}' not found");
                inspectorRoot.Add(new HelpBox($"Missing button '{buttonName}' in " +
                    $"FlowchartInspector.uxml.",
                    HelpBoxMessageType.Warning));
            }
            else
            {
                flowchartWindowButton.RegisterCallback<ClickEvent>(OpenFlowchartWindow);
                LogLifecycle(nameof(CreateInspectorGUI), "OpenFlowchartWindow button wired");
            }
            #endregion

            #region Wire up the dropdown for FlowchartEditorQol assets
            var dropdownRoot = inspectorRoot.Q<VisualElement>("EditorOnly");
            if (dropdownRoot == null)
            {
                LogLifecycle(nameof(CreateInspectorGUI), "DropdownField 'EditorOnly' not found");
                inspectorRoot.Add(new HelpBox("Missing DropdownField 'EditorOnly' in " +
                    $"FlowchartInspector.uxml.",
                    HelpBoxMessageType.Warning));
            }
            else
            {
                var allInRegistry = FlowchartEditorQolRegistry.GetAll().ToList();
                bool weHaveValidQol = _fcTarg.EditorQol != null;
                _popup = new PopupField<FlowchartEditorQol>(allInRegistry, 0, 
                    PopupFieldFormatCallback, PopupFieldFormatCallback);

                if (weHaveValidQol)
                {
                    _popup.SetValueWithoutNotify(_fcTarg.EditorQol);
                }

                _popup.RegisterValueChangedCallback(evt =>
                {
                    LogLifecycle(nameof(CreateInspectorGUI), 
                        $"Dropdown changed to {evt.newValue?.name ?? "None"}");
                    var selected = evt.newValue;
                    Undo.RecordObject(_fcTarg, "Change Flowchart Editor QOL");
                    _fcTarg.EditorQol = selected;
                    _fcTarg.UpdateHideFlags();
                    EditorUtility.SetDirty(_fcTarg);
                });

                _popup.parent?.Remove(_popup);
                dropdownRoot.Insert(0, _popup);
            }
            
            #endregion

            _saveSelection = inspectorRoot.Q<Toggle>("SaveSelection");
            _showLineNumbers = inspectorRoot.Q<Toggle>("ShowLineNumbers");
            _hideComponents = inspectorRoot.Q<Toggle>("HideComponents");
            _stepPause = inspectorRoot.Q<Slider>("StepPause");
            _commandsToHide = inspectorRoot.Q<PropertyField>("CommandsToHide");

            _hideComponents.RegisterValueChangedCallback(evt =>
            {
                _fcTarg.UpdateHideFlags();
                EditorUtility.SetDirty(_fcTarg);
            });

            RebindEditorQolFields();

            rootElement.Add(inspectorRoot);
            LogLifecycle(nameof(CreateInspectorGUI), "Returning inspector root");

            rootElement.schedule.Execute(() => rootElement.MarkDirtyRepaint()).ExecuteLater(500);
            // ^ Might compensate for a 6.3 glitch where the Flowchart and Variable
            // Manager Inspectors freak out until your restart the project. This is
            // a temporary workaround until Unity fixes the issue.

            return rootElement;
        }

        private string PopupFieldFormatCallback(FlowchartEditorQol selected)
        {
            // It's possible that we were passed a qol that's in the process
            // of being deleted, so...
            if (selected == null) 
            {
                return "None";
            }
            return selected.name;
        }
        private static readonly string _pathToUxml = "Editor/Uxml/FlowchartInspector";
        private static readonly string _openFlowchartWindowButtonName = "OpenFlowchartWindow";

        protected virtual void OpenFlowchartWindow(ClickEvent clickEvent)
        {
            LogLifecycle(nameof(OpenFlowchartWindow), "Button clicked");
            FlowchartWindow.BringUp();
        }

        private Toggle _saveSelection, _showLineNumbers, _hideComponents;
        private Slider _stepPause;
        private PropertyField _commandsToHide;
        private SerializedObject _qolSerialized;
        private void RebindEditorQolFields()
        {
            _saveSelection.Unbind();
            _showLineNumbers.Unbind();
            _hideComponents.Unbind();
            _stepPause.Unbind();
            _commandsToHide.Unbind();

            // We want those bound to the editor qol asset, not the flowchart itself.
            // The flowchart just holds a reference to the asset.

            if (_fcTarg.EditorQol != null)//
            {
                _qolSerialized = new SerializedObject(_fcTarg.EditorQol);
                _saveSelection.BindProperty(_qolSerialized.FindProperty("_saveSelection"));
                _showLineNumbers.BindProperty(_qolSerialized.FindProperty("_showLineNumbers"));
                _hideComponents.BindProperty(_qolSerialized.FindProperty("_hideComponents"));
                _stepPause.BindProperty(_qolSerialized.FindProperty("_stepPause"));
                _commandsToHide.BindProperty(_qolSerialized.FindProperty("_commandsToHide"));
            }
        }

        private void LogLifecycle(string source, string message)
        {
            if (!_debugInspectorLifecycle)
            {
                return;
            }

#pragma warning disable CS0162 // Unreachable code detected
            string targetInfo = target != null ? 
                $"{target.name} ({target.GetType().Name})" : 
                "null";
#pragma warning restore CS0162 // Unreachable code detected
            string activeObject = Selection.activeObject != null ?
                $"{Selection.activeObject.name} ({Selection.activeObject.GetType().Name})" :
                "null";

            Debug.Log($"[FlowchartEditor::{source}] {message} | target={targetInfo} | activeObject={activeObject}");
        }

    }

}
