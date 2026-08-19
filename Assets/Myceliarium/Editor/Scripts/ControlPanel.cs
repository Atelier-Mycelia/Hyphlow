using System.Linq;
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
        public static void BringUp()
        {
            if (S != null)
            {
                S.Focus();
                return;
            }
            ControlPanel wnd = GetWindow<ControlPanel>();
            wnd.titleContent = new GUIContent("ControlPanel");
            wnd.minSize = wnd.maxSize = _windowSize;
        }

        public static ControlPanel S { get; private set; }

        private static readonly Vector2 _windowSize = new Vector2(1280, 800);

        public void CreateGUI()
        {
            if (S != null && S != this)
            {
                this.Close();
            }
            if (S == this)
            {
                return; // To deal with cases where CreateGUI is called multiple times
                        // for the same instance.
            }
            S = this;

            VisualElement root = rootVisualElement;

            VisualElement baseWindow = m_VisualTreeAsset.Instantiate();
            root.Add(baseWindow);

            InitializeEntryAttacher();
        }

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        private void InitializeEntryAttacher()
        {
            _attacher?.Dispose();
            _attacher.Init(rootVisualElement);
            var toAttach = ControlPanelEntryRegistry.Entries.ToList();
            _attacher.Attach(toAttach);
        }

        private ControlPanelEntryAttacher _attacher = new ControlPanelEntryAttacher();

        protected void OnDestroy()
        {
            _attacher?.Dispose();
            _attacher = null;
        }
    }
}