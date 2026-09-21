using UnityEditor;

namespace AtMycelia.Hyphlow.EditorExt
{
    [CustomEditor (typeof(InvokeEvent))]
    public class InvokeEventEditor : CommandEditor 
    {
        protected SerializedProperty _descriptionProp;
        protected SerializedProperty _delayProp;
        protected SerializedProperty _invokeTypeProp;
        protected SerializedProperty _staticEventProp;
        protected SerializedProperty _booleanParameterProp;
        protected SerializedProperty _booleanEventProp;
        protected SerializedProperty _integerParameterProp;
        protected SerializedProperty _integerEventProp;
        protected SerializedProperty _floatParameterProp;
        protected SerializedProperty _floatEventProp;
        protected SerializedProperty _stringParameterProp;
        protected SerializedProperty _stringEventProp;

        public override void OnEnable()
        {
            base.OnEnable();

            _descriptionProp = serializedObject.FindProperty("description");
            _delayProp = serializedObject.FindProperty("delay");
            _invokeTypeProp = serializedObject.FindProperty("invokeType");
            _staticEventProp = serializedObject.FindProperty("staticEvent");
            _booleanParameterProp = serializedObject.FindProperty("booleanParameter");
            _booleanEventProp = serializedObject.FindProperty("booleanEvent");
            _integerParameterProp = serializedObject.FindProperty("integerParameter");
            _integerEventProp = serializedObject.FindProperty("integerEvent");
            _floatParameterProp = serializedObject.FindProperty("floatParameter");
            _floatEventProp = serializedObject.FindProperty("floatEvent");
            _stringParameterProp = serializedObject.FindProperty("stringParameter");
            _stringEventProp = serializedObject.FindProperty("stringEvent");
        }

        public override void DrawCommandGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_descriptionProp);
            EditorGUILayout.PropertyField(_delayProp);
            EditorGUILayout.PropertyField(_invokeTypeProp);

            switch ((InvokeType)_invokeTypeProp.enumValueIndex)
            {
            case InvokeType.Static:
                EditorGUILayout.PropertyField(_staticEventProp);
                break;
            case InvokeType.DynamicBoolean:
                EditorGUILayout.PropertyField(_booleanEventProp);
                EditorGUILayout.PropertyField(_booleanParameterProp);
                break;
            case InvokeType.DynamicInteger:
                EditorGUILayout.PropertyField(_integerEventProp);
                EditorGUILayout.PropertyField(_integerParameterProp);
                break;
            case InvokeType.DynamicFloat:
                EditorGUILayout.PropertyField(_floatEventProp);
                EditorGUILayout.PropertyField(_floatParameterProp);
                break;
            case InvokeType.DynamicString:
                EditorGUILayout.PropertyField(_stringEventProp);
                EditorGUILayout.PropertyField(_stringParameterProp);
                break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
