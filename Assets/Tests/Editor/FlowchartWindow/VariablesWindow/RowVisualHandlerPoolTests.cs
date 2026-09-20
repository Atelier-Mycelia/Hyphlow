using System;
using System.Collections.Generic;
using System.Linq;
using AtMycelia;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// Unit tests for <see cref="RowVisualHandlerPool"/>, the recycling pool that hands out
    /// <see cref="IRowVisualHandler"/> instances to <see cref="VariableRowFactory"/>. Since this
    /// is pure C# logic with no IMGUI/UI Toolkit dependency, it's tested directly rather than
    /// through a host window.
    /// </summary>
    public class RowVisualHandlerPoolTests
    {
        private FakeRowVisualHandlerResolver _resolver;
        private IDictionary<Type, Type> _lookup;
        private RowVisualHandlerPool _pool;

        [SetUp]
        public void SetUp()
        {
            FakeResettableHandler.InstancesCreated = 0;
            FakeDisposableOnlyHandler.InstancesCreated = 0;

            _lookup = new Dictionary<Type, Type>
            {
                { typeof(int), typeof(FakeResettableHandler) },
                { typeof(string), typeof(FakeDisposableOnlyHandler) },
            };

            _resolver = new FakeRowVisualHandlerResolver(_lookup);
            _pool = new RowVisualHandlerPool(_resolver, _lookup);
        }

        [Test]
        public void GetHandlerFor_WhenPoolEmpty_CreatesNewInstance()
        {
            var variable = new IntMuscariable { Key = "i", Value = 1 };

            var handler = _pool.GetHandlerFor(typeof(int), variable);

            Assert.IsNotNull(handler);
            Assert.IsInstanceOf<FakeResettableHandler>(handler);
            Assert.AreEqual(1, FakeResettableHandler.InstancesCreated,
                "Should have created exactly one new handler instance.");
        }

        [Test]
        public void GetHandlerFor_CallsInitWithProvidedVariable()
        {
            var variable = new IntMuscariable { Key = "i", Value = 42 };

            var handler = (FakeResettableHandler)_pool.GetHandlerFor(typeof(int), variable);

            Assert.AreSame(variable, handler.Variable);
        }

        [Test]
        public void Release_ThenGetHandlerFor_ReusesPooledInstance()
        {
            var firstVar = new IntMuscariable { Key = "i1", Value = 1 };
            var handler = _pool.GetHandlerFor(typeof(int), firstVar);

            _pool.Release(handler);
            Assert.AreEqual(1, _pool.PooledHandlerCount, "Handler should be sitting in the pool after release.");

            var secondVar = new IntMuscariable { Key = "i2", Value = 2 };
            var reused = _pool.GetHandlerFor(typeof(int), secondVar);

            Assert.AreSame(handler, reused, "Should reuse the pooled instance instead of creating a new one.");
            Assert.AreEqual(1, FakeResettableHandler.InstancesCreated,
                "Only one instance should have ever been created.");
            Assert.AreEqual(0, _pool.PooledHandlerCount, "Pool should be empty again after handing the instance back out.");
        }

        [Test]
        public void Release_WhenHandlerIsResettable_CallsResetInsteadOfDispose()
        {
            var variable = new IntMuscariable { Key = "i", Value = 1 };
            var handler = (FakeResettableHandler)_pool.GetHandlerFor(typeof(int), variable);

            _pool.Release(handler);

            Assert.IsTrue(handler.ResetCalled, "IResettable.Reset should be invoked on release.");
            Assert.IsFalse(handler.DisposeCalled, "Dispose should not be called for resettable handlers.");
        }

        [Test]
        public void Release_WhenHandlerIsNotResettable_CallsDispose()
        {
            var variable = new StringMuscariable { Key = "s", Value = "a" };
            var handler = (FakeDisposableOnlyHandler)_pool.GetHandlerFor(typeof(string), variable);

            _pool.Release(handler);

            Assert.IsTrue(handler.DisposeCalled, "Dispose should be invoked for non-resettable handlers.");
            Assert.AreEqual(0, _pool.PooledHandlerCount,
                "Non-resettable handlers are disposed and should not be tracked as pooled.");
        }

        [Test]
        public void Release_SameHandlerTwice_OnlyPushedOnce()
        {
            var variable = new IntMuscariable { Key = "i", Value = 1 };
            var handler = _pool.GetHandlerFor(typeof(int), variable);

            _pool.Release(handler);
            _pool.Release(handler);

            Assert.AreEqual(1, _pool.PooledHandlerCount,
                "Releasing the same handler instance twice should not double-count it in the pool.");
        }

        [Test]
        public void ReleaseRange_ReleasesEveryHandlerProvided()
        {
            var firstHandler = _pool.GetHandlerFor(typeof(int), new IntMuscariable { Key = "i1", Value = 1 });
            var secondHandler = _pool.GetHandlerFor(typeof(int), new IntMuscariable { Key = "i2", Value = 2 });

            _pool.ReleaseRange(new[] { firstHandler, secondHandler });

            Assert.AreEqual(2, _pool.PooledHandlerCount, "Both handlers should have been released into the pool.");
        }

        [Test]
        public void ReleaseIn_ReleasesVisualHandlerOfEachRow()
        {
            var firstHandler = (FakeResettableHandler)_pool.GetHandlerFor(typeof(int), new IntMuscariable { Key = "i1", Value = 1 });
            var secondHandler = (FakeResettableHandler)_pool.GetHandlerFor(typeof(int), new IntMuscariable { Key = "i2", Value = 2 });

            var firstRow = new VariableRow();
            firstRow.Init(new IntMuscariable { Key = "i1", Value = 1 }, firstHandler);
            var secondRow = new VariableRow();
            secondRow.Init(new IntMuscariable { Key = "i2", Value = 2 }, secondHandler);

            _pool.ReleaseIn(new[] { firstRow, secondRow });

            Assert.IsTrue(firstHandler.ResetCalled);
            Assert.IsTrue(secondHandler.ResetCalled);
            Assert.AreEqual(2, _pool.PooledHandlerCount);
        }

        [Test]
        public void Clear_EmptiesAllPooledStacksWithoutRemovingKeys()
        {
            var handler = _pool.GetHandlerFor(typeof(int), new IntMuscariable { Key = "i", Value = 1 });
            _pool.Release(handler);
            Assert.AreEqual(1, _pool.PooledHandlerCount);

            _pool.Clear();

            Assert.AreEqual(0, _pool.PooledHandlerCount, "Clear should empty all pooled handler stacks.");
        }

        [Test]
        public void PooledHandlerCount_SumsAcrossMultipleHandlerTypes()
        {
            var intHandler = _pool.GetHandlerFor(typeof(int), new IntMuscariable { Key = "i", Value = 1 });
            var stringHandler = _pool.GetHandlerFor(typeof(string), new StringMuscariable { Key = "s", Value = "a" });

            _pool.Release(intHandler); // resettable -> pooled
            _pool.Release(stringHandler); // disposable-only -> not pooled

            Assert.AreEqual(1, _pool.PooledHandlerCount,
                "Only the resettable handler should count toward the pooled total.");
        }

        [Test]
        public void GetHandlerFor_UsesResolverToPickHandlerType()
        {
            var variable = new IntMuscariable { Key = "i", Value = 1 };

            _pool.GetHandlerFor(typeof(int), variable);

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

        private class FakeResettableHandler : IRowVisualHandler, IResettable
        {
            public static int InstancesCreated;

            public FakeResettableHandler()
            {
                InstancesCreated++;
            }

            public bool ResetCalled;
            public bool DisposeCalled;

            public IVariable Variable { get; set; }
            public UnityEngine.UIElements.VisualElement RowRoot => null;
            public UnityEngine.UIElements.VisualTreeAsset Template => null;
            public Type VarContentType => typeof(int);

            public void Init(IVariable variable) => Variable = variable;
            public void Refresh() { }
            public void Reset() => ResetCalled = true;
            public void Dispose() => DisposeCalled = true;

            public event Action<IRowVisualHandler> RemoveButtonClicked = delegate { };
            public event Action<UnityEngine.UIElements.TextField> KeyFieldChanged = delegate { };
            public event Action<AccessScope> ScopeFieldChanged = delegate { };
            public event Action<object> ValueFieldChanged = delegate { };
        }

        private class FakeDisposableOnlyHandler : IRowVisualHandler
        {
            public static int InstancesCreated;

            public FakeDisposableOnlyHandler()
            {
                InstancesCreated++;
            }

            public bool DisposeCalled;

            public IVariable Variable { get; set; }
            public UnityEngine.UIElements.VisualElement RowRoot => null;
            public UnityEngine.UIElements.VisualTreeAsset Template => null;
            public Type VarContentType => typeof(string);

            public void Init(IVariable variable) => Variable = variable;
            public void Refresh() { }
            public void Dispose() => DisposeCalled = true;

            public event Action<IRowVisualHandler> RemoveButtonClicked = delegate { };
            public event Action<UnityEngine.UIElements.TextField> KeyFieldChanged = delegate { };
            public event Action<AccessScope> ScopeFieldChanged = delegate { };
            public event Action<object> ValueFieldChanged = delegate { };
        }
    }
}
