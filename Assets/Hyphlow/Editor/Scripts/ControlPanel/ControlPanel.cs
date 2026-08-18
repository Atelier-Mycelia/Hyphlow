using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Serves as a hub for the Database-esque UI that other plugins may want to set up.
    /// </summary>
    public class ControlPanel : EditorWindow
    {
        [MenuItem("Window/Atelier Mycelia/ControlPanel")]
        public static void ShowExample()
        {
            ControlPanel wnd = GetWindow<ControlPanel>();
            wnd.titleContent = new GUIContent("ControlPanel");
            wnd.minSize = wnd.maxSize = _windowSize;
        }

        private static readonly Vector2 _windowSize = new Vector2(1280, 800);

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            VisualElement baseWindow = m_VisualTreeAsset.Instantiate();
            root.Add(baseWindow);

            InitializeEntryAttacher();
        }

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private void InitializeEntryAttacher()
        {
            _entryAttacher?.Dispose();

            _entryAttacher = new ControlPanelEntryAttacher(rootVisualElement);
            _entryAttacher.AttachAllEntries();
        }

        private ControlPanelEntryAttacher _entryAttacher;

        protected void OnDestroy()
        {
            _entryAttacher?.Dispose();
            _entryAttacher = null;
        }
    }
}