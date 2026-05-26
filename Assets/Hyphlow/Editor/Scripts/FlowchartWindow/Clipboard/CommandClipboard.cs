using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Clipboard for copying and pasting Flowchart commands. Stores copies of the selected 
    /// commands in a temporary GameObject.
    /// </summary>
    public class CommandClipboard
    {
        public virtual bool HasCommands()
        {
            return CommandCopyBuffer.GetInstance().HasCommands();
        }

        public virtual void CopySelectedCommands(Flowchart flowchart)
        {
            if (flowchart == null || flowchart.SelectedBlock == null)
            {
                return;
            }

            CommandCopyBuffer commandCopyBuffer = CommandCopyBuffer.GetInstance();
            commandCopyBuffer.Clear();

            IList<ICommand> commandList = flowchart.SelectedBlock.CommandList;
            for (int i = 0; i < commandList.Count; i++)
            {
                Command command = commandList[i] as Command;
                if (command == null)
                {
                    continue;
                }

                if (flowchart.SelectedCommands.Contains(command))
                {
                    System.Type type = command.GetType();
                    Command newCommand = Undo.AddComponent(commandCopyBuffer.gameObject, type) as Command;
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                    for (int j = 0; j < fields.Length; j++)
                    {
                        FieldInfo field = fields[j];
                        bool copy = field.IsPublic;

                        object[] attributes = field.GetCustomAttributes(typeof(SerializeField), true);
                        if (attributes.Length > 0)
                        {
                            copy = true;
                        }

                        if (copy)
                        {
                            field.SetValue(newCommand, field.GetValue(command));
                        }
                    }
                }
            }
        }

        public virtual void CutSelectedCommands(Flowchart flowchart)
        {
            CopySelectedCommands(flowchart);
            DeleteSelectedCommands(flowchart);
        }

        public virtual void DeleteSelectedCommands(Flowchart flowchart)
        {
            if (flowchart == null || flowchart.SelectedBlock == null)
            {
                return;
            }

            IBlock block = flowchart.SelectedBlock;
            int lastSelectedIndex = 0;
            for (int i = block.CommandList.Count - 1; i >= 0; --i)
            {
                ICommand command = block.CommandList[i];
                IList<ICommand> selectedCommands = flowchart.SelectedCommands;
                for (int j = 0; j < selectedCommands.Count; j++)
                {
                    ICommand selectedCommand = selectedCommands[j];
                    if (command == selectedCommand)
                    {
                        command.OnCommandRemoved(block);
                        Undo.DestroyObjectImmediate(command as Command);

                        Undo.RecordObject(block as Block, "Delete");
                        block.CommandList.RemoveAt(i);

                        lastSelectedIndex = i;
                        break;
                    }
                }
            }

            Undo.RecordObject(flowchart, "Delete");
            flowchart.ClearSelectedCommands();

            if (lastSelectedIndex < block.CommandList.Count)
            {
                ICommand nextCommand = block.CommandList[lastSelectedIndex];
                flowchart.AddSelectedCommand(nextCommand);
            }
        }
    }
}