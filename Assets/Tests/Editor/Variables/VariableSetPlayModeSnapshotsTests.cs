using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// Drives Capture and Restore directly rather than through Play Mode, which
    /// they're what VariableSetPlayModeSnapshots runs on either side of.
    /// </summary>
    public class VariableSetPlayModeSnapshotsTests
    {
        private VariableSet _set;

        private const string TestAssetPath = "Assets/TestVariableSetSnapshot.asset";
        private const string GreetingKey = "greeting";
        private const string GreetingValue = "hello";
        private const string CountKey = "count";

        [SetUp]
        public void SetUp()
        {
            VariableTypeDiscovery.DiscoverAndRegister();
            AssetDatabase.DeleteAsset(TestAssetPath);

            _set = ScriptableObject.CreateInstance<VariableSet>();
            AssetDatabase.CreateAsset(_set, TestAssetPath);
            _set.Refresh();
            _set.AddNewVariableOfContentType<string>(GreetingKey, GreetingValue);
            _set.AddNewVariableOfContentType<int>(CountKey, 3);
            AssetDatabase.SaveAssetIfDirty(_set);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestAssetPath);
            _set = null;
        }

        private VariableSetPlayModeSnapshots.Snapshot CaptureSet()
        {
            return VariableSetPlayModeSnapshots.Capture(new[] { _set });
        }

        private static Muscariable<T> Var<T>(VariableSet set, string key)
        {
            return (Muscariable<T>)set.GetVariableByName(key);
        }

        [Test]
        public void Restore_UndoesValueAddAndRemoveChanges_KeepingInstances()
        {
            var snapshot = CaptureSet();
            IVariable greeting = _set.GetVariableByName(GreetingKey);
            Var<string>(_set, GreetingKey).Value = "changed in play mode";
            _set.RemoveVariable(CountKey);
            _set.AddNewVariableOfContentType<bool>("flag", true);

            var result = VariableSetPlayModeSnapshots.Restore(snapshot);

            Assert.AreEqual(1, result.restored);
            Assert.AreEqual(0, result.failed);
            CollectionAssert.AreEqual(new[] { GreetingKey, CountKey },
                _set.Variables.Select(v => v.Key).ToArray());
            Assert.AreSame(greeting, _set.GetVariableByName(GreetingKey));
            Assert.AreEqual(GreetingValue, Var<string>(_set, GreetingKey).Value);
            Assert.AreEqual(3, Var<int>(_set, CountKey).Value);
        }

        [Test]
        public void Restore_UnchangedSet_IsLeftAlone()
        {
            var snapshot = CaptureSet();
            int applied = 0;
            Action<VariableSet> onApplied = _ => applied++;

            VarSetSignals.StateApplied += onApplied;
            VariableSetPlayModeSnapshots.RestoreResult result;
            try
            {
                result = VariableSetPlayModeSnapshots.Restore(snapshot);
            }
            finally
            {
                VarSetSignals.StateApplied -= onApplied;
            }

            Assert.AreEqual(0, result.restored);
            Assert.AreEqual(0, applied);
            Assert.IsFalse(EditorUtility.IsDirty(_set));
        }

        [Test]
        public void Restore_SetCleanBeforehand_IsSavedAfterwards()
        {
            var snapshot = CaptureSet();
            Var<string>(_set, GreetingKey).Value = "changed in play mode";
            EditorUtility.SetDirty(_set);

            VariableSetPlayModeSnapshots.Restore(snapshot);

            Assert.IsFalse(EditorUtility.IsDirty(_set));
        }

        [Test]
        public void Restore_SetDirtyBeforehand_StaysUnsaved()
        {
            Var<string>(_set, GreetingKey).Value = "unsaved edit-mode change";
            EditorUtility.SetDirty(_set);
            var snapshot = CaptureSet();
            Var<string>(_set, GreetingKey).Value = "changed in play mode";

            VariableSetPlayModeSnapshots.Restore(snapshot);

            Assert.IsTrue(EditorUtility.IsDirty(_set));
            Assert.AreEqual("unsaved edit-mode change", Var<string>(_set, GreetingKey).Value);
        }

        [Test]
        public void Restore_SetDeletedSinceCapture_IsSkipped()
        {
            var snapshot = CaptureSet();
            AssetDatabase.DeleteAsset(TestAssetPath);

            var result = VariableSetPlayModeSnapshots.Restore(snapshot);

            Assert.AreEqual(0, result.restored);
            Assert.AreEqual(0, result.failed);
        }

        [Test]
        public void Capture_SkipsSetsThatArentAssets()
        {
            var inMemory = ScriptableObject.CreateInstance<VariableSet>();
            try
            {
                var snapshot = VariableSetPlayModeSnapshots.Capture(new[] { inMemory });
                Assert.AreEqual(0, snapshot.entries.Count);
            }
            finally
            {
                UnityObj.DestroyImmediate(inMemory);
            }
        }

        /// <summary>
        /// The snapshot goes through a file so it can outlive the domain reload on entering
        /// Play Mode, so it has to come back from JSON intact.
        /// </summary>
        [Test]
        public void Snapshot_SurvivesJsonRoundTrip()
        {
            string asJson = JsonUtility.ToJson(CaptureSet());
            Var<int>(_set, CountKey).Value = 99;

            var reloaded = JsonUtility.FromJson<VariableSetPlayModeSnapshots.Snapshot>(asJson);
            var result = VariableSetPlayModeSnapshots.Restore(reloaded);

            Assert.AreEqual(1, result.restored);
            Assert.AreEqual(3, Var<int>(_set, CountKey).Value);
        }
    }
}
