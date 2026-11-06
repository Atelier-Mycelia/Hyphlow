using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// Editor-side tests for <see cref="AnyVariableAndDataPairDrawer"/>, which draws a
    /// left-hand-side variable reference paired with a right-hand-side AnyVariableData
    /// whose content type automatically follows the selected variable. These tests
    /// exercise the drawer through Unity's IMGUI pipeline via a host window, similar
    /// to how VariableDataEditorTests exercises VariableDataDrawer.
    /// </summary>
    public class AnyVariableAndDataPairDrawerTests
    {
        private Flowchart _flowchart;
        private readonly WaitForSecondsRealtime _windowViewWait = new WaitForSecondsRealtime(1f);
        private readonly System.Collections.Generic.List<UnityObj> _toDestroyInTearDown = new System.Collections.Generic.List<UnityObj>();

        [SetUp]
        public void SetUp()
        {
            VariableRegistry.Rebuild();

            var fcGo = new GameObject("TestFlowchart");
            _flowchart = fcGo.AddComponent<Flowchart>();
            Selection.activeGameObject = _flowchart.gameObject;

            _toDestroyInTearDown.Add(_flowchart.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            VariableRegistry.Rebuild();
            foreach (var obj in _toDestroyInTearDown)
            {
                if (obj != null)
                {
                    if (obj is EditorWindow wnd)
                    {
                        wnd.Close();
                    }
                    else
                    {
                        UnityObj.DestroyImmediate(obj);
                    }
                }
            }
            _toDestroyInTearDown.Clear();
        }

        private AnyVariableAndDataPairDrawerHostWindow OpenHostWindow(AnyVariableAndDataPairHolder holder)
        {
            var so = new SerializedObject(holder);
            var pairProp = so.FindProperty(nameof(AnyVariableAndDataPairHolder._pair));
            Assert.IsNotNull(pairProp, "Could not find '_pair' property on holder.");

            var wnd = ScriptableObject.CreateInstance<AnyVariableAndDataPairDrawerHostWindow>();
            wnd.Drawer = new AnyVariableAndDataPairDrawer();
            wnd.SO = so;
            wnd.PairProp = pairProp;
            wnd.Show();

            _toDestroyInTearDown.Add(wnd);
            return wnd;
        }

        [UnityTest]
        public IEnumerator NoLhsVariableSelected_DoesNotThrow_AndLeavesDataUnset()
        {
            var holder = ScriptableObject.CreateInstance<AnyVariableAndDataPairHolder>();
            _toDestroyInTearDown.Add(holder);

            var wnd = OpenHostWindow(holder);

            wnd.Repaint();
            yield return null;
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
            yield return _windowViewWait;

            Assert.IsTrue(wnd.DidDraw, "Drawer should have run at least one OnGUI pass.");
            Assert.IsNull(holder._pair.Data.ContentType,
                "With no LHS variable selected, the AnyVariableData should remain unset.");
        }

        [UnityTest]
        public IEnumerator LhsVariableSelected_AdaptsDataContentTypeToMatch()
        {
            var varManagerComponent = _flowchart.GetComponent<VariableManagerComponent>();
            Assert.IsNotNull(varManagerComponent, "VariableManagerComponent not found on Flowchart.");

            var intVar = varManagerComponent.AddNewVariableOfContentType<int>("Health", 123, AccessScope.Private);
            Assert.IsNotNull(intVar, "Failed to create muscariable for test.");
            Assert.Greater(intVar.ItemId, 0, "Muscariable ItemId should be assigned.");

            _flowchart.Refresh();
            VariableRegistry.Rebuild(_flowchart);

            var holder = ScriptableObject.CreateInstance<AnyVariableAndDataPairHolder>();
            _toDestroyInTearDown.Add(holder);
            holder._pair.LhsVariable = intVar;

            var wnd = OpenHostWindow(holder);

            wnd.Repaint();
            yield return null;
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
            EditorUtility.SetDirty(holder);
            yield return null;
            yield return _windowViewWait;

            Assert.IsTrue(wnd.DidDraw, "Drawer should have run at least one OnGUI pass.");
            Assert.AreEqual(typeof(int), holder._pair.Data.ContentType,
                "AnyVariableData's content type should adapt to match the selected LHS variable's content type.");
        }

        [UnityTest]
        public IEnumerator ChangingLhsVariable_UpdatesPreviousLhsTracking()
        {
            var varManagerComponent = _flowchart.GetComponent<VariableManagerComponent>();
            var firstVar = varManagerComponent.AddNewVariableOfContentType<int>("First", 1, AccessScope.Private);
            var secondVar = varManagerComponent.AddNewVariableOfContentType<int>("Second", 2, AccessScope.Private);

            _flowchart.Refresh();
            VariableRegistry.Rebuild(_flowchart);

            var holder = ScriptableObject.CreateInstance<AnyVariableAndDataPairHolder>();
            _toDestroyInTearDown.Add(holder);
            holder._pair.LhsVariable = firstVar;

            var wnd = OpenHostWindow(holder);

            wnd.Repaint();
            yield return null;
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
            yield return _windowViewWait;

            // Now switch the LHS variable and re-draw
            holder._pair.LhsVariable = secondVar;
            wnd.SO.Update();

            wnd.Repaint();
            yield return null;
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
            yield return _windowViewWait;

            Assert.AreEqual(typeof(int), holder._pair.Data.ContentType);
            wnd.Close();
        }

        // Non-generic ScriptableObject holder (Unity cannot serialize plain classes at the root).
        [System.Serializable]
        public class AnyVariableAndDataPairHolder : ScriptableObject
        {
            public AnyVariableAndDataPair _pair = new AnyVariableAndDataPair();
        }
    }
}
