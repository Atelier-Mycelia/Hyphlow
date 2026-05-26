using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    [CustomEditor (typeof(Call))]
    public class CallEditor : CommandEditor 
    {
        protected SerializedProperty _targetFlowchartProp;
        protected SerializedProperty _targetBlockProp;
        protected SerializedProperty _startLabelProp;
        protected SerializedProperty _startIndexProp;
        protected SerializedProperty _callModeProp;

        public override void OnEnable()
        {
            base.OnEnable();

            _targetFlowchartProp = serializedObject.FindProperty("_targetFlowchart");
            _targetBlockProp = serializedObject.FindProperty("_targetBlock");
            _startLabelProp = serializedObject.FindProperty("_startLabel");
            _startIndexProp = serializedObject.FindProperty("_startIndex");
            _callModeProp = serializedObject.FindProperty("_callMode");
        }

        public override void DrawCommandGUI()
        {
            serializedObject.Update();

            Call commandTarg = target as Call;
            Flowchart commandFc = commandTarg.GetFlowchart();
            Flowchart targetFc = null;
            if (_targetFlowchartProp.objectReferenceValue == null)
            {
                targetFc = commandFc;
            }
            else
            {
                targetFc = _targetFlowchartProp.objectReferenceValue as Flowchart;
            }

            EditorGUILayout.PropertyField(_targetFlowchartProp);

            if (targetFc != null)
            {
                AccessScope scopesAllowed = ReferenceEquals(targetFc, commandFc) ?
                    AccessScope.Null : // Null = no restrictions if the target flowchart is the same as the caller's flowchart
                    AccessScopeDefaults.VisibleToOutsiders;
                BlockEditor.BlockField(_targetBlockProp,
                                       new GUIContent("Target Block", "Block to call"), 
                                       new GUIContent("<None>"), 
                                       targetFc);

                EditorGUILayout.PropertyField(_startLabelProp);

                EditorGUILayout.PropertyField(_startIndexProp);
            }

            EditorGUILayout.PropertyField(_callModeProp);

            serializedObject.ApplyModifiedProperties();
        }

    }
}
