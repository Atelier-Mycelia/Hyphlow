using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// The only pool in the variables panel: handlers (and the visuals they own) are recycled
    /// per handler type.
    /// </summary>
    public class RowVisualHandlerPool
    {
        public RowVisualHandlerPool
            (
            IRowVisualHandlerResolver handlerResolver,
            IDictionary<Type, Type> visualHandlerLookup
            )
        {
            _handlerResolver = handlerResolver;
            _visualHandlerLookup = visualHandlerLookup;
        }

        protected readonly IRowVisualHandlerResolver _handlerResolver;

        // Again: the keys are the types the vars hold (string, int, etc) while the values
        // are the types of the visual handlers
        protected readonly IDictionary<Type, Type> _visualHandlerLookup;

        public virtual Type ResolveHandlerType(Type contentType)
        {
            return _handlerResolver.ResolveHandler(_visualHandlerLookup, contentType);
        }

        /// <summary>
        /// Hands out an unbound handler of the given type, recycled if one is available.
        /// </summary>
        public virtual IRowVisualHandler Take(Type handlerType)
        {
            bool recyclableInstanceAvailable = _poolMap.TryGetValue(handlerType, out var stack) &&
                stack.Count > 0;
            if (recyclableInstanceAvailable)
            {
                IRowVisualHandler recycled = stack.Pop();
                _pooled.Remove(recycled);
                return recycled;
            }

            return (IRowVisualHandler)Activator.CreateInstance(handlerType);
        }

        /// <summary>
        /// Unbinds the handler, detaches its visuals and keeps it for reuse.
        /// </summary>
        public virtual void Return(IRowVisualHandler handler)
        {
            if (handler == null || !_pooled.Add(handler))
            {
                return;
            }

            handler.Unbind();
            handler.RowRoot?.RemoveFromHierarchy();

            Type handlerType = handler.GetType();
            if (!_poolMap.TryGetValue(handlerType, out var stack))
            {
                stack = new Stack<IRowVisualHandler>();
                _poolMap[handlerType] = stack;
            }

            stack.Push(handler);
        }

        protected readonly Dictionary<Type, Stack<IRowVisualHandler>> _poolMap
            = new Dictionary<Type, Stack<IRowVisualHandler>>();

        // Guards against double-returns without paying for Stack.Contains
        private readonly HashSet<IRowVisualHandler> _pooled = new HashSet<IRowVisualHandler>();

        public int PooledHandlerCount => _pooled.Count;
        public Dictionary<Type, Stack<IRowVisualHandler>> PoolMap => _poolMap;

        /// <summary>
        /// Disposes every pooled handler and forgets them.
        /// </summary>
        public virtual void Clear()
        {
            foreach (IRowVisualHandler handler in _pooled)
            {
                handler.Dispose();
            }

            _pooled.Clear();
            foreach (var stack in _poolMap.Values)
            {
                stack.Clear();
            }
        }

        [Conditional("DEV_DIAGNOSTICS")]
        public void DumpState()
        {
            foreach (var kvp in _poolMap)
                Debug.Log($"{kvp.Key.Name}: {kvp.Value.Count} in pool");
        }
    }

}
