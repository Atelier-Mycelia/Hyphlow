using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using UnityObj = UnityEngine.Object;
using System.Collections.Generic;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// Tests for the pure/logic-heavy parts of <see cref="VariableDataDrawerBase"/>
    /// that don't require simulating a full IMGUI paint cycle (that flow is
    /// already covered by <see cref="VariableDataEditorTests"/>). These tests
    /// exercise <c>ShouldDrawLiteral</c> and <c>GetPropertyHeight</c> directly
    /// against real <see cref="SerializedProperty"/> instances.
    /// </summary>
    public class VariableDataDrawerTests
    {
        [SetUp]
        public void SetUp()
        {
            VariableRegistry.Rebuild();

            _fcGo = new GameObject("TestFlowchart");
            _flowchart = _fcGo.AddComponent<Flowchart>();

            _holder = ScriptableObject.CreateInstance<IntegerDataHolder>();
            _serializedHolder = new SerializedObject(_holder);
            _dataProp = _serializedHolder.FindProperty("_data");
            Assert.IsNotNull(_dataProp, "Could not find '_data' property on holder.");

            _drawer = new TestableVariableDataDrawer();

            _toDestroyInTearDown.Add(_fcGo);
            _toDestroyInTearDown.Add(_holder);
        }

        private GameObject _fcGo;
        private Flowchart _flowchart;
        private IntegerDataHolder _holder;
        private SerializedObject _serializedHolder;
        private SerializedProperty _dataProp;
        private TestableVariableDataDrawer _drawer;
        private List<UnityObj> _toDestroyInTearDown = new List<UnityObj>();

        [TearDown]
        public void TearDown()
        {
            VariableRegistry.Rebuild();
            for (int i = 0; i < _toDestroyInTearDown.Count; i++)
            {
                var toDestroy = _toDestroyInTearDown[i];
                UnityObj.DestroyImmediate(toDestroy);
            }
        }

        [Test]
        public void ShouldDrawLiteral_WhenVarRefIsNull_ReturnsTrue()
        {
            _holder._data.VarRef = null;
            _serializedHolder.Update();

            bool result = TestableVariableDataDrawer.InvokeShouldDrawLiteral(_dataProp);

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldDrawLiteral_WhenVarRefIsAssignedValidVariable_ReturnsFalse()
        {
            var varManagerComponent = _flowchart.GetComponent<VariableManagerComponent>();
            var intVar = varManagerComponent.AddNewVariableOfContentType<int>("Health", 123, AccessScope.Private);
            Assert.Greater(intVar.ItemId, 0, "Muscariable ItemId should be assigned.");

            _holder._data.VarRef = intVar;
            EditorUtility.SetDirty(_holder);
            _serializedHolder.Update();

            bool result = TestableVariableDataDrawer.InvokeShouldDrawLiteral(_dataProp);

            Assert.IsFalse(result);
        }

        [Test]
        public void GetPropertyHeight_WhenLiteral_AndNoTextAreaAttribute_ReturnsSingleLineHeight()
        {
            _holder._data.VarRef = null;
            _serializedHolder.Update();

            float height = _drawer.GetPropertyHeight(_dataProp, new GUIContent("VariableData"));

            Assert.AreEqual(EditorGUIUtility.singleLineHeight, height, 0.01f);
        }

        [Test]
        public void GetPropertyHeight_WhenRepresentingVariable_ReturnsSingleLineHeight()
        {
            var varManagerComponent = _flowchart.GetComponent<VariableManagerComponent>();
            var intVar = varManagerComponent.AddNewVariableOfContentType<int>("Score", 1, AccessScope.Private);

            _holder._data.VarRef = intVar;
            EditorUtility.SetDirty(_holder);
            _serializedHolder.Update();

            float height = _drawer.GetPropertyHeight(_dataProp, new GUIContent("VariableData"));

            Assert.AreEqual(EditorGUIUtility.singleLineHeight, height, 0.01f);
        }

        // Exposes the protected static ShouldDrawLiteral method for testing, and
        // provides a concrete, instantiable drawer for GetPropertyHeight.
        private class TestableVariableDataDrawer : VariableDataDrawerBase
        {
            public static bool InvokeShouldDrawLiteral(SerializedProperty varDataProp)
            {
                return ShouldDrawLiteral(varDataProp);
            }
        }

        // Non-generic ScriptableObject holder for IntegerData (Unity cannot instantiate generic ScriptableObjects)
        [System.Serializable]
        public class IntegerDataHolder : ScriptableObject
        {
            public IntegerData _data = new IntegerData();
        }
    }
}
