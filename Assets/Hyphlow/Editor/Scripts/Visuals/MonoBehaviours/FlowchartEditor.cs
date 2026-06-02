using UnityEditor;
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
            LogLifecycle(nameof(OnEnable), $"AddSmall texture assigned? {_addTexture != null}");
        }

        protected virtual void OnDisable()
        {
            LogLifecycle(nameof(OnDisable), "Entered");
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
            LogLifecycle(nameof(CreateInspectorGUI), "Entered");

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
                rootElement.Add(new HelpBox("Failed to build Flowchart inspector UI.", HelpBoxMessageType.Error));
                return rootElement;
            }

            Button flowchartWindowButton = inspectorRoot.Q<Button>(_openFlowchartWindowButtonName);
            if (flowchartWindowButton == null)
            {
                LogLifecycle(nameof(CreateInspectorGUI), $"Button '{_openFlowchartWindowButtonName}' not found");
                inspectorRoot.Add(new HelpBox(
                    $"Missing button '{_openFlowchartWindowButtonName}' in FlowchartInspector.uxml.",
                    HelpBoxMessageType.Warning));
            }
            else
            {
                flowchartWindowButton.RegisterCallback<ClickEvent>(OpenFlowchartWindow);
                LogLifecycle(nameof(CreateInspectorGUI), "OpenFlowchartWindow button wired");
            }

            rootElement.Add(inspectorRoot);
            LogLifecycle(nameof(CreateInspectorGUI), "Returning inspector root");
            return rootElement;
        }

        private static readonly string _pathToUxml = "Editor/UIToolkitTemplates/FlowchartInspector";
        private static readonly string _openFlowchartWindowButtonName = "OpenFlowchartWindow";

        protected virtual void OpenFlowchartWindow(ClickEvent clickEvent)
        {
            LogLifecycle(nameof(OpenFlowchartWindow), "Button clicked");
            FlowchartWindow.BringUp();
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
