using System;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// For settings that should apply globally to all Flowcharts in a project,
    /// such as default Block size and default Block names.
    /// </summary>
    public sealed class FlowchartGlobalDefaults : ScriptableObject
    {
        [SerializeField]
        private AccessScope _newBlockScope = AccessScope.Private;

        [SerializeField]
        [Tooltip("For the first Block that a Flowchart spawns with.")]
        private string _firstBlockName = "Init";

        [SerializeField]
        [Tooltip("For Blocks other than the first that gets added to a Flowchart.")]
        private string _newBlockName = "New Block";

        [SerializeField]
        private Vector2 _blockSize = new Vector2(300f, 100f);

        [Range(0f, 5f)]
        [SerializeField]
        private float _stepPause = 0f;

        [SerializeField]
        private string _firstBlockEventHandlerTypeName = "";

        public AccessScope NewBlockScope
        {
            get => _newBlockScope;
        }

        public string FirstBlockName
        {
            get => _firstBlockName;
        }

        public string NewBlockName
        {
            get => _newBlockName;
        }

        public Vector2 BlockSize
        {
            get => _blockSize;
        }

        public float StepPause
        {
            get => _stepPause;
        }

        public string FirstBlockEventHandlerTypeName
        {
            get => _firstBlockEventHandlerTypeName;
        }

        public Type FirstBlockEventHandlerType
        {
            get
            {
                if (string.IsNullOrEmpty(_firstBlockEventHandlerTypeName))
                {
                    return null;
                }

                return Type.GetType(_firstBlockEventHandlerTypeName);
            }
        }

        private const string _defaultResourcesPath = "AtMycelia/Hyphlow/FcDefaultConfig";

        public static FlowchartGlobalDefaults S
        {
            get
            {
                if (_s == null)
                {
                    _s = Resources.Load<FlowchartGlobalDefaults>(_defaultResourcesPath);

                    if (_s == null)
                    {
                        _s = CreateRuntimeFallback();
                    }
                }

                return _s;
            }
            set => _s = value;
        }

        private static FlowchartGlobalDefaults CreateRuntimeFallback()
        {
            FlowchartGlobalDefaults fallback = CreateInstance<FlowchartGlobalDefaults>();
            fallback.name = nameof(FlowchartGlobalDefaults) + " (RuntimeFallback)";
            Debug.LogWarning($"Could not load {nameof(FlowchartGlobalDefaults)} at Resources/{_defaultResourcesPath}. Using runtime fallback defaults.");
            return fallback;
        }

        private static FlowchartGlobalDefaults _s;

        private void OnEnable()
        {
            if (_s == null || !_s || _s.name.EndsWith("(RuntimeFallback)"))
            {
                _s = this;
            }
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(_s, this))
            {
                _s = null;
            }
        }
    }
}