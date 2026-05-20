using System;
using UnityEngine;

namespace AtMycelia.Hyphlow
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

        public void SetDefaultFirstBlockEventHandlerType(Type eventHandlerType)
        {
            if (eventHandlerType == null)
            {
                _firstBlockEventHandlerTypeName = string.Empty;
                return;
            }

            bool valid = typeof(EventHandler).IsAssignableFrom(eventHandlerType);
            if (!valid)
            {
                string message =
                    $"Type {eventHandlerType.FullName} is not a valid {nameof(EventHandler)} type.";
                Debug.LogError(message, this);
                return;
            }

            _firstBlockEventHandlerTypeName = eventHandlerType.AssemblyQualifiedName;
        }

        private const string _defaultResourcesPath = "AtMycelia/Hyphlow/FcDefaultConfig";

        public static FlowchartGlobalDefaults S
        {
            get
            {
                if (_s == null)
                {
                    _s = Resources.Load<FlowchartGlobalDefaults>(_defaultResourcesPath);
                }

                return _s;
            }
            set => _s = value;
        }

        private static FlowchartGlobalDefaults _s;

        private void OnEnable()
        {
            if (S == null)
            {
                S = this;
            }
        }

        private void OnDestroy()
        {
            if (S == this)
            {
                S = null;
            }
        }
    }
}