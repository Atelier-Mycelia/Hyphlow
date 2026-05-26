using System;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.EditorExt
{
    sealed class MissingFlowchartOverlay : IDisposable
    {
        private readonly Action _refreshHandler;
        private UitkLabel _errorLabel;
        private Button _refreshButton;

        public MissingFlowchartOverlay(Action refreshHandler)
        {
            this._refreshHandler = refreshHandler ?? throw new ArgumentNullException(nameof(refreshHandler));
        }

        public void Show(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            EnsureErrorLabel();
            EnsureRefreshButton();

            if (_errorLabel.parent == null)
            {
                root.Add(_errorLabel);
            }

            if (_refreshButton.parent == null)
            {
                root.Add(_refreshButton);
            }
        }

        public void Hide()
        {
            _errorLabel?.RemoveFromHierarchy();
            _refreshButton?.RemoveFromHierarchy();
        }

        public void Dispose()
        {
            Hide();
            _errorLabel = null;
            _refreshButton = null;
        }

        private void EnsureErrorLabel()
        {
            if (_errorLabel != null)
            {
                return;
            }

            _errorLabel = new UitkLabel("No Flowcharts found in the scene. ");
            _errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _errorLabel.style.fontSize = 48;
            _errorLabel.style.color = Color.yellow;
        }

        private void EnsureRefreshButton()
        {
            if (_refreshButton != null)
            {
                return;
            }

            _refreshButton = new Button(_refreshHandler);
            _refreshButton.text = "Refresh";
            _refreshButton.style.alignSelf = Align.Center;
            Vector2 buttonSize = new Vector2(200, 50);
            _refreshButton.style.width = buttonSize.x;
            _refreshButton.style.height = buttonSize.y;
            _refreshButton.style.fontSize = 24;
        }
    }

}