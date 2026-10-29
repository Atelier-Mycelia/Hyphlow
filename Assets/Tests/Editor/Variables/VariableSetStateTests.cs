using AtMycelia.Hyphlow;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// Covers detached copies and ApplyStateFrom, which Play Mode restores and the
    /// Control Panel's staged editing are built on.
    /// </summary>
    public class VariableSetStateTests
    {
        private VariableSet _original;
        private readonly List<UnityObj> _toDestroy = new List<UnityObj>();

        private const string TestAssetPath = "Assets/TestVariableSetState.asset";
        private const string GreetingKey = "greeting";
        private const string GreetingValue = "hello";
        private const string CountKey = "count";

        [SetUp]
        public void SetUp()
        {
            VariableTypeDiscovery.DiscoverAndRegister();
            AssetDatabase.DeleteAsset(TestAssetPath);

            // A real asset, since that's what detachment and UniqueIds are defined against
            _original = ScriptableObject.CreateInstance<VariableSet>();
            AssetDatabase.CreateAsset(_original, TestAssetPath);
            _original.Refresh();

            _original.AddNewVariableOfContentType<string>(GreetingKey, GreetingValue);
            _original.AddNewVariableOfContentType<int>(CountKey, 3);
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
            AssetDatabase.DeleteAsset(TestAssetPath);
            _original = null;
        }

        private VariableSet MakeCopy()
        {
            VariableSet copy = _original.CreateDetachedCopy();
            _toDestroy.Add(copy);
            return copy;
        }

        private static Muscariable<T> Var<T>(VariableSet set, string key)
        {
            return (Muscariable<T>)set.GetVariableByName(key);
        }

        #region Detached copies
        [Test]
        public void CreateDetachedCopy_OnlyTheCopyIsDetached()
        {
            VariableSet copy = MakeCopy();

            Assert.IsTrue(copy.IsDetached);
            Assert.IsFalse(_original.IsDetached);
        }

        [Test]
        public void CreateDetachedCopy_SharesIdentityAndState_ButNotInstances()
        {
            VariableSet copy = MakeCopy();

            Assert.AreEqual(_original.UniqueId, copy.UniqueId);
            Assert.AreEqual(_original.name, copy.name);
            Assert.AreEqual(GreetingValue, Var<string>(copy, GreetingKey).Value);
            Assert.AreNotSame(_original.GetVariableByName(GreetingKey), copy.GetVariableByName(GreetingKey));
            Assert.AreEqual(_original.GetVariableByName(GreetingKey).ItemId,
                copy.GetVariableByName(GreetingKey).ItemId);
        }

        [Test]
        public void DetachedCopy_AddingAndRemovingVars_RaisesNoOutsideSignals()
        {
            VariableSet copy = MakeCopy();
            int setSignals = 0, varSignals = 0;
            Action<VariableSet, IVariable> onSetSignal = (set, _) => { if (set == copy) setSignals++; };
            Action<IVariable> onVarSignal = _ => varSignals++;

            VarSetSignals.VariableAdded += onSetSignal;
            VarSetSignals.VariableRemoved += onSetSignal;
            VariableSignals.VariableAdded += onVarSignal;
            VariableSignals.VariableRemoved += onVarSignal;
            try
            {
                copy.AddNewVariableOfContentType<float>("speed", 1f);
                copy.RemoveVariable(GreetingKey);
            }
            finally
            {
                VarSetSignals.VariableAdded -= onSetSignal;
                VarSetSignals.VariableRemoved -= onSetSignal;
                VariableSignals.VariableAdded -= onVarSignal;
                VariableSignals.VariableRemoved -= onVarSignal;
            }

            Assert.AreEqual(0, setSignals);
            Assert.AreEqual(0, varSignals);
        }

        [Test]
        public void Original_AddingVars_StillRaisesSignals()
        {
            int setSignals = 0;
            Action<VariableSet, IVariable> onSetSignal = (set, _) => { if (set == _original) setSignals++; };

            VarSetSignals.VariableAdded += onSetSignal;
            try
            {
                _original.AddNewVariableOfContentType<float>("speed", 1f);
            }
            finally
            {
                VarSetSignals.VariableAdded -= onSetSignal;
            }

            Assert.AreEqual(1, setSignals);
        }

        [Test]
        public void DetachedCopy_Destroyed_DoesNotReportOriginalAsDestroyed()
        {
            VariableSet copy = MakeCopy();
            string originalId = _original.UniqueId;
            bool reported = false;
            Action<string, string> onDestroyed = (_, uid) => reported |= uid == originalId;

            VarSetSignals.VsaDestroyed += onDestroyed;
            try
            {
                UnityObj.DestroyImmediate(copy);
            }
            finally
            {
                VarSetSignals.VsaDestroyed -= onDestroyed;
            }

            Assert.IsFalse(reported);
        }
        #endregion

        #region ApplyStateFrom
        [Test]
        public void ApplyStateFrom_ChangedValue_IsOverwrittenIntoTheSameInstance()
        {
            VariableSet copy = MakeCopy();
            Var<string>(copy, GreetingKey).Value = "goodbye";
            IVariable before = _original.GetVariableByName(GreetingKey);

            _original.ApplyStateFrom(copy);

            Assert.AreSame(before, _original.GetVariableByName(GreetingKey));
            Assert.AreEqual("goodbye", Var<string>(_original, GreetingKey).Value);
        }

        [Test]
        public void ApplyStateFrom_RenamedVar_KeepsInstanceUnderTheNewKey()
        {
            VariableSet copy = MakeCopy();
            copy.GetVariableByName(GreetingKey).Key = "salutation";
            IVariable before = _original.GetVariableByName(GreetingKey);

            _original.ApplyStateFrom(copy);

            Assert.IsNull(_original.GetVariableByName(GreetingKey));
            Assert.AreSame(before, _original.GetVariableByName("salutation"));
        }

        [Test]
        public void ApplyStateFrom_VarOnlyInSource_IsAddedAsItsOwnInstance_WithTheSameId()
        {
            VariableSet copy = MakeCopy();
            Muscariable added = copy.AddNewVariableOfContentType<float>("speed", 2.5f);

            _original.ApplyStateFrom(copy);

            Muscariable result = _original.GetVariableByName("speed");
            Assert.IsNotNull(result);
            Assert.AreNotSame(added, result);
            Assert.AreEqual(added.ItemId, result.ItemId);
            Assert.AreEqual(added.GetType(), result.GetType());
            Assert.AreEqual(2.5f, ((Muscariable<float>)result).Value);
            Assert.AreSame(_original, result.Owner);
        }

        [Test]
        public void ApplyStateFrom_VarMissingFromSource_IsRemoved()
        {
            VariableSet copy = MakeCopy();
            copy.RemoveVariable(CountKey);

            _original.ApplyStateFrom(copy);

            Assert.IsNull(_original.GetVariableByName(CountKey));
            Assert.AreEqual(1, _original.Variables.Count);
        }

        [Test]
        public void ApplyStateFrom_ReorderedSource_ReordersTheSameInstances()
        {
            VariableSet copy = MakeCopy();
            copy.ReorderVariables(copy.Variables.Reverse().ToList());
            var originalInstances = _original.Variables.ToList();

            _original.ApplyStateFrom(copy);

            var keys = _original.Variables.Select(v => v.Key).ToArray();
            CollectionAssert.AreEqual(new[] { CountKey, GreetingKey }, keys);
            Assert.AreSame(originalInstances[1], _original.Variables[0]);
            Assert.AreSame(originalInstances[0], _original.Variables[1]);
        }

        [Test]
        public void ApplyStateFrom_SameIdButDifferentType_IsTreatedAsADifferentVariable()
        {
            VariableSet copy = MakeCopy();
            byte reusedId = copy.GetVariableByName(GreetingKey).ItemId;
            copy.RemoveVariable(GreetingKey);
            Muscariable replacement = copy.AddNewVariableOfContentType<float>("speed", 1f);
            replacement.ItemId = reusedId;
            copy.Refresh();
            IVariable oldGreeting = _original.GetVariableByName(GreetingKey);

            _original.ApplyStateFrom(copy);

            IVariable result = _original.GetVariable(reusedId);
            Assert.AreNotSame(oldGreeting, result);
            Assert.AreEqual(typeof(float), result.ContentType);
            Assert.IsNull(_original.GetVariableByName(GreetingKey));
        }

        [Test]
        public void ApplyStateFrom_CopiesSettings_ButKeepsIdentity()
        {
            string originalId = _original.UniqueId;
            string originalName = _original.name;
            VariableSet copy = MakeCopy();
            copy.IncludeInSaves = !_original.IncludeInSaves;
            copy.AlwaysKeepGuid = !_original.AlwaysKeepGuid;
            copy.name = "SomethingElse";

            _original.ApplyStateFrom(copy);

            Assert.AreEqual(copy.IncludeInSaves, _original.IncludeInSaves);
            Assert.AreEqual(copy.AlwaysKeepGuid, _original.AlwaysKeepGuid);
            Assert.AreEqual(originalId, _original.UniqueId);
            Assert.AreEqual(originalName, _original.name);
        }

        [Test]
        public void ApplyStateFrom_RaisesSignalsForTheTarget()
        {
            VariableSet copy = MakeCopy();
            copy.RemoveVariable(CountKey);
            copy.AddNewVariableOfContentType<float>("speed", 1f);
            int added = 0, removed = 0, applied = 0;
            Action<VariableSet, IVariable> onAdded = (set, _) => { if (set == _original) added++; };
            Action<VariableSet, IVariable> onRemoved = (set, _) => { if (set == _original) removed++; };
            Action<VariableSet> onApplied = set => { if (set == _original) applied++; };

            VarSetSignals.VariableAdded += onAdded;
            VarSetSignals.VariableRemoved += onRemoved;
            VarSetSignals.StateApplied += onApplied;
            try
            {
                _original.ApplyStateFrom(copy);
            }
            finally
            {
                VarSetSignals.VariableAdded -= onAdded;
                VarSetSignals.VariableRemoved -= onRemoved;
                VarSetSignals.StateApplied -= onApplied;
            }

            Assert.AreEqual(1, added);
            Assert.AreEqual(1, removed);
            Assert.AreEqual(1, applied);
        }

        /// <summary>
        /// The Play Mode restore scenario: snapshot, mutate the original, apply the snapshot.
        /// </summary>
        [Test]
        public void ApplyStateFrom_Snapshot_UndoesEveryKindOfChange()
        {
            VariableSet snapshot = MakeCopy();
            IVariable greeting = _original.GetVariableByName(GreetingKey);
            Var<string>(_original, GreetingKey).Value = "changed";
            _original.RemoveVariable(CountKey);
            _original.AddNewVariableOfContentType<bool>("flag", true);

            _original.ApplyStateFrom(snapshot);

            var keys = _original.Variables.Select(v => v.Key).ToArray();
            CollectionAssert.AreEqual(new[] { GreetingKey, CountKey }, keys);
            Assert.AreSame(greeting, _original.GetVariableByName(GreetingKey));
            Assert.AreEqual(GreetingValue, Var<string>(_original, GreetingKey).Value);
            Assert.AreEqual(3, Var<int>(_original, CountKey).Value);
        }

        [Test]
        public void ApplyStateFrom_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _original.ApplyStateFrom(null));
        }
        #endregion
    }
}
