using System.Collections.Generic;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// Exercises the bind/unbind/pool lifecycle the ListView drives, without needing a panel
    /// (which is why the old pooling tests ended up asserting that nothing happened).
    /// </summary>
    public class VariableRowBinderTests
    {
        private RowVisualHandlerPool _pool;
        private VariableRowBinder _binder;
        private RecordingHost _host;

        [SetUp]
        public void SetUp()
        {
            _pool = new RowVisualHandlerPool(new RowVisualHandlerResolver(),
                RowVisualHandlerRegistry.VisualHandlerLookup);
            _binder = new VariableRowBinder(_pool);
            _host = new RecordingHost();
            _binder.Host = _host;
        }

        [TearDown]
        public void TearDown()
        {
            _binder.Dispose();
        }

        [Test]
        public void Bind_FillsShellWithMatchingHandlerVisuals()
        {
            var shell = _binder.CreateShell();
            var floatVar = new FloatMuscariable { Key = "f", Value = 2f };

            _binder.Bind(shell, floatVar);

            var handler = VariableRowBinder.HandlerOf(shell);
            Assert.IsInstanceOf<FloatRowVisualHandler>(handler);
            Assert.AreSame(floatVar, handler.Variable);
            Assert.AreSame(shell, handler.RowRoot.parent);
            Assert.AreEqual(1, _binder.BoundCount);
        }

        [Test]
        public void Bind_SameTypeAgain_ReusesHandlerAndVisuals()
        {
            var shell = _binder.CreateShell();
            _binder.Bind(shell, new FloatMuscariable { Key = "a", Value = 1f });
            var handler = VariableRowBinder.HandlerOf(shell);
            var root = handler.RowRoot;

            var second = new FloatMuscariable { Key = "b", Value = 2f };
            _binder.Bind(shell, second);

            Assert.AreSame(handler, VariableRowBinder.HandlerOf(shell));
            Assert.AreSame(root, handler.RowRoot, "Visuals should not be rebuilt on rebind.");
            Assert.AreSame(second, handler.Variable);
            Assert.AreEqual(1, _binder.BoundCount);
        }

        [Test]
        public void Bind_DifferentType_SwapsHandlerThroughPool()
        {
            var shell = _binder.CreateShell();
            _binder.Bind(shell, new FloatMuscariable { Key = "a", Value = 1f });
            var floatHandler = VariableRowBinder.HandlerOf(shell);

            _binder.Bind(shell, new StringMuscariable { Key = "s", Value = "x" });

            Assert.IsInstanceOf<StringRowVisualHandler>(VariableRowBinder.HandlerOf(shell));
            Assert.AreEqual(1, _pool.PooledHandlerCount, "The float handler should be waiting in the pool.");
            Assert.IsNull(floatHandler.Variable);
            Assert.IsNull(floatHandler.RowRoot.parent);
            Assert.AreEqual(1, _binder.BoundCount);
        }

        [Test]
        public void Unbind_KeepsHandlerInShell_ButStopsTrackingVariable()
        {
            var shell = _binder.CreateShell();
            var floatVar = new FloatMuscariable { Key = "a", Value = 1f };
            _binder.Bind(shell, floatVar);
            var handler = VariableRowBinder.HandlerOf(shell);

            _binder.Unbind(shell);

            Assert.AreSame(handler, VariableRowBinder.HandlerOf(shell));
            Assert.IsNull(handler.Variable);
            Assert.IsFalse(_binder.TryGetHandler(floatVar, out _));
            Assert.AreEqual(0, _pool.PooledHandlerCount);
        }

        [Test]
        public void Destroy_ReturnsHandlerToPool_AndReuseHappensOnNextShell()
        {
            var shell = _binder.CreateShell();
            _binder.Bind(shell, new IntMuscariable { Key = "i", Value = 1 });
            var handler = VariableRowBinder.HandlerOf(shell);

            _binder.Destroy(shell);

            Assert.IsNull(VariableRowBinder.HandlerOf(shell));
            Assert.AreEqual(1, _pool.PooledHandlerCount);

            var otherShell = _binder.CreateShell();
            _binder.Bind(otherShell, new IntMuscariable { Key = "j", Value = 2 });
            Assert.AreSame(handler, VariableRowBinder.HandlerOf(otherShell));
            Assert.AreEqual(0, _pool.PooledHandlerCount);
        }

        [Test]
        public void TryGetHandler_UsesReferenceEquality()
        {
            var firstShell = _binder.CreateShell();
            var secondShell = _binder.CreateShell();
            var first = new FloatMuscariable { Key = "same", Value = 1f };
            var second = new FloatMuscariable { Key = "same", Value = 1f };
            _binder.Bind(firstShell, first);
            _binder.Bind(secondShell, second);

            Assert.IsTrue(_binder.TryGetHandler(second, out var handler));
            Assert.AreSame(VariableRowBinder.HandlerOf(secondShell), handler);
        }

        [Test]
        public void Host_ReceivesEditsFromHandlers_ThroughTheBinder()
        {
            var floatVar = new FloatMuscariable { Key = "f", Value = 1f };

            _binder.KeyEdited(floatVar, "newKey");
            _binder.ScopeEdited(floatVar, AccessScope.Public);
            _binder.ValueEdited(floatVar, 5f);
            _binder.RemoveRequested(floatVar);

            CollectionAssert.AreEqual(new[] { "key:newKey", "scope:Public", "value:5", "remove" }, _host.Calls);
        }

        [Test]
        public void Dispose_UnbindsEverythingAndClearsPool()
        {
            var shell = _binder.CreateShell();
            _binder.Bind(shell, new FloatMuscariable { Key = "f", Value = 1f });
            var spare = _binder.CreateShell();
            _binder.Bind(spare, new IntMuscariable { Key = "i", Value = 1 });
            _binder.Destroy(spare);

            _binder.Dispose();

            Assert.AreEqual(0, _binder.BoundCount);
            Assert.AreEqual(0, _pool.PooledHandlerCount);
        }

        private class RecordingHost : IRowEditHost
        {
            public readonly List<string> Calls = new List<string>();

            public void KeyEdited(IVariable variable, string newKey) => Calls.Add($"key:{newKey}");
            public void ScopeEdited(IVariable variable, AccessScope newScope) => Calls.Add($"scope:{newScope}");
            public void ValueEdited(IVariable variable, object newValue) => Calls.Add($"value:{newValue}");
            public void RemoveRequested(IVariable variable) => Calls.Add("remove");
        }
    }
}
