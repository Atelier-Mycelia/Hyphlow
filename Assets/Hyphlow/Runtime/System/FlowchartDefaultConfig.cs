using System;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    [CreateAssetMenu(
        fileName = "FcDefaultConfig",
        menuName = "Atelier Mycelia/Hyphlow/Flowchart Default Config")]
    public sealed class FlowchartDefaultConfig : ScriptableObject
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

        public static FlowchartDefaultConfig S
        {
            get
            {
                if (_s == null)
                {
                    _s = Resources.Load<FlowchartDefaultConfig>(_defaultResourcesPath);
                }

                return _s;
            }
            set => _s = value;
        }

        private static FlowchartDefaultConfig _s;

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