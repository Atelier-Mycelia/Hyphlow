using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using System.Linq;

namespace AtMycelia.Hyphlow.EditorExt
{
    public static class RowVisualHandlerRegistry
    {
        [InitializeOnLoadMethod]
        private static void InitializeHandlerLookup()
        {
            // Always rebuild lookup right now
            RefreshHandlerLookup(); 

            // Ensure we only subscribe once
            AssemblyReloadEvents.afterAssemblyReload -= RefreshHandlerLookup;
            AssemblyReloadEvents.afterAssemblyReload += RefreshHandlerLookup;
        }

        private static readonly object _handlerLookupLock = new object();

        public static void RefreshHandlerLookup()
        {
            // 1) Snapshot types using Unity's cached type database (avoids scanning all assemblies)
            var discovered = TypeCache.GetTypesDerivedFrom<RowVisualHandler>()
                .Where(typeEl =>
                    typeEl.IsConcrete() &&
                    typeEl.GetCustomAttribute<RowVisualHandlerAttribute>() != null)
                .ToArray(); // snapshot

            // 2) Precompute the pairs outside the lock
            var pairs = new List<KeyValuePair<Type, Type>>(discovered.Length);
            foreach (var handlerType in discovered)
            {
                var attr = handlerType.GetCustomAttribute<RowVisualHandlerAttribute>();
                var newPair = new KeyValuePair<Type, Type>(attr.ContentType, handlerType);
                pairs.Add(newPair);
            }

            // 3) Atomic update under the lock (keep same dictionary instance)
            lock (_handlerLookupLock)
            {
                _visualHandlerLookup.Clear();
                foreach (var kv in pairs)
                {
                    _visualHandlerLookup[kv.Key] = kv.Value;
                }
            }
        }

        public static IDictionary<Type, Type> VisualHandlerLookup
        {
            get {  return new Dictionary<Type, Type>(_visualHandlerLookup); }
        }

        // Keys are var content types (like for floats, ints, etc), values are the types
        // of the visual handlers meant for the corresponding keys.
        private static readonly IDictionary<Type, Type> _visualHandlerLookup = 
            new Dictionary<Type, Type>(new TypeNameComparer());

    }
}