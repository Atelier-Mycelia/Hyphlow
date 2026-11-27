using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Plain C# successor to the MonoBehaviour <see cref="EventHandler"/>. Lives inside its
    /// Block through [SerializeReference] instead of being a component of its own, which keeps
    /// scenes smaller and skips the MonoBehaviour overhead.
    ///
    /// Since it isn't a component, Unity doesn't drive it. Its host Block is expected to:
    /// <list type="bullet">
    /// <item>assign <see cref="ParentBlock"/> after deserializing (it isn't serialized, which
    /// avoids a reference from the handler back to its own container)</item>
    /// <item>call <see cref="Enable"/> / <see cref="Disable"/> from its own OnEnable / OnDisable</item>
    /// <item>call <see cref="Validate"/> from its own OnValidate (editor only)</item>
    /// </list>
    ///
    /// To make one: subclass this, mark the subclass [Serializable] (that attribute isn't
    /// inherited), add a <see cref="HyphandlerInfoAttribute"/>, subscribe to whatever event you
    /// respond to in <see cref="SetSubs"/>, and call <see cref="ExecuteBlock"/> when it fires.
    /// Handlers that relied on Unity messages (OnMouseDown, Update...) need a notifier component
    /// to subscribe to instead, like the physics handlers' PhysicsEventNotifier.
    ///
    /// [SerializeReference] stores the full type name, so a renamed or moved subclass needs
    /// [MovedFrom] or existing data will fail to load.
    /// </summary>
    [Serializable]
    public abstract class Hyphandler : IEventHandler
    {
        [Tooltip("If true, the flowchart window will not auto select the Block when the Event " +
            "Handler fires. Affects Editor only.")]
        [SerializeField] protected bool _suppressBlockAutoSelect = false;

        #region Ownership

        /// <summary>
        /// The Block that owns this handler. Assigned by that Block; not serialized. The Block sets
        /// it during deserialization, so overrides mustn't touch the Unity API.
        /// </summary>
        public virtual IBlock ParentBlock
        {
            get { return _parentBlock; }
            set { _parentBlock = value; }
        }

        [NonSerialized] protected IBlock _parentBlock;

        protected Flowchart ParentFlowchart => _parentBlock?.ParentFlowchart;

        /// <summary>
        /// The GameObject the handler effectively sits on, for subclasses that need one to
        /// watch or attach notifiers to by default.
        /// </summary>
        protected GameObject HostGameObject
        {
            get
            {
                Flowchart flowchart = ParentFlowchart;
                return flowchart != null ? 
                flowchart.gameObject : 
                null;
            }
        }

        #endregion

        #region Lifecycle (driven by the host Block)

        /// <summary>
        /// Whether the handler is currently subscribed to its events.
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Subscribes to the handler's events if it's allowed to right now. Safe to call
        /// more than once.
        /// </summary>
        public virtual void Enable()
        {
            if (IsEnabled || !CanSubscribe)
            {
                return;
            }

            SetSubs(true);
            IsEnabled = true;
        }

        private bool CanSubscribe
        {
            get
            {
                if (_parentBlock == null || !IsInTheScene)
                {
                    return false;
                }

                return !SetSubsOnlyInRuntime || Application.isPlaying;
            }
        }

        // Prefab assets have no valid scene, and Prefab Mode puts prefabs in a preview scene;
        // neither should react to anything. Unsaved scenes (which have no name) still count.
        private bool IsInTheScene
        {
            get
            {
                GameObject host = HostGameObject;
                if (host == null || !host.scene.IsValid())
                {
                    return false;
                }

#if UNITY_EDITOR
                if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(host.scene))
                {
                    return false;
                }
#endif
                return true;
            }
        }

        /// <summary>
        /// Some handlers would rather not react to anything in edit mode, so they only subscribe
        /// while playing. Override to return false for ones that should work in edit mode too.
        /// </summary>
        protected virtual bool SetSubsOnlyInRuntime => true;

        /// <summary>
        /// Unsubscribes from the handler's events. Safe to call more than once.
        /// </summary>
        public virtual void Disable()
        {
            if (!IsEnabled)
            {
                return;
            }

            SetSubs(false);
            IsEnabled = false;
        }

        /// <summary>
        /// Subscribe to (or unsubscribe from) whatever this handler responds to.
        /// </summary>
        protected virtual void SetSubs(bool on)
        {

        }

        /// <summary>
        /// Editor-only sanity checks (clamping values and such). Called from the host's OnValidate.
        /// </summary>
        public virtual void Validate()
        {

        }

        #endregion

        #region Execution

        /// <summary>
        /// Subclasses call this when their event occurs.
        /// </summary>
        public virtual bool ExecuteBlock()
        {
            if (_parentBlock == null)
            {
                return false;
            }

            Flowchart flowchart = ParentFlowchart;
            // If somehow the flowchart is invalid or has been disabled we don't want to continue
            if (flowchart == null || !flowchart.isActiveAndEnabled)
            {
                string logMessage = $"{GetType().Name} attempted to execute its Block, " +
                    "but the Flowchart was not valid.";
                Debug.LogWarning(logMessage, flowchart);
                return false;
            }

            if (_suppressBlockAutoSelect)
            {
                _parentBlock.SuppressNextAutoSelection = true;
            }

            flowchart.ExecuteBlock(_parentBlock);
            return true;
        }

        /// <summary>
        /// Runs a coroutine on the parent Flowchart, since a POCO can't run its own. It stops
        /// when the Flowchart is disabled. Returns null when there's no active Flowchart to run on.
        /// </summary>
        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            Flowchart flowchart = ParentFlowchart;
            if (flowchart == null || !flowchart.isActiveAndEnabled)
            {
                return null;
            }

            return flowchart.StartCoroutine(routine);
        }

        protected void StopCoroutine(Coroutine coroutine)
        {
            Flowchart flowchart = ParentFlowchart;
            if (flowchart != null && coroutine != null)
            {
                flowchart.StopCoroutine(coroutine);
            }
        }

        #endregion

        #region Display

        /// <summary>
        /// Custom summary text for the handler, shown in the BlockWindow's Trigger section.
        /// </summary>
        public virtual string GetSummary()
        {
            return "";
        }

        public virtual string DisplayNameAboveBlock
        {
            get
            {
                HyphandlerInfoAttribute info = GetType().GetCustomAttribute<HyphandlerInfoAttribute>();
                return info != null ?
                    info.HandlerName :
                    GetType().Name;
            }
        }

        #endregion
    }
}
