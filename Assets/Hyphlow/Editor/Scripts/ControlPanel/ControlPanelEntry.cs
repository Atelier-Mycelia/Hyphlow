using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// These handle the logic for their own entries in the appropriate Control 
    /// Panel, represented by a tab on the left sidebar and its associated 
    /// subwindow in the appropriate holder.
    /// 
    /// These should be automatically found through reflection and added to the 
    /// CP when appropriate.
    /// </summary>
    public abstract class ControlPanelEntry : IControlPanelEntry, IDisposable
    {
        public abstract string MainDisplayName { get; }

        public virtual void Init(bool forceReinit = false)
        {
            PrepareTabButton();
            PrepareSubwindow();
            ToggleSubs(true);
        }

        protected virtual void PrepareTabButton()
        {
            if (_tabButton == null)
            {
                var visualTree = Resources.Load<VisualTreeAsset>(PathToTabButtonUXML);
                bool loadFailed = visualTree == null;
                if (loadFailed)
                {
                    string logMessage = $"Failed to load VisualTreeAsset at " +
                        $"{PathToTabButtonUXML} for {GetType().Name}.";
                    throw new InvalidOperationException(logMessage);
                }
                _tabButton = visualTree.CloneTree();
            }
        }

        protected VisualElement _tabButton;

        protected abstract string PathToTabButtonUXML { get; }

        protected virtual void PrepareSubwindow()
        {
            if (_subwindow == null)
            {
                var visualTree = Resources.Load<VisualTreeAsset>(PathToSubwindowUXML);
                bool loadFailed = visualTree == null;
                if (loadFailed)
                {
                    string logMessage = $"Failed to load VisualTreeAsset at " +
                        $"{PathToSubwindowUXML} for {GetType().Name}.";
                    throw new InvalidOperationException(logMessage);
                }
                _subwindow = visualTree.CloneTree();
            }
        }

        protected VisualElement _subwindow;
        protected abstract string PathToSubwindowUXML { get; }

        protected abstract void ToggleSubs(bool on);

        /// <summary>
        /// Meant to be overridden by subclasses that have state that needs
        /// to be stringified for saving/loading purposes.
        /// </summary>
        public abstract string StringifiedState { get; }

        public virtual void Apply(string stringifiedState, out bool success)
        {
            success = false;
        }

        // Clients shouldn't even try to access the Subwindow or tab before
        // the Init call, hence why the getters here throw exceptions if the
        // subwindow or tab button is null. This is to help catch bugs in the code.
        public virtual VisualElement TabButton
        {
            get
            {
                if (_tabButton == null)
                {
                    string logMessage = $"TabButton for {GetType().Name} was null. " +
                        $"This should not happen if Init() has been called.";
                    throw new InvalidOperationException(logMessage);
                }
                return _tabButton;
            }
            protected set => _tabButton = value;
        }

        public virtual VisualElement Subwindow
        {
            get
            {
                if (_subwindow == null)
                {
                    string logMessage = $"Subwindow for {GetType().Name} was null. " +
                        $"This should not happen if Init() has been called.";
                    throw new InvalidOperationException(logMessage);
                }
                return _subwindow;
            }
            protected set => _subwindow = value;
        }

        public virtual void Dispose()
        {
            // No-op by default. Subclasses can override this to clean up
            // any resources they may have allocated.
        }
    }

    public interface IControlPanelEntry
    {
        /// <summary>
        /// Functions as the constructor for this entry. Should be called once when the 
        /// entry is first created, and can be called again if the entry needs to 
        /// be reinitialized.
        /// </summary>
        void Init(bool forceReinit = false);

        /// <summary>
        /// The display name of this entry in English. This is used to help keep things
        /// consistently sorted in the tab sidebar on the left side of the Control Panel.
        /// </summary>
        string MainDisplayName { get; }

        VisualElement TabButton { get; }
        VisualElement Subwindow { get; }

        /// <summary>
        /// Some entries may have state that needs to be stringified for saving/loading purposes.
        /// This method returns a string representation of the entry's state.
        /// </summary>
        /// <returns></returns>
        string StringifiedState { get; }

        void Apply(string stringifiedState, out bool success);
    }


}