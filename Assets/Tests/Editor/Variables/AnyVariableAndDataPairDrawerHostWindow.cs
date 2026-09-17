using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using UnityEditor;
using UnityEngine;

namespace VScriptingTests.VariableOperations
{
    // Testing-only window to exercise AnyVariableAndDataPairDrawer via Unity's IMGUI pipeline,
    // mirroring the pattern used by VariableReferenceDrawerHostWindow.
    public class AnyVariableAndDataPairDrawerHostWindow : EditorWindow
    {
        internal AnyVariableAndDataPairDrawer Drawer;
        internal SerializedObject SO;
        internal SerializedProperty PairProp;
        internal GUIContent Label = new GUIContent("Any Variable And Data Pair");
        internal bool DidDraw;

        private void OnGUI()
        {
            if (Drawer == null || SO == null || PairProp == null)
                return;

            // SO.targetObject can become a destroyed (fake-null) Unity object between
            // test yields/teardown, in which case SO.Update() would throw/log an error.
            if (SO.targetObject == null)
                return;

            SO.Update();

            float height = EditorGUIUtility.singleLineHeight * 2f;
            var rect = new Rect(4, 4, position.width - 8, height);
            Drawer.OnGUI(rect, PairProp, Label);
            DidDraw = true;

            if (SO.hasModifiedProperties)
            {
                SO.ApplyModifiedProperties();
            }
        }
    }
}
