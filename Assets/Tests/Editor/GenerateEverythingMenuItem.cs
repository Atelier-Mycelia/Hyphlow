using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorUtils
{
    public static class GenerateEverythingMenuItem
    {
        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Utilities/Generate Everything Flowchart")]
        public static void GenerateEverythingFlowchart()
        {
            var newGO = new GameObject("Flowchart w/ EVERYTHING");
            var flow = newGO.AddComponent<Flowchart>();

            var blockPos = Vector2.zero;
            var blockPosStep = new Vector2(0, 60);

            //adding a block for all event handlers
            foreach (var eventHandlerType in TypeCache.GetTypesWithAttribute<EventHandlerInfoAttribute>())
            {
                var block = flow.CreateBlock(blockPos);
                blockPos += blockPosStep;

                block.BlockName = eventHandlerType.Name;

                EventHandler newHandler = newGO.AddComponent(eventHandlerType) as EventHandler;
                newHandler.ParentBlock = block;
                block.EventHandler = newHandler;
            }

            //reset head
            blockPos = new Vector2(200, 0);

            //adding a block for each category, fill it with its commands
            var blockComCats = new Dictionary<string, IBlock>();
            foreach (var commandType in TypeCache.GetTypesWithAttribute<CommandInfoAttribute>())
            {
                var commandTypeAttr = commandType.GetCustomAttributes(typeof(CommandInfoAttribute), false)[0] as CommandInfoAttribute;

                blockComCats.TryGetValue(commandTypeAttr.Category, out IBlock targetBlock);
                if (targetBlock == null)
                {
                    targetBlock = flow.CreateBlock(blockPos);
                    blockPos += blockPosStep;

                    targetBlock.BlockName = commandTypeAttr.Category;
                    blockComCats[commandTypeAttr.Category] = targetBlock;
                }


                var newCommand = newGO.AddComponent(commandType) as Command;
                targetBlock.Add(newCommand, true);

            }

            //add all variable types
            foreach (var varType in TypeCache.GetTypesWithAttribute<VariableInfoAttribute>())
            {
                Variable newVariable = newGO.AddComponent(varType) as Variable;
                newVariable.Key = UniqueKeyGenerator.GetUniqueKeyFor(varType.Name, 
                    flow.Variables);
                flow.AddVariable(newVariable);
            }
        }
    }

}