using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    public sealed class FcwVariablesPanel : IFlowchartWindowModule, IFlowchartChangeResponder
    {
        public int Priority { get; set; } = 0;

        public void Initialize(FlowchartWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            _window = window;
            _isDisposed = false;

            _panelRoot = ResolveRootElement();
            if (_panelRoot == null)
            {
                Debug.LogError("[FcWindowVariablesPanelUitk] Could not find VarDisplay instance in UXML.");
                return;
            }

            BuildManager(_window.Flowchart);
            SetSubs(true);
            _panelRoot.BringToFront();
        }

        private FlowchartWindow _window;
        private bool _isDisposed;
        private VisualElement _panelRoot;

        private VisualElement ResolveRootElement()
        {
            return _window.rootVisualElement.Q<VisualElement>("VarDisplay");
        }

        private void BuildManager(Flowchart flowchart)
        {
            if (_isDisposed || _panelRoot == null || flowchart == null)
            {
                return;
            }

            _manager?.Dispose();
            _manager = VariableRowManager.CreateFor(_panelRoot, flowchart);
        }

        private VariableRowManager _manager;

        private void SetSubs(bool on)
        {
            if (on)
            {
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
            else
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                Flowchart fc = _window != null ? 
                    _window.Flowchart : 
                    null;
                BuildManager(fc);
            }
        }

        public void OnFlowchartChanged(Flowchart oldFc, Flowchart newFc)
        {
            BuildManager(newFc);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            SetSubs(false);

            _manager?.Dispose();
            _manager = null;

            _window = null;
            _panelRoot = null;
        }
    }
}