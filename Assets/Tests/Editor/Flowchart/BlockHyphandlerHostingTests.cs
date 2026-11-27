using System;
using System.Text.RegularExpressions;
using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.FlowchartStructure
{
    /// <summary>
    /// Covers a legacy (MonoBehaviour) Block hosting a POCO Hyphandler: ownership, keeping it
    /// exclusive with legacy handlers, and keeping its subscriptions in step with the Block.
    /// </summary>
    public class BlockHyphandlerHostingTests
    {
        private GameObject _fcHolder;
        private Flowchart _flowchart;
        private Block _block;

        [SetUp]
        public void SetUp()
        {
            CountingHyphandler.ActiveSubscriptions = 0;

            _fcHolder = new GameObject("BlockHyphandlerHostingTests_Flowchart");
            _flowchart = _fcHolder.AddComponent<Flowchart>();
            _block = _flowchart.CreateBlock(Vector2.zero) as Block;
            _block.BlockName = "TestBlock";
        }

        [TearDown]
        public void TearDown()
        {
            if (_fcHolder != null)
            {
                UnityObj.DestroyImmediate(_fcHolder);
            }

            _fcHolder = null;
            _flowchart = null;
            _block = null;
        }

        private CountingHyphandler AssignCountingHyphandler()
        {
            var handler = new CountingHyphandler();
            _block.EventHandler = handler;
            return handler;
        }

        #region Ownership and exclusivity

        [Test]
        public void AssigningHyphandler_ExposesItAndSetsItsParent()
        {
            CountingHyphandler handler = AssignCountingHyphandler();

            Assert.AreSame(handler, _block.EventHandler);
            Assert.AreSame(_block, handler.ParentBlock);
        }

        [Test]
        public void AssigningHyphandler_ClearsLegacyHandler()
        {
            KeyPressed legacy = _fcHolder.AddComponent<KeyPressed>();
            _block.EventHandler = legacy;

            CountingHyphandler handler = AssignCountingHyphandler();

            Assert.AreSame(handler, _block.EventHandler);
            Assert.IsFalse(_block.EventHandler is AtMycelia.Hyphlow.EventHandler);
        }

        [Test]
        public void AssigningLegacyHandler_ClearsAndUnsubscribesHyphandler()
        {
            CountingHyphandler handler = AssignCountingHyphandler();
            KeyPressed legacy = _fcHolder.AddComponent<KeyPressed>();

            _block.EventHandler = legacy;

            Assert.AreSame(legacy, _block.EventHandler);
            Assert.IsFalse(handler.IsEnabled);
            Assert.AreEqual(0, CountingHyphandler.ActiveSubscriptions);
            Assert.IsNull(handler.ParentBlock);
        }

        [Test]
        public void AssigningNull_UnsubscribesAndOrphansHyphandler()
        {
            CountingHyphandler handler = AssignCountingHyphandler();

            _block.EventHandler = null;

            Assert.IsNull(_block.EventHandler);
            Assert.IsFalse(handler.IsEnabled);
            Assert.AreEqual(0, CountingHyphandler.ActiveSubscriptions);
            Assert.IsNull(handler.ParentBlock);
        }

        [Test]
        public void AssigningUnsupportedHandlerType_IsRejected()
        {
            CountingHyphandler handler = AssignCountingHyphandler();
            LogAssert.Expect(LogType.Error, new Regex("can't hold an Event Handler of type"));

            _block.EventHandler = new UnsupportedEventHandler();

            Assert.AreSame(handler, _block.EventHandler);
        }

        #endregion

        #region Subscriptions

        [Test]
        public void Subscription_FollowsBlockEnabledState()
        {
            CountingHyphandler handler = AssignCountingHyphandler();
            Assert.IsTrue(handler.IsEnabled, "Should subscribe as soon as an enabled Block takes it.");

            _block.enabled = false;
            Assert.IsFalse(handler.IsEnabled);
            Assert.AreEqual(0, CountingHyphandler.ActiveSubscriptions);

            _block.enabled = true;
            Assert.IsTrue(handler.IsEnabled);
            Assert.AreEqual(1, CountingHyphandler.ActiveSubscriptions);
        }

        [Test]
        public void EnableAndDisable_AreSafeToRepeat()
        {
            CountingHyphandler handler = AssignCountingHyphandler();

            handler.Enable();
            handler.Enable();
            Assert.AreEqual(1, handler.SubscribeCalls);

            handler.Disable();
            handler.Disable();
            Assert.AreEqual(1, handler.UnsubscribeCalls);
            Assert.AreEqual(0, CountingHyphandler.ActiveSubscriptions);
        }

        [Test]
        public void RuntimeOnlyHyphandler_DoesNotSubscribeInEditMode()
        {
            var handler = new RuntimeOnlyHyphandler();

            _block.EventHandler = handler;

            Assert.AreSame(_block, handler.ParentBlock);
            Assert.IsFalse(handler.IsEnabled);
            Assert.AreEqual(0, handler.SubscribeCalls);
        }

        [Test]
        public void HyphandlerWithoutBlock_DoesNotSubscribe()
        {
            var handler = new CountingHyphandler();

            handler.Enable();

            Assert.IsFalse(handler.IsEnabled);
            Assert.AreEqual(0, handler.SubscribeCalls);
        }

        #endregion

        #region Deserialization

        [Test]
        public void DuplicatingFlowchart_GivesTheCloneItsOwnSubscribedHyphandler()
        {
            CountingHyphandler original = AssignCountingHyphandler();
            GameObject cloneHolder = UnityObj.Instantiate(_fcHolder);
            try
            {
                Block cloneBlock = cloneHolder.GetComponent<Block>();
                var cloneHandler = cloneBlock.EventHandler as CountingHyphandler;

                Assert.IsNotNull(cloneHandler);
                Assert.AreNotSame(original, cloneHandler);
                Assert.AreSame(cloneBlock, cloneHandler.ParentBlock);
                Assert.AreSame(_block, original.ParentBlock, "The original must keep its own parent.");
                Assert.IsTrue(cloneHandler.IsEnabled);
                Assert.AreEqual(2, CountingHyphandler.ActiveSubscriptions);
            }
            finally
            {
                UnityObj.DestroyImmediate(cloneHolder);
            }
        }

        [Test]
        public void InPlaceDeserialization_LeavesTheCurrentInstanceAsTheOnlySubscriber()
        {
            AssignCountingHyphandler();
            RunPendingDelayCalls();

            // An inspector-style edit; Unity may hand the Block a fresh Hyphandler instance for it
            var serializedBlock = new SerializedObject(_block);
            SerializedProperty prop = serializedBlock.FindProperty("_hyphandler._suppressBlockAutoSelect");
            Assert.IsNotNull(prop);
            prop.boolValue = true;
            serializedBlock.ApplyModifiedProperties();
            RunPendingDelayCalls();

            var current = _block.EventHandler as CountingHyphandler;
            Assert.IsNotNull(current);
            Assert.AreSame(_block, current.ParentBlock);
            Assert.IsTrue(current.IsEnabled);
            Assert.AreEqual(1, CountingHyphandler.ActiveSubscriptions);
        }

        // delayCall doesn't fire in EditMode tests while the editor is unfocused, so flush it
        // the way Unity does
        private static void RunPendingDelayCalls()
        {
            EditorApplication.CallbackFunction pending = EditorApplication.delayCall;
            EditorApplication.delayCall = null;
            pending?.Invoke();
        }

        #endregion

        #region Execution guards

        [Test]
        public void ExecuteBlock_WithoutParent_ReturnsFalse()
        {
            var handler = new CountingHyphandler();

            Assert.IsFalse(handler.ExecuteBlock());
        }

        [Test]
        public void ExecuteBlock_WithDisabledFlowchart_ReturnsFalseAndWarns()
        {
            CountingHyphandler handler = AssignCountingHyphandler();
            _flowchart.enabled = false;
            LogAssert.Expect(LogType.Warning, new Regex("Flowchart was not valid"));

            Assert.IsFalse(handler.ExecuteBlock());
        }

        #endregion
    }

    [Serializable]
    public class CountingHyphandler : Hyphandler
    {
        /// <summary>
        /// Across all instances, so a stale instance that never unsubscribed shows up.
        /// </summary>
        public static int ActiveSubscriptions;

        [NonSerialized] public int SubscribeCalls;
        [NonSerialized] public int UnsubscribeCalls;

        protected override bool SetSubsOnlyInRuntime => false;

        protected override void SetSubs(bool on)
        {
            base.SetSubs(on);
            if (on)
            {
                SubscribeCalls++;
                ActiveSubscriptions++;
            }
            else
            {
                UnsubscribeCalls++;
                ActiveSubscriptions--;
            }
        }
    }

    [Serializable]
    public class RuntimeOnlyHyphandler : Hyphandler
    {
        [NonSerialized] public int SubscribeCalls;

        protected override void SetSubs(bool on)
        {
            base.SetSubs(on);
            if (on)
            {
                SubscribeCalls++;
            }
        }
    }

    public class UnsupportedEventHandler : IEventHandler
    {
        public IBlock ParentBlock { get; set; }
        public string GetSummary() => "";
        public string DisplayNameAboveBlock => "Unsupported";
    }
}
