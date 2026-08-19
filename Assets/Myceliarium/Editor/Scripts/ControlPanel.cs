using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Serves as a hub for the Database-esque UI that other plugins may want to set up.
    /// </summary>
    public abstract class ControlPanel : EditorWindow
    {
        /// <summary>
        /// The path to the uxml for the control panel's root window. This is relative
        /// to Resources.
        /// </summary>
        protected abstract string PathToUxml { get; }

        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        protected virtual void SetTitleContent()
        {
            titleContent = new GUIContent("ControlPanel");
        }

        protected virtual void SetWindowSizeBounds()
        {
            minSize = maxSize = DefaultWindowSize;
        }

        protected virtual Vector2 DefaultWindowSize => new Vector2(1280, 800);

        public void CreateGUI()
        {
            PreRootPrep(out bool success);
            if (!success)
            {
                Debug.LogError("ControlPanel failed to initialize. Aborting GUI creation.");
                this.Close();
                return;
            }

            PrepRoot();
            
        }

        /// <summary>
        /// By default, this func has success set to true. When overriding this, you
        /// might want it to be false if some critical initialization fails, so that
        /// the window doesn't open in a broken state.
        /// </summary>
        protected virtual void PreRootPrep(out bool success)
        {
            SetTitleContent();
            SetWindowSizeBounds();
            success = true;
        }

        protected virtual void PrepRoot()
        {
            VisualElement root = rootVisualElement;
            var vTreeAsset = Resources.Load<VisualTreeAsset>(PathToUxml);
            bool loaded = vTreeAsset != null;
            if (!loaded)
            {
                Debug.LogError($"Failed to load uxml at path {PathToUxml}. " +
                    $"Please ensure the path is correct and the file exists.");
                return;
            }
            VisualElement baseWindow = vTreeAsset.Instantiate();
            root.Add(baseWindow);
            GetEntriesAttached();
        }

        private void GetEntriesAttached()
        {
            _attacher?.Dispose();
            _attacher.Init(rootVisualElement);

            var toAttach = GetEntriesToAttach().ToList();
            Sort(toAttach);
            _attacher.Attach(toAttach);
        }

        private ControlPanelEntryAttacher _attacher = new ControlPanelEntryAttacher();

        protected abstract IEnumerable<IControlPanelEntry> GetEntriesToAttach();

        /// <summary>
        /// Default implementation does nothing. Subclasses can override to
        /// provide sorting logic for the entries before they are
        /// attached to the control panel.
        /// </summary>
        /// <param name="entries"></param>
        protected virtual void Sort(IEnumerable<IControlPanelEntry> entries)
        {
        }

        protected void OnDestroy()
        {
            _attacher?.Dispose();
            _attacher = null;
        }
    }
}