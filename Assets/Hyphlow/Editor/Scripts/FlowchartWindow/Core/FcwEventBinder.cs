using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{
    internal sealed class FcwEventBinder
    {
        private readonly FcwModuleHost _moduleHost;
        private readonly Action<Flowchart, Flowchart> _onSelectedFlowchartChanged;
        private readonly EditorSceneManager.SceneOpenedCallback _onSceneOpened;
        private readonly EditorSceneManager.SceneClosedCallback _onSceneClosed;
        private readonly UnityAction<Scene, LoadSceneMode> _onSceneLoaded;
        private readonly Action<PlayModeStateChange> _onPlayModeStateChanged;
        private readonly Action<float> _onZoomChanged;

        public FcwEventBinder(FcwModuleHost moduleHost,
            Action<Flowchart, Flowchart> onSelectedFlowchartChanged,
            EditorSceneManager.SceneOpenedCallback onSceneOpened,
            EditorSceneManager.SceneClosedCallback onSceneClosed,
            UnityAction<Scene, LoadSceneMode> onSceneLoaded,
            Action<PlayModeStateChange> onPlayModeStateChanged,
            Action<float> onZoomChanged)
        {
            _moduleHost = moduleHost;
            _onSelectedFlowchartChanged = onSelectedFlowchartChanged;
            _onSceneOpened = onSceneOpened;
            _onSceneClosed = onSceneClosed;
            _onSceneLoaded = onSceneLoaded;
            _onPlayModeStateChanged = onPlayModeStateChanged;
            _onZoomChanged = onZoomChanged;
        }

        public void Toggle(bool on)
        {
            _moduleHost.ToggleDispatcherSubs(on);

            if (on)
            {
                EditorSelectionTracker.SelectedFlowchartChanged += _onSelectedFlowchartChanged;
                FlowchartWindowSignals.ChangedFlowchart += _moduleHost.ModuleDispatcher.NotifyFlowchartChanged;
                FlowchartWindowSignals.WindowPanned += _moduleHost.ModuleDispatcher.NotifyWindowPanned;

                EditorSceneManager.sceneOpened += _onSceneOpened;
                EditorSceneManager.sceneClosed += _onSceneClosed;
                EditorSceneManager.sceneLoaded += _onSceneLoaded;

                VariableSignals.PostValueChange += OnVarValueChanged;
                VariableSignals.VariableAdded += OnVarAddedOrRemoved;
                VariableSignals.VariableRemoved += OnVarAddedOrRemoved;

                EditorApplication.playModeStateChanged += _onPlayModeStateChanged;
                CommandSignals.CommandSelected += _moduleHost.ModuleDispatcher.NotifyCommandSelected;
                FlowchartWindowSignals.ZoomChanged += _onZoomChanged;
            }
            else
            {
                EditorSelectionTracker.SelectedFlowchartChanged -= _onSelectedFlowchartChanged;
                FlowchartWindowSignals.ChangedFlowchart -= _moduleHost.ModuleDispatcher.NotifyFlowchartChanged;
                FlowchartWindowSignals.WindowPanned -= _moduleHost.ModuleDispatcher.NotifyWindowPanned;
                EditorSceneManager.sceneOpened -= _onSceneOpened;
                EditorSceneManager.sceneClosed -= _onSceneClosed;
                EditorSceneManager.sceneLoaded -= _onSceneLoaded;

                VariableSignals.PostValueChange -= OnVarValueChanged;
                VariableSignals.VariableAdded -= OnVarAddedOrRemoved;
                VariableSignals.VariableRemoved -= OnVarAddedOrRemoved;

                EditorApplication.playModeStateChanged -= _onPlayModeStateChanged;
                CommandSignals.CommandSelected -= _moduleHost.ModuleDispatcher.NotifyCommandSelected;
                FlowchartWindowSignals.ZoomChanged -= _onZoomChanged;
            }
        }

        private void OnVarAddedOrRemoved(IVariable variable)
        {
            if (FlowchartWindow.S != null)
            {
                FlowchartWindow.S.Refresh();
            }
        }

        private void OnVarValueChanged(IVariable variable, object arg2)
        {
            // Unit tests might make the FlowchartWindow null, so we need to check for that.
            if (FlowchartWindow.S != null)
            {
                FlowchartWindow.S.Refresh();
            }
            else
            {
                string warningMessage = $"[Hyphlow] Variable value changed, but {nameof(FlowchartWindow)} is" +
                    $"null. This likely means that the change was made during an editor test. If you are a" +
                    $"user and see this message, please report it to the developers.";
                Debug.LogWarning(warningMessage);
            }
        }
    }
}