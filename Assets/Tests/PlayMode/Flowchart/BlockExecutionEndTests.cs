using System.Collections;
using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.FlowchartLifecycle
{
    /// <summary>
    /// Covers how a Block's execution ends: the end events firing once, and the onComplete
    /// callback (what Call with WaitUntilFinished waits on) firing however the Block stops.
    /// </summary>
    public class BlockExecutionEndTests
    {
        private GameObject _go;
        private Flowchart _flowchart;
        private Block _block;

        private int _execEndedCount, _endSignalCount, _onCompleteCount;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("BlockExecutionEndTests_Flowchart");
            _flowchart = _go.AddComponent<Flowchart>();
            _block = _flowchart.CreateBlock(Vector2.zero) as Block;
            _block.BlockName = "TestBlock";

            _execEndedCount = 0;
            _endSignalCount = 0;
            _onCompleteCount = 0;
            _block.ExecEnded += OnExecEnded;
            BlockSignals.BlockExecEnded += OnEndSignal;
        }

        private void OnExecEnded(IBlock block)
        {
            _execEndedCount++;
        }

        private void OnEndSignal(IBlock block)
        {
            if (ReferenceEquals(block, _block))
            {
                _endSignalCount++;
            }
        }

        private void OnComplete()
        {
            _onCompleteCount++;
        }

        [TearDown]
        public void TearDown()
        {
            BlockSignals.BlockExecEnded -= OnEndSignal;
            if (_block != null && _block.IsExecuting)
            {
                _block.Stop();
            }

            UnityObj.DestroyImmediate(_go);
            _go = null;
            _flowchart = null;
            _block = null;
            Flowchart.ResetStaticsForTest();
        }

        private T AddCommand<T>() where T : Command
        {
            T cmd = _go.AddComponent<T>();
            _block.Add(cmd, triggerSignals: false);
            return cmd;
        }

        [UnityTest]
        public IEnumerator NaturalCompletion_RaisesEndEventsOnce()
        {
            AddCommand<ContinueTestCommand>();
            AddCommand<ContinueTestCommand>();

            _flowchart.ExecuteBlock(_block);
            yield return null;

            Assert.IsFalse(_block.IsExecuting);
            Assert.AreEqual(1, _execEndedCount, "Block.ExecEnded");
            Assert.AreEqual(1, _endSignalCount, "BlockSignals.BlockExecEnded");
        }

        [UnityTest]
        public IEnumerator NaturalCompletion_InvokesOnCompleteOnce()
        {
            AddCommand<ContinueTestCommand>();

            _flowchart.ExecuteBlock(_block, 0, OnComplete);
            yield return null;
            yield return null;

            Assert.AreEqual(1, _onCompleteCount);
        }

        [UnityTest]
        public IEnumerator CommandStoppingItsOwnBlock_InvokesOnComplete()
        {
            // Like a Stop command, or an If with nothing after it. This used to leave a
            // Call set to WaitUntilFinished waiting forever.
            AddCommand<StopsOwnBlockTestCommand>();
            AddCommand<ContinueTestCommand>();

            _flowchart.ExecuteBlock(_block, 0, OnComplete);
            yield return null;
            yield return null;

            Assert.IsFalse(_block.IsExecuting);
            Assert.AreEqual(1, _onCompleteCount);
            Assert.AreEqual(1, _execEndedCount);
            Assert.AreEqual(1, _endSignalCount);
        }

        [UnityTest]
        public IEnumerator BlockStopWhileWaiting_InvokesOnCompleteOnce()
        {
            AddCommand<WaitsForeverTestCommand>();

            _flowchart.ExecuteBlock(_block, 0, OnComplete);
            yield return null;
            Assert.IsTrue(_block.IsExecuting, "Precondition: the Block should be waiting.");
            Assert.AreEqual(0, _onCompleteCount, "Precondition: nothing should have completed yet.");

            _block.Stop();
            yield return null;
            yield return null;

            Assert.IsFalse(_block.IsExecuting);
            Assert.AreEqual(1, _onCompleteCount);
            Assert.AreEqual(1, _execEndedCount);
        }

        [UnityTest]
        public IEnumerator FlowchartStopBlockWhileWaiting_InvokesOnCompleteOnce()
        {
            AddCommand<WaitsForeverTestCommand>();

            _flowchart.ExecuteBlock(_block, 0, OnComplete);
            yield return null;

            _flowchart.StopBlock(_block.BlockName);
            yield return null;
            yield return null;

            Assert.IsFalse(_block.IsExecuting);
            Assert.AreEqual(1, _onCompleteCount);
            Assert.AreEqual(1, _execEndedCount);
            Assert.AreEqual(1, _endSignalCount);
        }

        [UnityTest]
        public IEnumerator OnCompleteRestartingSameBlock_LeavesNewRunIntact()
        {
            // The first run's cleanup used to happen after onComplete, wiping out the
            // state of the run that onComplete had just started
            AddCommand<ContinueTestCommand>();
            WaitsOnSecondRunTestCommand waiter = AddCommand<WaitsOnSecondRunTestCommand>();

            bool restarted = false;
            void RestartOnce()
            {
                if (restarted)
                {
                    return;
                }

                restarted = true;
                _flowchart.ExecuteBlock(_block);
            }

            _flowchart.ExecuteBlock(_block, 0, RestartOnce);
            yield return null;

            Assert.IsTrue(restarted, "Precondition: the first run should have completed.");
            Assert.IsTrue(_block.IsExecuting, "The second run should still be going.");
            Assert.AreSame(waiter, _block.ActiveCommand);
            Assert.AreEqual(2, _block.ExecutionCount);
        }
    }

    [AddComponentMenu("")]
    public class ContinueTestCommand : Command
    {
        public override void OnEnter()
        {
            Continue();
        }
    }

    [AddComponentMenu("")]
    public class WaitsForeverTestCommand : Command
    {
        public override void OnEnter()
        {
        }
    }

    [AddComponentMenu("")]
    public class StopsOwnBlockTestCommand : Command
    {
        public override void OnEnter()
        {
            ParentBlock.Stop();
        }
    }

    [AddComponentMenu("")]
    public class WaitsOnSecondRunTestCommand : Command
    {
        private int _timesEntered;

        public override void OnEnter()
        {
            _timesEntered++;
            if (_timesEntered == 1)
            {
                Continue();
            }
        }
    }
}
