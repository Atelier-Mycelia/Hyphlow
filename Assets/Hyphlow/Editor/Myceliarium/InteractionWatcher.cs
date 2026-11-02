using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Tells a subwindow's owner when the user may have changed something in controls
    /// that are bound to serialized properties. Those apply their edits themselves, so
    /// there's no edit to report; what can be known is that an interaction just ended.
    /// Waiting for that (instead of reacting to every value change) makes a slider drag
    /// or a typing session one change rather than dozens.
    /// </summary>
    internal sealed class InteractionWatcher : IDisposable
    {
        public InteractionWatcher(VisualElement root, Action onPossibleEdit)
        {
            _root = root;
            _onPossibleEdit = onPossibleEdit;
            _root.RegisterCallback<PointerUpEvent>(OnInteractionEnded);
            _root.RegisterCallback<FocusOutEvent>(OnInteractionEnded);
            _root.RegisterCallback<KeyUpEvent>(OnInteractionEnded);
            _root.RegisterCallback<SerializedPropertyChangeEvent>(OnInteractionEnded);
        }

        private readonly VisualElement _root;
        private readonly Action _onPossibleEdit;

        private void OnInteractionEnded<TEvent>(TEvent evt) where TEvent : EventBase<TEvent>, new()
        {
            // Bound fields write to their property while handling the same event
            _root.schedule.Execute(_onPossibleEdit);
        }

        public void Dispose()
        {
            _root.UnregisterCallback<PointerUpEvent>(OnInteractionEnded);
            _root.UnregisterCallback<FocusOutEvent>(OnInteractionEnded);
            _root.UnregisterCallback<KeyUpEvent>(OnInteractionEnded);
            _root.UnregisterCallback<SerializedPropertyChangeEvent>(OnInteractionEnded);
        }
    }
}
