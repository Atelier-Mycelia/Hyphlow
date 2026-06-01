using UnityEngine;
using AtMycelia.Hyphlow.EditorExt.FcWindow;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Custom drawer for the BlockReference, allows for more easily selecting a target block in external c#
    /// scripts.
    /// </summary>
    [CustomPropertyDrawer(typeof(BlockReference))]
    public class BlockReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty blockRefProp, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, blockRefProp);

            blockRefProp.serializedObject.Update();

            UnityObj targetObject = blockRefProp.serializedObject.targetObject;
            SerializedProperty itemIdProp = blockRefProp.FindPropertyRelative("_itemId");
            SerializedProperty owningSourceProp = blockRefProp.FindPropertyRelative("_owningSource");

            if (itemIdProp == null || owningSourceProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Block reference fields not found.");
                EditorGUI.EndProperty();
                return;
            }

            FlowchartRegistry.EnsureInitialized();

            Flowchart localFlowchart = GetLocalFlowchart(targetObject);
            IReadOnlyList<Flowchart> flowcharts = FlowchartRegistry.GetFlowcharts();

            List<IBlock> candidates = new List<IBlock>();
            List<string> options = new List<string> { "<None>" };
            RegisterBlocksIn(localFlowchart);
            void RegisterBlocksIn(Flowchart blockSource)
            {
                IReadOnlyList<IBlock> blocks = blockSource.Blocks;
                for (int j = 0; j < blocks.Count; j++)
                {
                    IBlock block = blocks[j];
                    if (block == null)
                    {
                        continue;
                    }

                    string blockName = TruncatedAsNeeded(block.BlockName);
                    string fcName = TruncatedAsNeeded(blockSource.name);

                    bool isLocal = ReferenceEquals(blockSource, localFlowchart);
                    bool visibleToOutsiders = (block.Scope & AccessScopeDefaults.VisibleToOutsiders) > 0;
                    if (!isLocal && !visibleToOutsiders)
                    {
                        continue;
                    }
                    string optionLabel = isLocal ?
                        $"this/{blockName}" :
                        $"[{fcName}]/{blockName}";
                    // ^We put "this" here to reduce potential clutter in the popup menu.
                    // It's also closer to C# terminology, which may make it more intuitive for
                    // users doing custom scripting alongside their Hyphlow usage.
                    candidates.Add(block);
                    options.Add(optionLabel);
                }
            }
            for (int i = 0; i < flowcharts.Count; i++)
            {
                Flowchart flowchart = flowcharts[i];
                if (flowchart == null || flowchart == localFlowchart)
                {
                    continue;
                }

                RegisterBlocksIn(flowchart);
            }

            int currentItemId = itemIdProp.intValue;
            UnityObj storedOwner = owningSourceProp.objectReferenceValue;

            int currentIndex = 0;
            bool validId = currentItemId != Block.InvalidId;
            if (validId)
            {
                int found = candidates.FindIndex(IsBlockWithRightIdAndOwner);
                if (found >= 0)
                {
                    currentIndex = found + 1;
                }
            }

            bool IsBlockWithRightIdAndOwner(IBlock block)
            {
                bool rightId = block != null && block.ItemId == currentItemId;
                if (!rightId)
                {
                    return false;
                }

                if (storedOwner == null)
                {
                    return true;
                }

                bool rightOwner = ReferenceEquals(block.ParentFlowchart as UnityObj, storedOwner);
                return rightId && rightOwner;
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                if (targetObject != null)
                {
                    Undo.RecordObject(targetObject, "Set Block Reference");
                }

                if (newIndex == 0)
                {
                    itemIdProp.intValue = Block.InvalidId;
                    owningSourceProp.objectReferenceValue = null;
                }
                else
                {
                    IBlock chosen = candidates[newIndex - 1];
                    itemIdProp.intValue = chosen.ItemId;
                    owningSourceProp.objectReferenceValue = chosen.ParentFlowchart as UnityObj;
                }

                blockRefProp.serializedObject.ApplyModifiedProperties();

                if (targetObject != null && PrefabUtility.IsPartOfPrefabInstance(targetObject))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(targetObject);
                }

                if (targetObject != null)
                {
                    EditorUtility.SetDirty(targetObject);
                }

                GameObject go = null;
                if (targetObject is GameObject)
                {
                    go = targetObject as GameObject;
                }
                else if (targetObject is Component)
                {
                    go = (targetObject as Component).gameObject;
                }

                PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(go);
                if (prefabStage != null)
                {
                    EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                }
            }

            EditorGUI.EndProperty();
        }

        private static string TruncatedAsNeeded(string toConsiderTruncating)
        {
            if (toConsiderTruncating.Length <= _maxCharsPerName)
            {
                return toConsiderTruncating;
            }
            int maxIndex = _maxCharsPerName - _truncationSuffix.Length;
            string result = toConsiderTruncating.Substring(0, maxIndex) + _truncationSuffix;
            return result;
        }

        private static readonly int _maxCharsPerName = 18;
        private static readonly string _truncationSuffix = "...";

        private static Flowchart GetLocalFlowchart(UnityObj targetObject)
        {
            if (targetObject is Component)
            {
                Component component = targetObject as Component;
                return component.GetComponent<Flowchart>();
            }

            if (targetObject is GameObject)
            {
                GameObject gameObject = targetObject as GameObject;
                if (gameObject.TryGetComponent(out Flowchart foundFlowchart))
                {
                    return foundFlowchart;
                }
            }

            if (!Application.isPlaying)
            {
                GameObject activeGo = Selection.activeGameObject;
                if (activeGo != null && activeGo.TryGetComponent(out Flowchart selectedFlowchart))
                {
                    return selectedFlowchart;
                }

                FlowchartWindow fcWindow = FlowchartWindow.S;
                if (fcWindow != null)
                {
                    return fcWindow.Flowchart;
                }
            }

            return null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        
    }
}