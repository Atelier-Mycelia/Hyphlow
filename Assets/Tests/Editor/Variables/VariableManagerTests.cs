using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.Variables
{
    public sealed class VariableManagerTests
    {
        private VariableManager _manager;
        private Flowchart _fc;
        private readonly List<UnityObj> _toDestroy = new List<UnityObj>();

        [SetUp]
        public void SetUp()
        {
            _fc = GameObject.Instantiate(new GameObject("VariableManagerTestFlowchart")).AddComponent<Flowchart>();
            _toDestroy.Add(_fc.gameObject);
            _manager = new VariableManager();
            _manager.VarOwner = _fc;
            _manager.Initialize();
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
            _manager = null;
        }

        [Test]
        public void RemoveVariable_RemovesMuscariableWithNullValue()
        {
            StringMuscariable variable = _manager.AddNewMuscari<string, StringMuscariable>("NullVar", null);

            List<Muscariable> muscariables = GetMuscariablesList(_manager);
            Assert.AreEqual(1, muscariables.Count);

            _manager.RemoveVariable(variable);

            Assert.AreEqual(0, muscariables.Count);
            Assert.AreEqual(0, _manager.VariableCount);
        }

        [Test]
        public void Clear_EmptiesInternalLegacyAndMuscariableLists()
        {
            GameObject host = CreateHostWithFlowchart("VariableManagerTests_LegacyVarHost");

            IntegerVariable legacyVar = host.AddComponent<IntegerVariable>();
            legacyVar.Key = "LegacyInt";
            legacyVar.Value = 7;

            StringMuscariable muscariVar = new StringMuscariable
            {
                Key = "MuscariStr",
                Value = "abc"
            };

            _manager.Initialize(
                new List<Muscariable> { muscariVar },
                new List<Variable> { legacyVar });

            List<Muscariable> muscariables = GetMuscariablesList(_manager);
            List<Variable> legacyVariables = GetLegacyVariablesList(_manager);

            Assert.AreEqual(1, muscariables.Count);
            Assert.AreEqual(1, legacyVariables.Count);

            _manager.Clear();

            Assert.AreEqual(0, muscariables.Count, "Expected _muscariables to be empty after Clear().");
            Assert.AreEqual(0, legacyVariables.Count, "Expected _legacyVariables to be empty after Clear().");
            Assert.AreEqual(0, _manager.VariableCount, "Expected VariableCount to be zero after Clear().");
        }

        [Test]
        public void Clear_FalseTriggerSignals_StillRemovesAllVariables()
        {
            int preRemovedCalls = 0;
            int removedCalls = 0;
            _manager.PreVariableRemoved += _ => preRemovedCalls++;
            _manager.VariableRemoved += _ => removedCalls++;

            _manager.AddNewMuscari<string, StringMuscariable>("VarA", "A");
            _manager.AddNewMuscari<string, StringMuscariable>("VarB", "B");

            Assert.AreEqual(2, _manager.VariableCount);

            _manager.Clear(triggerSignals: false);

            Assert.AreEqual(0, _manager.VariableCount,
                "Expected all variables to be removed by Clear(false).");

            Assert.AreEqual(0, GetMuscariablesList(_manager).Count,
                "Expected _muscariables to be empty after Clear(false).");

            Assert.AreEqual(0, GetLegacyVariablesList(_manager).Count,
                "Expected _legacyVariables to be empty after Clear(false).");

            Assert.AreEqual(0, preRemovedCalls,
                "PreVariableRemoved should not fire when triggerSignals is false.");

            Assert.AreEqual(0, removedCalls,
                "VariableRemoved should not fire when triggerSignals is false.");
        }

        [Test]
        public void RemoveLegacyVarAtIndex_TriggerSignalsFalse_RemovesVariable()
        {
            int preRemovedCalls = 0;
            int removedCalls = 0;
            _manager.PreVariableRemoved += _ => preRemovedCalls++;
            _manager.VariableRemoved += _ => removedCalls++;

            GameObject host = CreateHostWithFlowchart("VariableManagerTests_RemoveLegacyVarAtIndexHost");

            IntegerVariable legacyVar = host.AddComponent<IntegerVariable>();
            legacyVar.Key = "LegacyToRemove";
            legacyVar.Value = 42;

            _manager.Initialize(
                new List<Muscariable>(),
                new List<Variable> { legacyVar });

            Assert.AreEqual(1, GetLegacyVariablesList(_manager).Count);
            Assert.AreEqual(1, _manager.VariableCount);

            _manager.RemoveLegacyVarAtIndex(0, triggerSignals: false);

            Assert.AreEqual(0, GetLegacyVariablesList(_manager).Count,
                "Expected legacy variable to be removed even when triggerSignals is false.");
            Assert.AreEqual(0, _manager.VariableCount,
                "Expected lookup/count to reflect legacy variable removal.");
            Assert.AreEqual(0, preRemovedCalls,
                "PreVariableRemoved should not fire when triggerSignals is false.");
            Assert.AreEqual(0, removedCalls,
                "VariableRemoved should not fire when triggerSignals is false.");
        }

        [Test]
        public void RemoveMuscariAtIndex_TriggerSignalsFalse_DoesNotEmitEvents()
        {
            int preRemovedCalls = 0;
            int removedCalls = 0;
            _manager.PreVariableRemoved += _ => preRemovedCalls++;
            _manager.VariableRemoved += _ => removedCalls++;

            _manager.AddNewMuscari<string, StringMuscariable>("ToRemove", "x");
            Assert.AreEqual(1, GetMuscariablesList(_manager).Count);

            _manager.RemoveMuscariAtIndex(0, triggerSignals: false);

            Assert.AreEqual(0, GetMuscariablesList(_manager).Count,
                "Expected muscariable to be removed.");
            Assert.AreEqual(0, _manager.VariableCount,
                "Expected lookup/count to reflect muscariable removal.");
            Assert.AreEqual(0, preRemovedCalls,
                "PreVariableRemoved should not fire when triggerSignals is false.");
            Assert.AreEqual(0, removedCalls,
                "VariableRemoved should not fire when triggerSignals is false.");
        }

        [Test]
        public void AddVariable_LegacyVariable_IsConvertedToMuscariable()
        {
            GameObject host = CreateHostWithFlowchart("VariableManagerTests_LegacyToMuscariHost");

            IntegerVariable legacyVar = host.AddComponent<IntegerVariable>();
            legacyVar.Key = "LegacySource";
            legacyVar.Value = 13;

            IVariable result = _manager.AddVariable((IVariable)legacyVar);

            Assert.NotNull(result, "Expected AddVariable to return a converted muscariable.");
            Assert.IsInstanceOf<Muscariable>(result, "Expected returned variable to be a muscariable.");
            Assert.AreNotSame(legacyVar, result, "Expected returned variable to be a converted instance.");

            Assert.AreEqual(1, GetMuscariablesList(_manager).Count,
                "Expected converted variable to be registered in _muscariables.");
            Assert.AreEqual(0, GetLegacyVariablesList(_manager).Count,
                "Expected no raw legacy variable registration.");
            Assert.AreEqual(1, _manager.VariableCount,
                "Expected lookup to contain exactly one registered variable.");
        }

        [Test]
        public void AddVariable_DuplicateInstance_IsIgnored()
        {
            StringMuscariable sameInstance = new StringMuscariable
            {
                Key = "dup",
                Value = "v"
            };

            Muscariable firstAdd = _manager.AddVariable(sameInstance);
            Muscariable secondAdd = _manager.AddVariable(sameInstance);

            Assert.NotNull(firstAdd, "First add should succeed.");
            Assert.IsNull(secondAdd, "Second add of same instance should return null.");
            Assert.AreEqual(1, GetMuscariablesList(_manager).Count,
                "Expected only one registered muscariable instance.");
            Assert.AreEqual(1, _manager.VariableCount,
                "Expected lookup count to remain stable after duplicate add attempt.");
        }

        [Test]
        public void AddVariable_LegacyDuplicateInputInstance_IsIgnored()
        {
            GameObject host = CreateHostWithFlowchart("VariableManagerTests_LegacyDuplicateHost");

            IntegerVariable legacyVar = host.AddComponent<IntegerVariable>();
            legacyVar.Key = "LegacyDup";
            legacyVar.Value = 99;

            IVariable firstAdd = _manager.AddVariable(legacyVar);
            Assert.NotNull(firstAdd, "First add should succeed.");

            IVariable secondAdd = _manager.AddVariable(legacyVar);
            Assert.IsNull(secondAdd,
                "Second add of same legacy input should be ignored and return null.");

            Assert.AreEqual(1, GetMuscariablesList(_manager).Count,
                "Expected only one converted muscariable registration.");
            Assert.AreEqual(0, GetLegacyVariablesList(_manager).Count,
                "Expected no raw legacy registrations.");
            Assert.AreEqual(1, _manager.VariableCount,
                "Expected lookup count to remain stable after duplicate legacy add attempt.");
        }

        [Test]
        public void EnsureValidAndUniqueIdsForAllOurVars_FixesInvalidAndDuplicateIds()
        {
            List<Muscariable> muscariables = GetMuscariablesList(_manager);

            StringMuscariable first = new StringMuscariable { Key = "A", Value = "a", ItemId = 0 };
            StringMuscariable second = new StringMuscariable { Key = "B", Value = "b", ItemId = 5 };
            StringMuscariable third = new StringMuscariable { Key = "C", Value = "c", ItemId = 5 };

            muscariables.Add(first);
            muscariables.Add(second);
            muscariables.Add(third);

            _manager.Refresh();
            _manager.EnsureValidAndUniqueIdsForAllOurVars();

            IReadOnlyList<IVariable> vars = _manager.Variables;
            List<byte> ids = vars.Select(v => v.ItemId).ToList();

            Assert.AreEqual(3, vars.Count, "Expected all seeded variables to remain registered.");
            Assert.False(ids.Any(id => id == 0), "Expected all IDs to be non-zero after fix-up.");
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Expected all IDs to be unique after fix-up.");
            Assert.AreEqual(3, _manager.VariableCount, "Expected lookup count to match registered variables.");
        }

        private GameObject CreateHostWithFlowchart(string name)
        {
            GameObject host = new GameObject(name);
            host.AddComponent<Flowchart>();
            _toDestroy.Add(host);
            return host;
        }

        private static List<Muscariable> GetMuscariablesList(VariableManager manager)
        {
            FieldInfo field = typeof(VariableManager).GetField("_muscariables",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field, "VariableManager._muscariables field not found.");

            return (List<Muscariable>)field.GetValue(manager);
        }

        private static List<Variable> GetLegacyVariablesList(VariableManager manager)
        {
            FieldInfo field = typeof(VariableManager).GetField("_legacyVariables",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(field, "VariableManager._legacyVariables field not found.");

            return (List<Variable>)field.GetValue(manager);
        }

        [Test]
        public void AddNewVariable_DuplicateKeys_AutoUniquifiesKey()
        {
            _manager.AddNewVariable("dupKey", "A");
            _manager.AddNewVariable("dupKey", "B");

            List<string> keys = _manager.Variables.Select(v => v.Key).ToList();

            Assert.AreEqual(2, _manager.VariableCount,
                "Expected exactly two variables to be registered.");

            Assert.AreEqual(2, keys.Count,
                "Expected exactly two keys in the manager.");

            Assert.AreEqual(2, keys.Distinct().Count(),
                "Expected duplicate requested keys to be uniquified.");

            Assert.True(keys.All(k => !string.IsNullOrEmpty(k)),
                "Expected all generated keys to be non-empty.");
        }

        [Test]
        public void ReorderVariables_WithSameContents_ReordersWithoutLoss()
        {
            _manager.AddNewMuscari<string, StringMuscariable>("A", "va");
            _manager.AddNewMuscari<string, StringMuscariable>("B", "vb");
            _manager.AddNewMuscari<string, StringMuscariable>("C", "vc");

            List<IVariable> before = _manager.Variables.ToList();
            List<IVariable> reordered = new List<IVariable>
    {
        before[2],
        before[0],
        before[1]
    };

            _manager.ReorderVariables(reordered);

            List<IVariable> after = _manager.Variables.ToList();

            Assert.AreEqual(before.Count, after.Count,
                "Expected variable count to remain unchanged after valid reorder.");

            CollectionAssert.AreEquivalent(
                before.Select(v => v.Key).ToList(),
                after.Select(v => v.Key).ToList(),
                "Expected no variables to be lost or added during reorder.");

            CollectionAssert.AreEqual(
                reordered.Select(v => v.Key).ToList(),
                after.Select(v => v.Key).ToList(),
                "Expected order to match the requested valid permutation.");
        }

        [Test]
        public void ReorderVariables_WithMismatchedContents_AbortsAndKeepsOriginalOrder()
        {
            _manager.AddNewMuscari<string, StringMuscariable>("A", "va");
            _manager.AddNewMuscari<string, StringMuscariable>("B", "vb");
            _manager.AddNewMuscari<string, StringMuscariable>("C", "vc");

            List<IVariable> before = _manager.Variables.ToList();
            List<string> beforeKeys = before.Select(v => v.Key).ToList();

            // Missing one element on purpose => mismatched contents
            List<IVariable> invalidReorder = new List<IVariable>
            {
                before[1],
                before[0]
            };

            _manager.ReorderVariables(invalidReorder);

            List<IVariable> after = _manager.Variables.ToList();
            List<string> afterKeys = after.Select(v => v.Key).ToList();

            Assert.AreEqual(before.Count, after.Count,
                "Expected reorder abort to keep count unchanged.");

            CollectionAssert.AreEqual(beforeKeys, afterKeys,
                "Expected reorder abort to preserve original order.");

            Assert.AreEqual(before.Count, _manager.VariableCount,
                "Expected lookup count to remain unchanged when reorder is aborted.");
        }

        [Test]
        public void Dispose_ClearsVariables_AndNullsEvents()
        {
            _manager.AddNewMuscari<string, StringMuscariable>("A", "a");
            _manager.AddNewMuscari<string, StringMuscariable>("B", "b");
            Assert.AreEqual(2, _manager.VariableCount, "Precondition failed: expected seeded variables.");

            _manager.Dispose();

            Assert.AreEqual(0, _manager.VariableCount,
                "Expected VariableCount to be zero after Dispose().");
            Assert.AreEqual(0, GetMuscariablesList(_manager).Count,
                "Expected _muscariables to be empty after Dispose().");
            Assert.AreEqual(0, GetLegacyVariablesList(_manager).Count,
                "Expected _legacyVariables to be empty after Dispose().");

            Assert.IsNull(GetInstanceFieldValue(_manager, "VariableAdded"),
                "Expected VariableAdded event backing field to be null after Dispose().");
            Assert.IsNull(GetInstanceFieldValue(_manager, "VariableRemoved"),
                "Expected VariableRemoved event backing field to be null after Dispose().");
            Assert.IsNull(GetInstanceFieldValue(_manager, "PreVariableAdded"),
                "Expected PreVariableAdded event backing field to be null after Dispose().");
            Assert.IsNull(GetInstanceFieldValue(_manager, "PreVariableRemoved"),
                "Expected PreVariableRemoved event backing field to be null after Dispose().");
            Assert.IsNull(GetInstanceFieldValue(_manager, "Refreshed"),
                "Expected Refreshed event backing field to be null after Dispose().");
            Assert.IsNull(GetInstanceFieldValue(_manager, "Reordered"),
                "Expected Reordered event backing field to be null after Dispose().");
        }

        [Test]
        public void VarOwner_Setter_UpdatesOwnerOnMuscariablesOnly()
        {
            GameObject legacyHost = CreateHostWithFlowchart("VariableManagerTests_LegacyOwnerHost");
            Flowchart legacyFlowchart = legacyHost.GetComponent<Flowchart>();
            IntegerVariable legacyVar = legacyHost.AddComponent<IntegerVariable>();
            legacyVar.Key = "legacyVar";
            legacyVar.Value = 123;

            StringMuscariable muscari = new StringMuscariable
            {
                Key = "muscariVar",
                Value = "abc"
            };

            _manager.Initialize(
                new List<Muscariable> { muscari },
                new List<Variable> { legacyVar });

            GameObject ownerHost = CreateHostWithFlowchart("VariableManagerTests_NewVarOwnerHost");
            Flowchart newOwner = ownerHost.GetComponent<Flowchart>();

            _manager.VarOwner = newOwner;

            Assert.AreSame(newOwner, muscari.Owner,
                "Expected muscariable Owner to update when VarOwner changes.");

            Assert.AreNotSame(newOwner, legacyVar.Owner,
                "Expected legacy Variable Owner not to be overwritten by VarOwner propagation.");
            Assert.AreSame(legacyFlowchart, legacyVar.Owner,
                "Expected legacy Variable Owner to remain its original Flowchart.");
        }

        [Test]
        public void ResetAllVars_CallsOnResetExactlyOncePerVariable()
        {
            ResetTrackingStringMuscariable first = new ResetTrackingStringMuscariable
            {
                Key = "resetA",
                Value = "a"
            };

            ResetTrackingStringMuscariable second = new ResetTrackingStringMuscariable
            {
                Key = "resetB",
                Value = "b"
            };

            ResetTrackingStringMuscariable third = new ResetTrackingStringMuscariable
            {
                Key = "resetC",
                Value = "c"
            };

            _manager.AddVariable(first);
            _manager.AddVariable(second);
            _manager.AddVariable(third);

            _manager.ResetAllVars();

            Assert.AreEqual(1, first.ResetCalls, "Expected first variable OnReset to be called exactly once.");
            Assert.AreEqual(1, second.ResetCalls, "Expected second variable OnReset to be called exactly once.");
            Assert.AreEqual(1, third.ResetCalls, "Expected third variable OnReset to be called exactly once.");
            Assert.AreEqual(3, _manager.VariableCount, "Expected variable count to remain unchanged after reset.");
        }

        private static object GetInstanceFieldValue(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.NotNull(field, $"Field '{fieldName}' not found on {instance.GetType().Name}.");
            return field.GetValue(instance);
        }

        private sealed class ResetTrackingStringMuscariable : StringMuscariable
        {
            public int ResetCalls { get; private set; }

            public override void OnReset()
            {
                ResetCalls++;
                base.OnReset();
            }
        }
    }
}