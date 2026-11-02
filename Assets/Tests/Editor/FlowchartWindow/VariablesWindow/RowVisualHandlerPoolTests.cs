using System;
using System.Collections.Generic;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// Unit tests for <see cref="RowVisualHandlerPool"/>, the only pool in the variables panel.
    /// It's pure C# logic with no UI Toolkit dependency, so it's tested directly with fakes.
    /// </summary>
    public class RowVisualHandlerPoolTests
    {
        private FakeRowVisualHandlerResolver _resolver;
        private IDictionary<Type, Type> _lookup;
        private RowVisualHandlerPool _pool;

        [SetUp]
        public void SetUp()
        {
            FakeHandler.InstancesCreated = 0;

            _lookup = new Dictionary<Type, Type>
            {
                { typeof(int), typeof(FakeHandler) },
                { typeof(string), typeof(OtherFakeHandler) },
            };

            _resolver = new FakeRowVisualHandlerResolver(_lookup);
            _pool = new RowVisualHandlerPool(_resolver, _lookup);
        }

        [Test]
        public void Take_WhenPoolEmpty_CreatesNewInstance()
        {
            var handler = _pool.Take(typeof(FakeHandler));

            Assert.IsInstanceOf<FakeHandler>(handler);
            Assert.AreEqual(1, FakeHandler.InstancesCreated);
        }

        [Test]
        public void Return_ThenTake_ReusesPooledInstance()
        {
            var handler = _pool.Take(typeof(FakeHandler));

            _pool.Return(handler);
            Assert.AreEqual(1, _pool.PooledHandlerCount, "Handler should be sitting in the pool after release.");

            var reused = _pool.Take(typeof(FakeHandler));

            Assert.AreSame(handler, reused, "Should reuse the pooled instance instead of creating a new one.");
            Assert.AreEqual(1, FakeHandler.InstancesCreated);
            Assert.AreEqual(0, _pool.PooledHandlerCount);
        }

        [Test]
        public void Return_UnbindsHandler_AndKeepsItAlive()
        {
            var handler = (FakeHandler)_pool.Take(typeof(FakeHandler));

            _pool.Return(handler);

            Assert.IsTrue(handler.UnbindCalled, "Returned handlers should stop reacting to their variable.");
            Assert.IsFalse(handler.DisposeCalled, "Pooled handlers keep their visuals for reuse.");
        }

        [Test]
        public void Return_SameHandlerTwice_OnlyPooledOnce()
        {
            var handler = _pool.Take(typeof(FakeHandler));

            _pool.Return(handler);
            _pool.Return(handler);

            Assert.AreEqual(1, _pool.PooledHandlerCount);
        }

        [Test]
        public void Return_Null_IsIgnored()
        {
            Assert.DoesNotThrow(() => _pool.Return(null));
            Assert.AreEqual(0, _pool.PooledHandlerCount);
        }

        [Test]
        public void Pools_AreSeparatePerHandlerType()
        {
            var first = _pool.Take(typeof(FakeHandler));
            var second = _pool.Take(typeof(OtherFakeHandler));

            _pool.Return(first);
            _pool.Return(second);

            Assert.AreEqual(2, _pool.PooledHandlerCount);
            Assert.AreEqual(1, _pool.PoolMap[typeof(FakeHandler)].Count);
            Assert.AreEqual(1, _pool.PoolMap[typeof(OtherFakeHandler)].Count);
            Assert.AreSame(second, _pool.Take(typeof(OtherFakeHandler)));
        }

        [Test]
        public void Clear_DisposesPooledHandlers()
        {
            var handler = (FakeHandler)_pool.Take(typeof(FakeHandler));
            _pool.Return(handler);

            _pool.Clear();

            Assert.AreEqual(0, _pool.PooledHandlerCount);
            Assert.IsTrue(handler.DisposeCalled);
        }

        [Test]
        public void ResolveHandlerType_UsesResolver()
        {
            Type resolved = _pool.ResolveHandlerType(typeof(int));

            Assert.AreEqual(typeof(FakeHandler), resolved);
            Assert.AreEqual(1, _resolver.ResolveCallCount);
            Assert.AreEqual(typeof(int), _resolver.LastContentTypeRequested);
        }

        // --- Fakes for testing ---

        private class FakeRowVisualHandlerResolver : IRowVisualHandlerResolver
        {
            private readonly IDictionary<Type, Type> _fixedLookup;

            public FakeRowVisualHandlerResolver(IDictionary<Type, Type> fixedLookup)
            {
                _fixedLookup = fixedLookup;
            }

            public int ResolveCallCount { get; private set; }
            public Type LastContentTypeRequested { get; private set; }

            public Type ResolveHandler(IDictionary<Type, Type> visualHandlerLookup, Type contentType)
            {
                ResolveCallCount++;
                LastContentTypeRequested = contentType;
                return _fixedLookup[contentType];
            }
        }

        private class FakeHandler : IRowVisualHandler
        {
            public static int InstancesCreated;

            public FakeHandler()
            {
                InstancesCreated++;
            }

            public bool UnbindCalled;
            public bool DisposeCalled;

            public IVariable Variable { get; private set; }
            public VisualElement RowRoot { get; } = new VisualElement();
            public virtual Type VarContentType => typeof(int);

            public void Bind(IVariable variable, IRowEditHost host) => Variable = variable;
            public void Unbind()
            {
                UnbindCalled = true;
                Variable = null;
            }
            public void RefreshValue() { }
            public void Dispose() => DisposeCalled = true;
        }

        private class OtherFakeHandler : FakeHandler
        {
            public override Type VarContentType => typeof(string);
        }
    }
}
