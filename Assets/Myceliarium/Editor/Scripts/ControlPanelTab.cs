using System;
using UnityEngine;
using UnityEngine.UIElements;
using UitkButton = UnityEngine.UIElements.Button;

namespace AtMycelia.Myceliarium
{
    public abstract class ControlPanelTab : IControlPanelTab, IDisposable
    {
        public abstract string DisplayName { get; }
        public abstract string PathToUxml { get; }

        public virtual void Init()
        {
            PrepRoot();
            RegisterVisualElements();
            ToggleSubs(true);
        }

        protected virtual void PrepRoot()
        {
            var vta = Resources.Load<VisualTreeAsset>(PathToUxml);
            if (vta == null)
            {
                throw new InvalidOperationException(
                    $"Failed to load tab UXML at {PathToUxml} for {GetType().Name}");
            }

            Root = vta.CloneTree();
        }

        public VisualElement Root { get; protected set; }

        protected virtual void RegisterVisualElements()
        {
            _button = Root.Q<UitkButton>();
            if (_button == null)
            {
                string logMessage = $"Failed to find a Button in the tab UXML " +
                    $"at {PathToUxml} for {GetType().Name}.";
                throw new InvalidOperationException(logMessage);
            }
        }

        private UitkButton _button;

        public virtual string Text
        {
            get => _button.text;
            set => _button.text = value;
        }

        protected virtual void ToggleSubs(bool on)
        {
            if (on)
            {
                _button.clicked += InvokeClicked;
            }
            else
            {
                _button.clicked -= InvokeClicked;
            }
        }

        public virtual void InvokeClicked()
        {
            OnClicked();
        }

        protected virtual void OnClicked()
        {
            IsSelected = true;
            Clicked.Invoke(this);
        }

        public event Action<IControlPanelTab> Clicked = delegate { };

        public virtual void Dispose()
        {
            if (Root != null)
            {
                ToggleSubs(false);
                Clicked = delegate { };
                Root.RemoveFromHierarchy();
                Root = null;
                _button = null;
            }
        }

        public virtual bool IsSelected
        {
            get => _button?.ClassListContains("tab-selected") ?? false;
            set
            {
                if (_button == null)
                {
                    throw new InvalidOperationException(
                        $"Cannot set IsSelected for {GetType().Name} because the button is null.");
                }
                if (value)
                {
                    _button.AddToClassList("selected");
                }
                else
                {
                    _button.RemoveFromClassList("selected");
                }
            }
        }
    }

    public interface IControlPanelTab
    {
        VisualElement Root { get; }

        void Init();

        /// <summary>
        /// The one defining the tab layout. This should be relative to Resources.
        /// </summary>
        string PathToUxml { get; }

        string DisplayName { get; }

        event Action<IControlPanelTab> Clicked;
        void InvokeClicked();

        string Text { get; set; }
        bool IsSelected { get; set; }
    }
}

