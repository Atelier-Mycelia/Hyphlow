using System.Collections;
using System.Collections.Generic;
using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.FlowchartLifecycle
{
    /// <summary>
    /// Covers the Hyphandler version of GameStarted firing its Block through the Flowchart's
    /// game-start path (BlockExecutionManagerComponent.Start).
    /// </summary>
    public class GameStartedHyphandlerTests
    {
        private GameObject _fcHolder;
        private Flowchart _flowchart;
        private Block _block;
        private int _execStartedCount;

        [SetUp]
        public void SetUp()
        {
            FlowchartRegistry.EnsureInitialized(true);
            _execStartedCount = 0;
        }

        /// <summary>
        /// Call from the test body, in the same frame the handler gets assigned. SetUp runs a frame
        /// earlier, so a Flowchart made there would already have started (and found no handler).
        /// </summary>
        private void CreateFlowchart()
        {
            _fcHolder = new GameObject("GameStartedHyphandlerTests_Flowchart");
            _flowchart = _fcHolder.AddComponent<Flowchart>();
            _flowchart.AlwaysKeepGuid = false;
            _block = _flowchart.CreateBlock(Vector2.zero) as Block;

            // A Block with no Commands refuses to execute, so give it something harmless to run
            Label label = _fcHolder.AddComponent<Label>();
            _block.Add(label, triggerSignals: false);

            _block.ExecStarted += OnExecStarted;
        }

        private void OnExecStarted(IBlock block)
        {
            _execStartedCount++;
        }

        [TearDown]
        public void TearDown()
        {
            if (_block != null)
            {
                _block.ExecStarted -= OnExecStarted;
            }

            var toDestroy = new List<UnityObj>();
            // Might've been created by the Flowchart during the test
            EventSystem evSys = UnityObj.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (evSys != null)
            {
                toDestroy.Add(evSys.gameObject);
            }

            if (_flowchart != null)
            {
                _flowchart.OnTearDown();
            }

            toDestroy.Add(_fcHolder);
            foreach (UnityObj obj in toDestroy)
            {
                if (obj != null)
                {
                    UnityObj.DestroyImmediate(obj);
                }
            }

            _fcHolder = null;
            _flowchart = null;
            _block = null;
        }

        [UnityTest]
        public IEnumerator GameStartedHyphandler_ExecutesItsBlockOnceWhenTheFlowchartStarts()
        {
            CreateFlowchart();
            var handler = new GameStarted();
            _block.EventHandler = handler;
            Assert.IsTrue(handler.IsEnabled, "Should subscribe while playing.");

            yield return WaitForStartup();

            Assert.AreEqual(1, _execStartedCount);
        }

        [UnityTest]
        public IEnumerator GameStartedHyphandler_OnDisabledBlock_DoesNotExecute()
        {
            CreateFlowchart();
            _block.EventHandler = new GameStarted();
            _block.enabled = false;

            yield return WaitForStartup();

            Assert.AreEqual(0, _execStartedCount);
        }

        // Start runs on the next frame, then the handler waits its frames before executing
        private static IEnumerator WaitForStartup()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }
        }
    }
}
