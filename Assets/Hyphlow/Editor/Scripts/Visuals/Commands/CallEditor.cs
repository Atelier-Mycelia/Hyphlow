using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    [CustomEditor (typeof(Call))]
    public class CallEditor : CommandEditor 
    {
        public override void OnEnable()
        {
            base.OnEnable();

            _targetBlockRefProp = serializedObject.FindProperty("_targetBlockReference");
            _targetFlowchartProp = serializedObject.FindProperty("_targetFlowchart");
            _targetBlockProp = serializedObject.FindProperty("_targetBlock");
            _startLabelProp = serializedObject.FindProperty("_startLabel");
            _startIndexProp = serializedObject.FindProperty("_startIndex");
            _callModeProp = serializedObject.FindProperty("_callMode");
        }

        protected SerializedProperty _targetFlowchartProp;
        protected SerializedProperty _targetBlockProp;
        protected SerializedProperty _startLabelProp;
        protected SerializedProperty _startIndexProp;
        protected SerializedProperty _callModeProp;
        protected SerializedProperty _targetBlockRefProp;

        public override void DrawCommandGUI()
        {
            serializedObject.Update();

            Call commandTarg = target as Call;
            Flowchart commandFc = commandTarg.ParentBlock.ParentFlowchart;
            Flowchart targetFc;
            if (_targetFlowchartProp.objectReferenceValue == null)
            {
                targetFc = commandFc;
            }
            else
            {
                targetFc = _targetFlowchartProp.objectReferenceValue as Flowchart;
            }

            if (targetFc != null)
            {
                EditorGUILayout.PropertyField(_targetBlockRefProp);
                EditorGUILayout.PropertyField(_startLabelProp);

                EditorGUILayout.PropertyField(_startIndexProp);
            }

            EditorGUILayout.PropertyField(_callModeProp);

            serializedObject.ApplyModifiedProperties();
        }

    }
}
