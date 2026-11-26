using System;
using System.Collections.Generic;
using System.Reflection;
using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.Variables
{
    public class VariableRegistryTests
    {
        private const string TestSetPath = "Assets/VariableRegistryTests_Set.asset";

        private readonly List<UnityObj> _toDestroy = new List<UnityObj>();
        private Flowchart _otherFc;

        [SetUp]
        public void SetUp()
        {
            VariableTypeDiscovery.DiscoverAndRegister();
            GameObject holder = new GameObject("VariableRegistryTests_Other");
            _toDestroy.Add(holder);
            _otherFc = holder.AddComponent<Flowchart>();
        }

        [TearDown]
        public void TearDown()
        {
            VariableRegistry.RegistryChanged -= OnRegistryChangedReenter;
            VariableRegistry.RegistryChanged -= CountRegistryChange;
            AssetDatabase.DeleteAsset(TestSetPath);

            for (int i = 0; i < _toDestroy.Count; i++)
            {
                if (_toDestroy[i] != null)
                {
                    UnityObj.DestroyImmediate(_toDestroy[i]);
                }
            }

            _toDestroy.Clear();
            _otherFc = null;
            VariableRegistry.Rebuild();
        }

        private string OtherFcKey(string varKey) =>
            $"[Others in Scene]/[{_otherFc.gameObject.name}]/{varKey}";

        [TestCase(AccessScope.Public, true)]
        [TestCase(AccessScope.ReadOnly, true)]
        [TestCase(AccessScope.Private, false)]
        [TestCase(AccessScope.Protected, false)]
        [TestCase(AccessScope.Null, false)]
        public void Rebuild_OtherFlowchartVar_RegisteredOnlyIfVisibleToOutsiders(AccessScope scope,
            bool expectRegistered)
        {
            _otherFc.AddNewVariable<int>("scopedVar", 1, scope);

            VariableRegistry.Rebuild();

            bool registered = VariableRegistry.Variables.ContainsKey(OtherFcKey("scopedVar"));
            Assert.AreEqual(expectRegistered, registered);
        }

        [Test]
        public void GetVarsOfMultiTypes_AssignableTypes_IncludesVector4VarForVector3Field()
        {
            // TransformProperty's _inOutVar used to reject the Vector (Vector4) variable
            _otherFc.AddNewVariable<Vector4>("vec4Var", Vector4.one, AccessScope.Public);
            VariableRegistry.Rebuild();
            Type[] fieldTypes = { typeof(Vector3), typeof(Transform), typeof(int), typeof(bool) };

            var assignable = VariableRegistry.GetVarsOfMultiTypes(fieldTypes, true);
            var exact = VariableRegistry.GetVarsOfMultiTypes(fieldTypes, false);

            Assert.IsTrue(assignable.ContainsKey(OtherFcKey("vec4Var")));
            Assert.IsFalse(exact.ContainsKey(OtherFcKey("vec4Var")),
                "Sanity check: an exact match shouldn't count a Vector4 as a Vector3.");
        }

        [Test]
        public void GetVarsOfMultiTypes_AssignableTypes_TreatsNumberTypesAsInterchangeable()
        {
            _otherFc.AddNewVariable<int>("intVar", 3, AccessScope.Public);
            VariableRegistry.Rebuild();

            var forFloatField = VariableRegistry.GetVarsOfMultiTypes(new[] { typeof(float) }, true);

            Assert.IsTrue(forFloatField.ContainsKey(OtherFcKey("intVar")));
        }

        [Test]
        public void TransformPropertyInOutVar_AllowsVector4Content()
        {
            FieldInfo field = typeof(TransformProperty).GetField("_inOutVar",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "TransformProperty has no _inOutVar field.");

            var constraint = field.GetCustomAttribute<ContentTypeConstraintAttribute>();
            Assert.IsNotNull(constraint, "_inOutVar has no ContentTypeConstraint.");
            CollectionAssert.Contains(constraint.AllowedTypes, typeof(Vector4));
        }

        [Test]
        public void AddingVarToVariableSet_RebuildsRegistry()
        {
            // The registry used to unsubscribe from VarSetSignals instead of subscribing
            VariableSet set = ScriptableObject.CreateInstance<VariableSet>();
            AssetDatabase.CreateAsset(set, TestSetPath);
            AssetDatabase.SaveAssets();
            RunPendingDelayCalls();
            VariableRegistry.Rebuild();

            set.AddNewVariableOfContentType<int>("addedLater", 5);
            string registryKey = $"~Globals~/~{set.name}~/addedLater";
            Assert.IsFalse(VariableRegistry.Variables.ContainsKey(registryKey),
                "In the editor, the rebuild should wait for a delayCall.");

            RunPendingDelayCalls();

            Assert.IsTrue(VariableRegistry.Variables.ContainsKey(registryKey));
        }

        [Test]
        public void BurstOfVariableSetSignals_SharesOneRebuild()
        {
            RunPendingDelayCalls(); // So a rebuild queued before this test can't be counted
            _registryChangeCount = 0;
            VariableRegistry.RegistryChanged += CountRegistryChange;

            for (int i = 0; i < 5; i++)
            {
                VarSetSignals.VariableAdded(null, null);
            }

            Assert.AreEqual(0, _registryChangeCount, "The rebuild should wait for a delayCall.");
            RunPendingDelayCalls();

            Assert.AreEqual(1, _registryChangeCount);
        }

        /// <summary>
        /// Does what the editor does with delayCall on its next update. The tests can't just
        /// yield for frames, since delayCall doesn't run while the editor is unfocused.
        /// </summary>
        private static void RunPendingDelayCalls()
        {
            EditorApplication.CallbackFunction pending = EditorApplication.delayCall;
            EditorApplication.delayCall = null;
            pending?.Invoke();
        }

        private int _registryChangeCount;

        private void CountRegistryChange()
        {
            _registryChangeCount++;
        }

        [Test]
        public void Rebuild_RequestedMidRebuild_RunsAfterCurrentOneInsteadOfNesting()
        {
            // Loading sets mid-rebuild raises VsaEnabled, which lands back in Rebuild
            _rebuildDepth = 0;
            _maxRebuildDepth = 0;
            _reentryCount = 0;
            VariableRegistry.RegistryChanged += OnRegistryChangedReenter;

            VariableRegistry.Rebuild();

            Assert.AreEqual(2, _reentryCount, "The deferred rebuild should still have run.");
            Assert.AreEqual(1, _maxRebuildDepth, "The second rebuild ran inside the first.");
        }

        private int _rebuildDepth, _maxRebuildDepth, _reentryCount;

        private void OnRegistryChangedReenter()
        {
            _reentryCount++;
            _rebuildDepth++;
            _maxRebuildDepth = Mathf.Max(_maxRebuildDepth, _rebuildDepth);
            if (_reentryCount == 1)
            {
                VariableRegistry.Rebuild();
            }

            _rebuildDepth--;
        }
    }
}
