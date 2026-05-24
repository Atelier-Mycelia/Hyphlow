using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityObj = UnityEngine.Object;
using UnityEd = UnityEditor.Editor;

namespace AtMycelia.Hyphlow.EditorExt
{
	[CustomEditor(typeof(Block))]
	public class BlockEditor : UnityEd
	{
		public static List<Action> actionList = new List<Action>();

		public static bool SelectedBlockDataStale { get; set; }

		protected Texture2D _upIcon;
		protected Texture2D _downIcon;
		protected Texture2D _addIcon;
		protected Texture2D _duplicateIcon;
		protected Texture2D _deleteIcon;
		
		private CommandListAdaptor _commandListAdaptor;
		private SerializedProperty _commandListProperty;

		private Rect _lastEventPopupPos, _lastCMDpopupPos;

		private string _callersString;
		private bool _callersFoldout;

		protected virtual void OnEnable()
		{
			//this appears to happen when leaving playmode
			try
			{
				if (serializedObject == null)
					return;
			}
			catch (Exception)
			{
				return;
			}

			_upIcon = HyphlowEditorSysAssets.Up;
			_downIcon = HyphlowEditorSysAssets.Down;
			_addIcon = HyphlowEditorSysAssets.Add;
			_duplicateIcon = HyphlowEditorSysAssets.Duplicate;
			_deleteIcon = HyphlowEditorSysAssets.Delete;

			_commandListProperty = serializedObject.FindProperty("_commandList");
			_commandListProperty ??= serializedObject.FindProperty("_legacyCommandList");
			_commandListAdaptor = new CommandListAdaptor(target as Block, _commandListProperty);
		}

		protected void CacheCallerString()
		{
			if (!string.IsNullOrEmpty(_callersString))
			{
				return;
			}

			var targetBlock = target as Block;
			var monoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
			var callerNames = new List<string>();

			for (int i = 0; i < monoBehaviours.Length; i++)
			{
				if (monoBehaviours[i] is IBlockCaller blockCaller &&
					blockCaller.MayCallBlock(targetBlock))
				{
					callerNames.Add(blockCaller.LocationIdentifier);
				}
			}

			_callersString = callerNames.Count > 0 ? string.Join("\n", callerNames) : "None";
		}

		public virtual void DrawBlockName(Flowchart flowchart)
		{
			serializedObject.Update();

			SerializedProperty blockNameProperty = serializedObject.FindProperty("_blockName");
			// Calc position as size of what we want to draw pushed up into the top bar of the inspector
			// Rect blockLabelRect = new Rect(45, -GUI.skin.window.padding.bottom -
			// EditorGUIUtility.singleLineHeight * 2, 120, 16);
			// EditorGUI.LabelField(blockLabelRect, new GUIContent("Block Name"));
			// Rect blockNameRect = new Rect(45, blockLabelRect.y + EditorGUIUtility.singleLineHeight, 180, 16);
			// EditorGUI.PropertyField(blockNameRect, blockNameProperty, new GUIContent(""));
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(new GUIContent("Block Name"), EditorStyles.largeLabel);
			EditorGUI.BeginChangeCheck();
			blockNameProperty.stringValue = EditorGUILayout.TextField(blockNameProperty.stringValue);
			if(EditorGUI.EndChangeCheck())
			{
				// Ensure block name is unique for this Flowchart
				var block = target as Block;
				string suggestedName = blockNameProperty.stringValue;
				string uniqueName = UniqueKeyGenerator.GetUniqueKeyFor(suggestedName, flowchart.Blocks, block);
				if (uniqueName != block.BlockName)
				{
					blockNameProperty.stringValue = uniqueName;
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}

		public virtual void DrawBlockGUI(Flowchart flowchart)
		{
			serializedObject.Update();

			var block = target as Block;

			ExecuteQueuedActions();

			EditorGUI.BeginChangeCheck();

			if (ReferenceEquals(block, flowchart.SelectedBlock))
			{
				DrawSelectedBlockDetails(flowchart, block);
			}

			RemoveNullCommandEntries();

			if (EditorGUI.EndChangeCheck())
			{
				SelectedBlockDataStale = true;
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void ExecuteQueuedActions()
		{
			// Execute any queued cut, copy, paste, etc. operations from the prevous GUI update
			// We need to defer applying these operations until the following update because
			// the ReorderableList control emits GUI errors if you clear the list in the same frame
			// as drawing the control (e.g. select all and then delete)
			if (Event.current.type != EventType.Layout)
			{
				return;
			}

			for (int i = 0; i < actionList.Count; i++)
			{
				Action action = actionList[i];
				if (action != null)
				{
					action();
				}
			}
			actionList.Clear();
		}

		private void DrawSelectedBlockDetails(Flowchart flowchart, Block block)
		{
			DrawCustomTintSettings();
			DrawSaveSettings();

			EditorGUILayout.Space();

			DrawDescription();
			DrawAutoSelectionSuppression();

			DrawCallersSection();

			EditorGUILayout.Space();

			DrawEventHandlerGUI(flowchart);

			block.UpdateIndentLevels();
			EnsureCommandParentReferences(block);

			EditorGUILayout.Space();

			_commandListAdaptor.DrawCommandList();

			HandleContextMenuInput();
			HandleKeyboardShortcuts(flowchart);
		}

		private void DrawCustomTintSettings()
		{
			SerializedProperty useCustomTintProp = serializedObject.FindProperty("useCustomTint");
			SerializedProperty tintProp = serializedObject.FindProperty("tint");

			EditorGUILayout.BeginHorizontal();

			useCustomTintProp.boolValue = GUILayout.Toggle(useCustomTintProp.boolValue, " Custom Tint",
				GUILayout.Width(120));
			if (useCustomTintProp.boolValue)
			{
				EditorGUILayout.PropertyField(tintProp, GUIContent.none);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
		}

		private void DrawSaveSettings()
		{
			SerializedProperty includeInSavesProp = serializedObject.FindProperty("_includeInSaves");
			SerializedProperty loadPriorityProp = serializedObject.FindProperty("_loadPriority");

			EditorGUILayout.BeginHorizontal();
			includeInSavesProp.boolValue = GUILayout.Toggle(includeInSavesProp.boolValue, " Include in Saves",
				GUILayout.Width(150));
			EditorGUILayout.LabelField("Load Priority", GUILayout.Width(78));
			loadPriorityProp.intValue = EditorGUILayout.IntField(loadPriorityProp.intValue, GUILayout.Width(50));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
		}

		private void DrawDescription()
		{
			SerializedProperty descriptionProp = serializedObject.FindProperty("_description");
			EditorGUILayout.PropertyField(descriptionProp);
		}

		private void DrawAutoSelectionSuppression()
		{
			SerializedProperty suppressProp = serializedObject.FindProperty("suppressAllAutoSelections");
			EditorGUILayout.PropertyField(suppressProp);
		}

		private void DrawCallersSection()
		{
			EditorGUI.indentLevel++;
			if (_callersFoldout = EditorGUILayout.Foldout(_callersFoldout, "Callers"))
			{
				CacheCallerString();
				GUI.enabled = false;
				EditorGUILayout.TextArea(_callersString);
				GUI.enabled = true;
			}
			EditorGUI.indentLevel--;
		}

		private static void EnsureCommandParentReferences(Block block)
		{
			// Make sure each command has a reference to its parent block
			for (int i = 0; i < block.CommandList.Count; i++)
			{
				var command = block.CommandList[i];
				if (command == null) // Will be deleted from the list later on
				{
					continue;
				}
				command.ParentBlock = block;
			}
		}

		private void HandleContextMenuInput()
		{
			// EventType.contextClick doesn't register since we moved the Block Editor to be inside
			// a GUI Area, no idea why. As a workaround we just check for right click instead.
			if (Event.current.type == EventType.MouseUp &&
				Event.current.button == 1)
			{
				ShowContextMenu();
				Event.current.Use();
			}
		}

		private void HandleKeyboardShortcuts(Flowchart flowchart)
		{
			if (GUIUtility.keyboardControl != 0) //Only call keyboard shortcuts when not typing in a text field
			{
				return;
			}

			Event e = Event.current;

			bool ShouldHandle(EventType type, string commandName) =>
				e.type == type && e.commandName == commandName;

			void HandleValidate(string commandName, bool canExecute)
			{
				if (ShouldHandle(EventType.ValidateCommand, commandName) && canExecute)
				{
					e.Use();
				}
			}

			void HandleExecute(string commandName, Action action)
			{
				if (ShouldHandle(EventType.ExecuteCommand, commandName))
				{
					action();
					e.Use();
				}
			}

			HandleValidate("Copy", flowchart.SelectedCommandCount > 0);
			HandleExecute("Copy", () => actionList.Add(Copy));

			HandleValidate("Cut", flowchart.SelectedCommandCount > 0);
			HandleExecute("Cut", () => actionList.Add(Cut));

			HandleValidate("Paste", CommandCopyBuffer.GetInstance().HasCommands());
			HandleExecute("Paste", () => actionList.Add(Paste));

			HandleValidate("Duplicate", flowchart.SelectedCommandCount > 0);
			HandleExecute("Duplicate", () =>
			{
				actionList.Add(Copy);
				actionList.Add(Paste);
			});

			HandleValidate("Delete", flowchart.SelectedCommandCount > 0);
			HandleExecute("Delete", () => actionList.Add(Delete));

			HandleValidate("SelectAll", true);
			HandleExecute("SelectAll", () => actionList.Add(SelectAll));
		}

		private void RemoveNullCommandEntries()
		{
			// Remove any null entries in the command list.
			// This can happen when a command class is deleted or renamed.
			for (int i = _commandListProperty.arraySize - 1; i >= 0; --i)
			{
				SerializedProperty commandProperty = _commandListProperty.GetArrayElementAtIndex(i);
				if (commandProperty.objectReferenceValue == null)
				{
					_commandListProperty.DeleteArrayElementAtIndex(i);
				}
			}
		}

		public virtual void DrawButtonToolbar()
		{
			GUILayout.BeginHorizontal();


			// Previous Command
			if (Event.current.type == EventType.KeyDown && (
				  Event.current.keyCode == KeyCode.PageUp ||
				  (HyphlowEditorPreferences.navigateCmdListWithArrows && Event.current.keyCode == KeyCode.UpArrow)))
			{
				SelectPrevious();
				GUI.FocusControl("dummycontrol");
				Event.current.Use();
			}
			// Next Command
			if (Event.current.type == EventType.KeyDown && (
				  Event.current.keyCode == KeyCode.PageDown ||
				  (HyphlowEditorPreferences.navigateCmdListWithArrows && Event.current.keyCode == KeyCode.DownArrow)))
			{
				SelectNext();
				GUI.FocusControl("dummycontrol");
				Event.current.Use();
			}

			if (GUILayout.Button(_upIcon))
			{
				SelectPrevious();
			}

			// Down Button
			if (GUILayout.Button(_downIcon))
			{
				SelectNext();
			}

			GUILayout.FlexibleSpace();


			//using false to prevent forcing a longer row than will fit on smallest inspector
			var pos = EditorGUILayout.GetControlRect(false, 0, EditorStyles.objectField);
			if (pos.x != 0)
			{
				_lastCMDpopupPos = pos;
				_lastCMDpopupPos.x += EditorGUIUtility.labelWidth;
				_lastCMDpopupPos.y += EditorGUIUtility.singleLineHeight * 2;
			}
			// Add Button
			if (GUILayout.Button(_addIcon))
			{
				//this may be less reliable for HDPI scaling but previous method using editor window height is now returning 
				//  null in 2019.2 suspect ongoing ui changes, so default to screen.height and then attempt to get the better result
				int h = Screen.height;
				if (EditorWindow.focusedWindow != null) h = (int)EditorWindow.focusedWindow.position.height;
				else if (EditorWindow.mouseOverWindow != null) h = (int)EditorWindow.mouseOverWindow.position.height;

				CommandSelectorPopupWindowContent.ShowCommandMenu(_lastCMDpopupPos, "", target as Block,
					(int)(EditorGUIUtility.currentViewWidth),
					(int)(h - _lastCMDpopupPos.y));
			}

			// Duplicate Button
			if (GUILayout.Button(_duplicateIcon))
			{
				Copy();
				Paste();
			}

			// Delete Button
			if (GUILayout.Button(_deleteIcon))
			{
				Delete();
			}

			GUILayout.EndHorizontal();

		}

		protected virtual void DrawEventHandlerGUI(Flowchart flowchart)
		{
			// Show available Event Handlers in a drop down list with type of current
			// event handler selected.
			Block block = target as Block;
			System.Type currentType = null;
			if (block.EventHandler != null)
			{
				currentType = block.EventHandler.GetType();
			}

			string currentHandlerName = "<None>";
			if (currentType != null)
			{
				EventHandlerInfoAttribute info = EventHandlerEditor.GetEventHandlerInfo(currentType);
				if (info != null)
				{
					currentHandlerName = info.EventHandlerName;
				}
			}

			var pos = EditorGUILayout.GetControlRect(true, 0, EditorStyles.objectField);
			if (pos.x != 0)
			{
				_lastEventPopupPos = pos;
				_lastEventPopupPos.x += EditorGUIUtility.labelWidth;
				_lastEventPopupPos.y += EditorGUIUtility.singleLineHeight;
			}
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(new GUIContent("Execute On Event"));
			if (EditorGUILayout.DropdownButton(new GUIContent(currentHandlerName), FocusType.Passive))
			{
				EventSelectorPopupWindowContent.DoEventHandlerPopUp(_lastEventPopupPos, currentHandlerName, block, (int)(EditorGUIUtility.currentViewWidth - _lastEventPopupPos.x), 200);
			}
			EditorGUILayout.EndHorizontal();

			if (block.EventHandler != null)
			{
				EventHandlerEditor eventHandlerEditor = Editor.CreateEditor(block.EventHandler as UnityObj) 
					as EventHandlerEditor;
				if (eventHandlerEditor != null)
				{
					EditorGUI.BeginChangeCheck();
					eventHandlerEditor.DrawInspectorGUI();

					if(EditorGUI.EndChangeCheck())
					{
						SelectedBlockDataStale = true;
					}

					DestroyImmediate(eventHandlerEditor);
				}
			}
		}

		public static void BlockField(SerializedProperty property, GUIContent label, GUIContent nullLabel,
			Flowchart flowchart, AccessScope allowedScope = AccessScope.Null)
		{
			if (flowchart == null)
			{
				return;
			}

			Block block = property.objectReferenceValue as Block;

			// Build dictionary of child blocks
			List<GUIContent> blockNames = new List<GUIContent>();

			int selectedIndex = 0;
			blockNames.Add(nullLabel);
			var blocks = GetSortedBlocks(flowchart.Blocks);

			for (int i = 0; i < blocks.Count; ++i)
			{
				var currentBlock = blocks[i];
				blockNames.Add(new GUIContent(currentBlock.BlockName));
				if (ReferenceEquals(block, currentBlock))
				{
					selectedIndex = i + 1;
				}
			}

			selectedIndex = EditorGUILayout.Popup(label, selectedIndex, blockNames.ToArray());
			if (selectedIndex == 0)
			{
				block = null; // Option 'None'
			}
			else
			{
				block = blocks[selectedIndex - 1] as Block;
			}

			property.objectReferenceValue = block;
		}

		public static Block BlockField(Rect position, GUIContent nullLabel, Flowchart flowchart, Block block)
		{
			if (flowchart == null)
			{
				return null;
			}

			Block result = block;

			// Build dictionary of child blocks
			List<GUIContent> blockNames = new List<GUIContent>();

			int selectedIndex = 0;
			blockNames.Add(nullLabel);
			var blocks = GetSortedBlocks(flowchart.GetComponents<Block>());

			for (int i = 0; i < blocks.Count; ++i)
			{
				blockNames.Add(new GUIContent(blocks[i].BlockName));

				if (ReferenceEquals(block, blocks[i]))
				{
					selectedIndex = i + 1;
				}
			}

			selectedIndex = EditorGUI.Popup(position, selectedIndex, blockNames.ToArray());
			if (selectedIndex == 0)
			{
				result = null; // Option 'None'
			}
			else
			{
				result = blocks[selectedIndex - 1] as Block;
			}

			return result;
		}

		public virtual void ShowContextMenu()
		{
			var block = target as Block;
			var flowchart = (Flowchart)block.GetFlowchart();

			if (flowchart == null)
			{
				return;
			}

			bool showCut = false;
			bool showCopy = false;
			bool showDelete = false;
			bool showPaste = false;
			bool showPlay = false;

			if (flowchart.SelectedCommandCount > 0)
			{
				showCut = true;
				showCopy = true;
				showDelete = true;
				if (flowchart.SelectedCommandCount == 1 && Application.isPlaying)
				{
					showPlay = true;
				}
			}

			CommandCopyBuffer commandCopyBuffer = CommandCopyBuffer.GetInstance();

			if (commandCopyBuffer.HasCommands())
			{
				showPaste = true;
			}

			GenericMenu commandMenu = new GenericMenu();

			if (showCut)
			{
				commandMenu.AddItem(new GUIContent("Cut"), false, Cut);
			}
			else
			{
				commandMenu.AddDisabledItem(new GUIContent("Cut"));
			}

			if (showCopy)
			{
				commandMenu.AddItem(new GUIContent("Copy"), false, Copy);
			}
			else
			{
				commandMenu.AddDisabledItem(new GUIContent("Copy"));
			}

			if (showPaste)
			{
				commandMenu.AddItem(new GUIContent("Paste"), false, Paste);
			}
			else
			{
				commandMenu.AddDisabledItem(new GUIContent("Paste"));
			}

			if (showDelete)
			{
				commandMenu.AddItem(new GUIContent("Delete"), false, Delete);
			}
			else
			{
				commandMenu.AddDisabledItem(new GUIContent("Delete"));
			}

			if (showPlay)
			{
				commandMenu.AddItem(new GUIContent("Play from selected"), false, PlayCommand);
				commandMenu.AddItem(new GUIContent("Stop all and play"), false, StopAllPlayCommand);
			}

			commandMenu.AddSeparator("");

			commandMenu.AddItem(new GUIContent("Select All"), false, SelectAll);
			commandMenu.AddItem(new GUIContent("Select None"), false, SelectNone);

			commandMenu.ShowAsContext();
		}

		protected void SelectAll()
		{
			if (!TryGetSelectedBlockFlowchart(out Block block, out Flowchart flowchart))
			{
				return;
			}

			flowchart.ClearSelectedCommands();
			Undo.RecordObject(flowchart, "Select All");
			foreach (Command command in flowchart.SelectedBlock.CommandList)
			{
				flowchart.AddSelectedCommand(command);
			}

			Repaint();
		}

		protected void SelectNone()
		{
			if (!TryGetSelectedBlockFlowchart(out Block block, out Flowchart flowchart))
			{
				return;
			}

			Undo.RecordObject(flowchart, "Select None");
			flowchart.ClearSelectedCommands();

			Repaint();
		}

		protected void Cut()
		{
			Copy();
			Delete();
		}

		protected void Copy()
		{
			if (!TryGetSelectedBlockFlowchart(out Block block, out Flowchart flowchart))
			{
				return;
			}

			CommandCopyBuffer commandCopyBuffer = CommandCopyBuffer.GetInstance();
			commandCopyBuffer.Clear();

			// Scan through all commands in execution order to see if each needs to be copied
			var commandList = flowchart.SelectedBlock.CommandList;
			var selectedCommands = flowchart.SelectedCommands;
			foreach (Command command in commandList)
			{
				if (selectedCommands.Contains(command))
				{
					var type = command.GetType();
					Command newCommand = Undo.AddComponent(commandCopyBuffer.gameObject, type) as Command;
					var fields = type.GetFields(_commandBindingFlags);
					foreach (var field in fields)
					{
						// Copy all public fields
						bool copy = field.IsPublic;

						// Copy non-public fields that have the SerializeField attribute
						var attributes = field.GetCustomAttributes(_serializeFieldType, true);
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

		private static readonly BindingFlags _commandBindingFlags = BindingFlags.Instance | 
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
		private static readonly Type _serializeFieldType = typeof(SerializeField);

		protected void Paste()
		{
			if (!TryGetSelectedBlockFlowchart(out Block block, out Flowchart flowchart))
			{
				return;
			}

			CommandCopyBuffer commandCopyBuffer = CommandCopyBuffer.GetInstance();

			// Find where to paste commands in block (either at end or after last selected command)
			var commandList = flowchart.SelectedBlock.CommandList;
			var selectedCommands = flowchart.SelectedCommands;
			int pasteIndex = commandList.Count;
			if (flowchart.SelectedCommandCount > 0)
			{
				for (int i = 0; i < commandList.Count; ++i)
				{
					ICommand command = commandList[i];

					foreach (ICommand selectedCommand in selectedCommands)
					{
						if (command == selectedCommand)
						{
							pasteIndex = i + 1;
						}
					}
				}
			}

			foreach (Command command in commandCopyBuffer.GetCommands())
			{
				// Using the Editor copy / paste functionality instead instead of reflection
				// because this does a deep copy of the command properties.
				if (ComponentUtility.CopyComponent(command))
				{
					if (ComponentUtility.PasteComponentAsNew(flowchart.gameObject))
					{
						Command[] commands = flowchart.GetComponents<Command>();
						Command pastedCommand = commands.Length > 0 ? 
							commands[commands.Length - 1] : 
							null;
						if (pastedCommand != null)
						{
							flowchart.SelectedBlock.Add(pastedCommand, true);
							flowchart.SelectedBlock.CommandList.Insert(pasteIndex++, pastedCommand);
						}
					}

					// This stops the user pasting the command manually into another game object.
					ComponentUtility.CopyComponent(flowchart.transform);
				}
			}

			// Because this is an async call, we need to force prefab instances to record changes
			PrefabUtility.RecordPrefabInstancePropertyModifications(block);

			Repaint();
		}

		protected void Delete()
		{
			if (!TryGetSelectedBlockFlowchart(out Block block, out Flowchart flowchart))
			{
				return;
			}

			int indexOfCmdBefore = -1, indexOfCmdAfter = -1;
			// ^So we can select the next (or previous) command after deletion for better UX.
			int lastSelectedIndex = 0;
			var commandList = flowchart.SelectedBlock.CommandList;
			for (int i = commandList.Count - 1; i >= 0; --i)
			{
				ICommand command = commandList[i];
				foreach (ICommand selectedCommand in flowchart.SelectedCommands)
				{
					if (command == selectedCommand)
					{
						command.OnCommandRemoved(block);

						// Order of destruction is important here for undo to work
						Undo.DestroyObjectImmediate(selectedCommand as UnityObj);

						Undo.RecordObject(flowchart.SelectedBlock as UnityObj, "Delete");
						indexOfCmdBefore = i - 1;
						indexOfCmdAfter = i;
						commandList.RemoveAt(i);

						lastSelectedIndex = i;

						break;
					}
				}
			}

			Undo.RecordObject(flowchart, "Delete");
			flowchart.ClearSelectedCommands();

			if (indexOfCmdAfter < commandList.Count)
			{
				var nextCommand = commandList[indexOfCmdAfter];
				flowchart.AddSelectedCommand(nextCommand);
			}
			else if (indexOfCmdBefore >= 0)
			{
				var previousCommand = commandList[indexOfCmdBefore];
				flowchart.AddSelectedCommand(previousCommand);
			}

			Repaint();
		}

		private bool TryGetSelectedBlockFlowchart(out Block block, out Flowchart flowchart)
		{
			block = target as Block;
			flowchart = block != null ? 
				block.GetFlowchart() : 
				null;

			return flowchart != null && flowchart.SelectedBlock != null;
		}

		protected void PlayCommand()
		{
			var targetBlock = target as Block;
			var flowchart = targetBlock.GetFlowchart();
			ICommand command = flowchart.SelectedCommands[0];
			if (targetBlock.IsExecuting)
			{
				// The Block is already executing.
				// Tell the Block to stop, wait a little while so the executing command has a 
				// chance to stop, and then start execution again from the new command. 
				targetBlock.Stop();
				flowchart.StartCoroutine(RunBlock(flowchart, targetBlock, command.CommandIndex, 0.2f));
			}
			else
			{
				// Block isn't executing yet so can start it now.
				flowchart.ExecuteBlock(targetBlock, command.CommandIndex);
			}
		}

		protected void StopAllPlayCommand()
		{
			var targetBlock = target as Block;
			var flowchart = targetBlock.GetFlowchart();
			ICommand command = flowchart.SelectedCommands[0];

			// Stop all active blocks then run the selected block.
			flowchart.StopAllBlocks();
			flowchart.StartCoroutine(RunBlock(flowchart, targetBlock, command.CommandIndex, 0.2f));
		}

		protected IEnumerator RunBlock(Flowchart flowchart, Block targetBlock, int commandIndex, float delay)
		{
			yield return new WaitForSeconds(delay);
			flowchart.ExecuteBlock(targetBlock, commandIndex);
		}

		protected void SelectPrevious()
		{
			var block = target as Block;
			var flowchart = block.GetFlowchart();

			int firstSelectedIndex = flowchart.SelectedBlock.CommandList.Count;
			bool firstSelectedCommandFound = false;
			if (flowchart.SelectedCommandCount > 0)
			{
				for (int i = 0; i < flowchart.SelectedBlock.CommandList.Count; i++)
				{
					ICommand commandInBlock = flowchart.SelectedBlock.CommandList[i];

					foreach (ICommand selectedCommand in flowchart.SelectedCommands)
					{
						if (commandInBlock == selectedCommand)
						{
							if (!firstSelectedCommandFound)
							{
								firstSelectedIndex = i;
								firstSelectedCommandFound = true;
								break;
							}
						}
					}
					if (firstSelectedCommandFound)
					{
						break;
					}
				}
			}
			if (firstSelectedIndex > 0)
			{
				flowchart.ClearSelectedCommands();
				flowchart.AddSelectedCommand(flowchart.SelectedBlock.CommandList[firstSelectedIndex - 1]);
			}

			Repaint();
		}

		protected void SelectNext()
		{
			var block = target as Block;
			var flowchart = (Flowchart)block.GetFlowchart();
			var commandList = flowchart.SelectedBlock.CommandList;
			int lastSelectedIndex = -1;
			if (flowchart.SelectedCommandCount > 0)
			{
				for (int i = 0; i < commandList.Count; i++)
				{
					ICommand commandInBlock = commandList[i];

					foreach (ICommand selectedCommand in flowchart.SelectedCommands)
					{
						if (commandInBlock == selectedCommand)
						{
							lastSelectedIndex = i;
						}
					}
				}
			}
			if (lastSelectedIndex < commandList.Count - 1)
			{
				flowchart.ClearSelectedCommands();
				flowchart.AddSelectedCommand(commandList[lastSelectedIndex + 1]);
			}

			Repaint();
		}

		private static List<IBlock> GetSortedBlocks(IList<IBlock> blocks, AccessScope allowedScopes)
		{
			bool includeAllBlocks = allowedScopes == AccessScope.Null;
			var sortedBlocks = new List<IBlock>(blocks.Count);
			for (int i = 0; i < blocks.Count; i++)
			{
				if (includeAllBlocks || allowedScopes.HasFlag(blocks[i].Scope))
				{
					sortedBlocks.Add(blocks[i]);
				}
			}

			SortBlocksByName(sortedBlocks);
			return sortedBlocks;
		}

		private static IList<IBlock> GetSortedBlocks(IReadOnlyList<IBlock> blocks)
		{
			var sortedBlocks = new List<IBlock>(blocks.Count);
			for (int i = 0; i < blocks.Count; i++)
			{
				sortedBlocks.Add(blocks[i]);
			}

			SortBlocksByName(sortedBlocks);
			return sortedBlocks;
		}

		private static void SortBlocksByName(List<IBlock> blocks)
		{
			blocks.Sort(CompareBlocksByName);
		}

		private static int CompareBlocksByName(IBlock left, IBlock right)
		{
			return string.Compare(left.BlockName, right.BlockName, StringComparison.Ordinal);
		}

		public static IList<KeyValuePair<Type, CommandInfoAttribute>> GetFilteredCommandInfoAttribute(IList<Type> menuTypes)
		{
			Dictionary<string, KeyValuePair<Type, CommandInfoAttribute>> filteredAttributes = new Dictionary<string, KeyValuePair<Type, CommandInfoAttribute>>();

			foreach (Type type in menuTypes)
			{
				object[] attributes = type.GetCustomAttributes(false);
				foreach (object obj in attributes)
				{
					CommandInfoAttribute infoAttr = obj as CommandInfoAttribute;
					if (infoAttr != null)
					{
						string dictionaryName = string.Format("{0}/{1}", infoAttr.Category, infoAttr.CommandName);

						int existingItemPriority = -1;
						if (filteredAttributes.ContainsKey(dictionaryName))
						{
							existingItemPriority = filteredAttributes[dictionaryName].Value.Priority;
						}

						if (infoAttr.Priority > existingItemPriority)
						{
							KeyValuePair<Type, CommandInfoAttribute> keyValuePair = new KeyValuePair<Type, CommandInfoAttribute>(type, infoAttr);
							filteredAttributes[dictionaryName] = keyValuePair;
						}
					}
				}
			}

			return new List<KeyValuePair<Type, CommandInfoAttribute>>(filteredAttributes.Values);
		}

		// Compare delegate for sorting the list of command attributes
		public static int CompareCommandAttributes(KeyValuePair<Type, CommandInfoAttribute> x, KeyValuePair<Type, CommandInfoAttribute> y)
		{
			int compare = (x.Value.Category.CompareTo(y.Value.Category));
			if (compare == 0)
			{
				compare = (x.Value.CommandName.CompareTo(y.Value.CommandName));
			}
			return compare;
		}
	}
}
