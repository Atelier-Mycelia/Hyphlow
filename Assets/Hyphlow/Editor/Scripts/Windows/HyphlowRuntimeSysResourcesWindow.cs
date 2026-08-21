using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;
using AtMycelia.Hyphlow.Sys;
using AtMycelia.HyphaTween;

namespace AtMycelia.Hyphlow.EditorExt
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
            
            _assets = HyphlowRuntimeSysAssets.S;
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
                rootVisualElement.Add(new HelpBox("HyphlowRuntimeSysAssets asset not found.", 
                    HelpBoxMessageType.Error));
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

            content.Add(_tweenAdapterField);
            content.Add(new UitkLabel("Variable Registry Configs"));
            content.Add(_addVariableRegistryConfigButton);

            rootVisualElement.Add(content);

            RefreshFields();
        }

        private ObjectField _tweenAdapterField;
        private Button _addVariableRegistryConfigButton;

        private void RefreshFields()
        {
            if (_assets == null)
            {
                return;
            }

        }

    }
}