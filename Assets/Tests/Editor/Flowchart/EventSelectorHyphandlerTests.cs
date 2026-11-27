using System.Collections.Generic;
using System.Linq;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Type = System.Type;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.FlowchartStructure
{
    /// <summary>
    /// Covers the event handler selector listing Hyphandlers next to legacy handlers, and
    /// swapping a Block between the two kinds.
    /// </summary>
    public class EventSelectorHyphandlerTests
    {
        private GameObject _fcHolder;
        private Block _block;

        private int _undoGroupBeforeTest;

        [SetUp]
        public void SetUp()
        {
            Undo.IncrementCurrentGroup();
            _undoGroupBeforeTest = Undo.GetCurrentGroup();

            _fcHolder = new GameObject("EventSelectorHyphandlerTests_Flowchart");
            Flowchart flowchart = _fcHolder.AddComponent<Flowchart>();
            _block = flowchart.CreateBlock(Vector2.zero) as Block;
        }

        [TearDown]
        public void TearDown()
        {
            // The selector records its changes with Undo (including destroying a legacy handler's
            // component). Left on the stack, undoing them later resurrects that component, along
            // with a copy of this test's GameObject in whatever scene is open.
            Undo.RevertAllDownToGroup(_undoGroupBeforeTest);

            if (_fcHolder != null)
            {
                UnityObj.DestroyImmediate(_fcHolder);
            }

            _fcHolder = null;
            _block = null;
        }

        [Test]
        public void Entries_ListTheHyphandler_AndHideTheLegacyHandlerItReplaces()
        {
            IReadOnlyList<string> paths = SelectorProbe.MenuPathsFor(typeof(GameStarted));
            IReadOnlyList<string> legacyPaths = SelectorProbe.MenuPathsFor(typeof(AtMycelia.Hyphlow.Legacy.GameStarted));

            CollectionAssert.AreEqual(new[] { "Game Started" }, paths);
            Assert.IsEmpty(legacyPaths, "Legacy GameStarted has ShowInMenu = false.");
        }

        [Test]
        public void Entries_StillListLegacyHandlersWithoutAReplacement()
        {
            CollectionAssert.IsNotEmpty(SelectorProbe.MenuPathsFor(typeof(AtMycelia.Hyphlow.Legacy.KeyPressed)));
        }

        [Test]
        public void Entries_SkipTestHandlers()
        {
            Assert.IsEmpty(SelectorProbe.MenuPathsFor(typeof(CountingHyphandler)));
            Assert.IsEmpty(SelectorProbe.MenuPathsFor(typeof(RuntimeOnlyHyphandler)));
        }

        [Test]
        public void Entries_AreSortedByOrderThenMenuPath()
        {
            IReadOnlyList<(int order, string path)> listed = SelectorProbe.OrderAndPaths();

            for (int i = 1; i < listed.Count; i++)
            {
                (int order, string path) previous = listed[i - 1];
                (int order, string path) current = listed[i];
                bool inOrder = previous.order < current.order ||
                    (previous.order == current.order && string.CompareOrdinal(previous.path, current.path) <= 0);
                Assert.IsTrue(inOrder, $"'{previous.path}' ({previous.order}) is listed before " +
                    $"'{current.path}' ({current.order}).");
            }
        }

        [Test]
        public void SelectingHyphandler_ReplacesLegacyHandlerOnTheBlock()
        {
            SelectorProbe.Select(_block, typeof(AtMycelia.Hyphlow.Legacy.GameStarted));
            Assert.IsNotNull(_fcHolder.GetComponent<AtMycelia.Hyphlow.Legacy.GameStarted>());

            SelectorProbe.Select(_block, typeof(GameStarted));

            Assert.IsInstanceOf<GameStarted>(_block.EventHandler);
            Assert.AreSame(_block, _block.EventHandler.ParentBlock);
            Assert.IsNull(_fcHolder.GetComponent<AtMycelia.Hyphlow.Legacy.GameStarted>(),
                "The legacy component should be destroyed, not left behind.");
        }

        [Test]
        public void SelectingLegacyHandler_ReplacesHyphandler()
        {
            SelectorProbe.Select(_block, typeof(GameStarted));

            SelectorProbe.Select(_block, typeof(AtMycelia.Hyphlow.Legacy.GameStarted));

            Assert.IsInstanceOf<AtMycelia.Hyphlow.Legacy.GameStarted>(_block.EventHandler);
        }

        [Test]
        public void SelectingNone_ClearsHyphandler()
        {
            SelectorProbe.Select(_block, typeof(GameStarted));

            SelectorProbe.Select(_block, null);

            Assert.IsNull(_block.EventHandler);
        }

        /// <summary>
        /// Exposes the selector's protected bits to the tests.
        /// </summary>
        private class SelectorProbe : EventSelectorPopupWindowContent
        {
            private SelectorProbe() : base("", null, 0, 0) { }

            public static IReadOnlyList<string> MenuPathsFor(Type handlerType)
            {
                return Entries
                    .Where(entry => entry.Type == handlerType)
                    .Select(entry => entry.MenuPath)
                    .ToList();
            }

            public static IReadOnlyList<(int order, string path)> OrderAndPaths()
            {
                return Entries
                    .Select(entry => (entry.Order, entry.MenuPath))
                    .ToList();
            }

            public static void Select(IBlock block, Type handlerType)
            {
                OnSelectEventHandler(new SetEventHandlerOperation
                {
                    block = block,
                    eventHandlerType = handlerType,
                });
            }
        }
    }
}
