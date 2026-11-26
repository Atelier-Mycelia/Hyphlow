using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.FlowchartStructure
{
    /// <summary>
    /// Duplicating, pasting or instantiating a Flowchart copies its UniqueId. The
    /// deduplicator should give the copy a new one, and leave the original's alone.
    /// </summary>
    public class FlowchartUidDeduplicatorTests
    {
        private readonly List<GameObject> _toDestroy = new List<GameObject>();
        private Flowchart _original;

        [SetUp]
        public void SetUp()
        {
            GameObject holder = new GameObject("UidDeduplicatorTests_Original");
            _toDestroy.Add(holder);
            _original = holder.AddComponent<Flowchart>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _toDestroy.Count; i++)
            {
                if (_toDestroy[i] != null)
                {
                    UnityObj.DestroyImmediate(_toDestroy[i]);
                }
            }

            _toDestroy.Clear();
            _original = null;
            FlowchartRegistry.EnsureInitialized(true);
        }

        private Flowchart DuplicateOriginal()
        {
            GameObject copy = UnityObj.Instantiate(_original.gameObject);
            _toDestroy.Add(copy);
            return copy.GetComponent<Flowchart>();
        }

        [Test]
        public void Precondition_InstantiatingFlowchart_CopiesUid()
        {
            Flowchart copy = DuplicateOriginal();

            Assert.IsFalse(string.IsNullOrEmpty(_original.UniqueId));
            Assert.AreEqual(_original.UniqueId, copy.UniqueId);
        }

        [Test]
        public void ResetUidIfTaken_CopyInSameScene_GivesCopyNewUid()
        {
            string originalUid = _original.UniqueId;
            Flowchart copy = DuplicateOriginal();

            bool reset = InvokeResetUidIfTaken(copy);

            Assert.IsTrue(reset);
            Assert.AreEqual(originalUid, _original.UniqueId, "The original's id should be kept.");
            Assert.AreNotEqual(originalUid, copy.UniqueId);
            Assert.IsFalse(string.IsNullOrEmpty(copy.UniqueId));
        }

        [Test]
        public void ResetUidIfTaken_UidNotShared_LeavesItAlone()
        {
            string originalUid = _original.UniqueId;

            bool reset = InvokeResetUidIfTaken(_original);

            Assert.IsFalse(reset);
            Assert.AreEqual(originalUid, _original.UniqueId);
        }

        [Test]
        public void DeduplicateUidsUnder_NestedCopy_GivesItNewUid()
        {
            string originalUid = _original.UniqueId;
            GameObject parent = new GameObject("UidDeduplicatorTests_Parent");
            _toDestroy.Add(parent);
            Flowchart nestedCopy = DuplicateOriginal();
            nestedCopy.transform.SetParent(parent.transform);

            bool resetAny = InvokeDeduplicateUidsUnder(parent.GetInstanceID());

            Assert.IsTrue(resetAny);
            Assert.AreNotEqual(originalUid, nestedCopy.UniqueId);
            Assert.AreEqual(originalUid, _original.UniqueId);
        }

        [UnityTest]
        public IEnumerator CreatingCopyWithUndo_GetsNewUidOnceChangesArePublished()
        {
            // This is how duplicating or pasting in the Hierarchy reaches the deduplicator
            string originalUid = _original.UniqueId;
            Flowchart copy = DuplicateOriginal();
            Undo.RegisterCreatedObjectUndo(copy.gameObject, "Duplicate Flowchart (test)");

            const int maxFramesToWait = 10;
            for (int i = 0; i < maxFramesToWait && copy.UniqueId == originalUid; i++)
            {
                yield return null;
            }

            Assert.AreNotEqual(originalUid, copy.UniqueId);
            Assert.AreEqual(originalUid, _original.UniqueId);
            Assert.AreSame(_original, FlowchartRegistry.GetFChartWith(originalUid));
            Assert.AreSame(copy, FlowchartRegistry.GetFChartWith(copy.UniqueId),
                "The registry should have been recaptured after the reset.");
        }

        #region Reflection helpers
        private const BindingFlags _privateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        private static bool InvokeResetUidIfTaken(Flowchart added)
        {
            MethodInfo method = typeof(FlowchartUidDeduplicator).GetMethod("ResetUidIfTaken",
                _privateStatic);
            Assert.IsNotNull(method, "FlowchartUidDeduplicator has no ResetUidIfTaken method.");
            return (bool)method.Invoke(null, new object[] { added });
        }

        private static bool InvokeDeduplicateUidsUnder(int gameObjectInstanceId)
        {
            MethodInfo method = typeof(FlowchartUidDeduplicator).GetMethod("DeduplicateUidsUnder",
                _privateStatic);
            Assert.IsNotNull(method, "FlowchartUidDeduplicator has no DeduplicateUidsUnder method.");
            return (bool)method.Invoke(null, new object[] { gameObjectInstanceId });
        }
        #endregion
    }
}
