using UnityEngine;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// In charge of handling the essential MonoBehaviours and GameObjects for the Hyphlow system,
    /// such as TweenManager.
    /// </summary>
    [ExecuteInEditMode]
    public sealed class HyphlowManager : MonoBehaviour
    {
        private void Awake()
        {
            if (_s != null && _s != this)
            {
                string warningMessage = $"Another instance of HyphlowManager " +
                    $"already exists: {_s.gameObject.name} Destroying this one: " +
                    $"{this.gameObject.name}";
                Debug.LogWarning(warningMessage);
                if (Application.isPlaying)
                {
                    Destroy(this.gameObject);
                }
                else
                {
                    DestroyImmediate(this.gameObject);
                }

                return;
            }

            _s = this;
            EventDispatcher = this.gameObject.GetOrAddComponent<EventDispatcher>();
        }

        public static HyphlowManager S
        {
            get => _s;
            private set => _s = value;
        }
        private static HyphlowManager _s;

        public EventDispatcher EventDispatcher { get; private set; }

        private void OnDestroy()
        {
            if (_s == this)
            {
                _s = null;
            }
        }
    }
}