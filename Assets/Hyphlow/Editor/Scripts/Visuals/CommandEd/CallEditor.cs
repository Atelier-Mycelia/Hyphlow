using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorUtils
{
    [CustomEditor (typeof(Call))]
    public class CallEditor : CommandEditor 
    {
        protected SerializedProperty targetFlowchartProp;
        protected SerializedProperty targetBlockProp;
        protected SerializedProperty startLabelProp;
        protected SerializedProperty startIndexProp;
        protected SerializedProperty callModeProp;

        public override void OnEnable()
        {
            base.OnEnable();

            targetFlowchartProp = serializedObject.FindProperty("_targetFlowchart");
            targetBlockProp = serializedObject.FindProperty("_targetBlock");
            startLabelProp = serializedObject.FindProperty("_startLabel");
            startIndexProp = serializedObject.FindProperty("_startIndex");
            callModeProp = serializedObject.FindProperty("_callMode");
        }

        public override void DrawCommandGUI()
        {
            serializedObject.Update();

            Call commandTarg = target as Call;
            Flowchart commandFc = commandTarg.GetFlowchart();
            Flowchart targetFc = null;
            if (targetFlowchartProp.objectReferenceValue == null)
            {
                targetFc = commandFc;
            }
            else
            {
                targetFc = targetFlowchartProp.objectReferenceValue as Flowchart;
            }

            EditorGUILayout.PropertyField(targetFlowchartProp);

            if (targetFc != null)
            {
                AccessScope scopesAllowed = ReferenceEquals(targetFc, commandFc) ?
                    AccessScope.Null : // Null = no restrictions if the target flowchart is the same as the caller's flowchart
                    AccessScopeDefaults.VisibleToOutsiders;
                BlockEditor.BlockField(targetBlockProp,
                                       new GUIContent("Target Block", "Block to call"), 
                                       new GUIContent("<None>"), 
                                       targetFc);

                EditorGUILayout.PropertyField(startLabelProp);

                EditorGUILayout.PropertyField(startIndexProp);
            }

            EditorGUILayout.PropertyField(callModeProp);

            serializedObject.ApplyModifiedProperties();
        }

    }
}
