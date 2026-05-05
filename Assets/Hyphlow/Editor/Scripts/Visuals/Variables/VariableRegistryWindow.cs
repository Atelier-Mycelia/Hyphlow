using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorUtils
{
    /// <summary>
    /// Editor window for viewing and managing the variable registry configuration, including global variable sources.
    /// </summary>
    public sealed class VariableRegistryWindow : EditorWindow
    {
        private SerializedObject _serializedConfig;
        private SerializedProperty _globalSourcesProperty;
        private IReadOnlyList<VariableRegistryConfig> _configs;
        private string[] _configLabels = new string[0];
        private int _selectedConfigIndex;

        [MenuItem("Window/Atelier Mycelia/Hyphlow/Variable Registry")]
        public static void BringUp()
        {
            VariableRegistryWindow wnd = GetWindow<VariableRegistryWindow>();
            wnd.titleContent = new GUIContent("Variable Registry");
            wnd.minSize = MinSize;
            wnd.Show();
        }

        private static readonly Vector2 MinSize = new Vector2(320f, 240f);

        private void OnEnable()
        {
            LoadConfigs();
            RefreshSelectedConfig();
        }

        private void LoadConfigs()
        {
            _configs = DefaultAssetMaintenance.EnsureVariableRegistryConfigs();
            _configLabels = BuildConfigLabels(_configs);
            if (_configLabels.Length == 0)
            {
                _selectedConfigIndex = 0;
                return;
            }

            _selectedConfigIndex = Mathf.Clamp(_selectedConfigIndex, 0, _configLabels.Length - 1);
        }

        private string[] BuildConfigLabels(IReadOnlyList<VariableRegistryConfig> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                return new string[0];
            }

            string[] labels = new string[configs.Count];
            for (int i = 0; i < configs.Count; i++)
            {
                VariableRegistryConfig config = configs[i];
                labels[i] = config != null ? config.name : $"Missing Config {i + 1}";
            }

            return labels;
        }

        private void RefreshSelectedConfig()
        {
            if (_configs == null || _configs.Count == 0 ||
                _selectedConfigIndex < 0 || _selectedConfigIndex >= _configs.Count)
            {
                _serializedConfig = null;
                _globalSourcesProperty = null;
                return;
            }

            VariableRegistryConfig config = _configs[_selectedConfigIndex];
            if (config == null)
            {
                _serializedConfig = null;
                _globalSourcesProperty = null;
                return;
            }

            _serializedConfig = new SerializedObject(config);
            _globalSourcesProperty = _serializedConfig.FindProperty("_globalSources")
                ?? _serializedConfig.FindProperty("globalSources");
        }

        private void OnGUI()
        {
            if (_configs == null || _configs.Count == 0)
            {
                EditorGUILayout.HelpBox("VariableRegistryConfig asset not found.", MessageType.Error);
                if (GUILayout.Button("Create Config"))
                {
                    OnEnable();
                }
                return;
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Registry Config", _selectedConfigIndex, _configLabels);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedConfigIndex = newIndex;
                RefreshSelectedConfig();
            }

            if (_serializedConfig == null || _globalSourcesProperty == null)
            {
                EditorGUILayout.HelpBox("Selected VariableRegistryConfig is missing.", MessageType.Error);
                if (GUILayout.Button("Reload Configs"))
                {
                    OnEnable();
                }
                return;
            }

            _serializedConfig.Update();

            EditorGUILayout.PropertyField(_globalSourcesProperty, new GUIContent("Global Sources"), true);

            _serializedConfig.ApplyModifiedProperties();

            if (GUILayout.Button("Rebuild Registry"))
            {
                VariableRegistryService.EnsureDefault().Rebuild();
            }
        }
    }
}