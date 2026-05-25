using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    public class CommandListAdaptor
    {
        public CommandListAdaptor(Block _block, SerializedProperty arrayProperty)
        {
            Validate();
            void Validate()
            {
                if (arrayProperty == null)
                    throw new ArgumentNullException("Array property was null.");
                if (!arrayProperty.isArray)
                    throw new InvalidOperationException("Specified serialized propery is not an array.");
            }

            this._arrayProperty = arrayProperty;
            this._block = _block;

            _list = new ReorderableList(arrayProperty.serializedObject, arrayProperty,
                draggable: true, displayHeader: true,
                displayAddButton: false, displayRemoveButton: false);

            HookUpCallbacks();
            void HookUpCallbacks()
            {
                _list.drawHeaderCallback = DrawHeader;
                _list.drawElementCallback = DrawItem;
                _list.onSelectCallback = SelectChanged;
            }

            _list.elementHeight = EditorGUIUtility.singleLineHeight + _lineHeightPadding;
        }

        protected SerializedProperty _arrayProperty;
        protected IBlock _block;
        protected ReorderableList _list;
        protected static readonly int _lineHeightPadding = 6;
        private bool _suppressSelectCallback;

        protected virtual void DrawHeader(Rect rect)
        {
            if (rect.width < 0) return;
            EditorGUI.LabelField(rect, new GUIContent("Commands"));
        }

        public virtual void DrawItem(Rect position, int index, bool selected, bool focused)
        {
            // Before we can start drawing visual elements, we need to gather up info
            // about how we're going to go about it. Hence the prep phase here.
            SerializedProperty prop;
            Command command;
            Flowchart flowchart = null;
            IList<Rect> indentRects = null;
            Rect labelRect = default, summaryRect = default, iconRect = default, clickRect = default;
            string commandName = string.Empty;
            bool exitEarly = false;
            PrepPhase();
            void PrepPhase()
            {
                prop = _arrayProperty.GetArrayElementAtIndex(index);
                command = prop.objectReferenceValue as Command;

                if (command != null)
                {
                    flowchart = command.GetFlowchart();
                }

                if (command == null || flowchart == null)
                {
                    exitEarly = true;
                    return;
                }

                commandName = BuildCommandNameLabel(flowchart, command);

                ComputeNeededRects();
                void ComputeNeededRects()
                {
                    indentRects = CalculateIndentRects(position, command.IndentLevel);
                    labelRect = CalculateLabelRect(position, command.IndentLevel);
                    summaryRect = CalculateSummaryRect(labelRect, commandName);

                    iconRect = CalculateIconRect(labelRect, command);
                    clickRect = position;  // covers entire row
                }
            }
            
            if (exitEarly)
            {
                return;
            }

            DrawTheVisuals();
            void DrawTheVisuals()
            {
                foreach (var elem in indentRects) // For If-else Commands and such
                    GUI.Box(elem, "", _commandLabelStyle);

                Color bgColor = DetermineBackgroundColor(flowchart, command);
                GUI.backgroundColor = bgColor;
                GUI.Label(labelRect, commandName, _commandLabelStyle);
                GUI.Label(summaryRect, command.GetSummary() ?? "", _summaryStyle);

                DrawExecutingIcon(iconRect, command);
            }

            HandleCommandSelection(clickRect, index, command, flowchart);

            HandleScrollingToCommandOnDraw();
            void HandleScrollingToCommandOnDraw()
            {
                if (Event.current.type != EventType.Repaint)
                {
                    return;
                }

                foreach (Command selectedCommand in flowchart.SelectedCommands)
                {
                    if (selectedCommand.ItemId == command.ItemId)
                    {
                        if (ScrollToCommandOnDraw)
                        {
                            GUI.ScrollTo(position);
                            ScrollToCommandOnDraw = false;
                        }
                        break;
                    }
                }
            }

            // Reverting colors so other drawers can do their thing properly
            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
        }

        protected virtual List<Rect> CalculateIndentRects(Rect row, int level)
        {
            var result = new List<Rect>();

            for (int i = 0; i < level; i++)
            {
                var currentIndentRect = row;
                float howFarToIndent = i * _indentSize;
                // ^Can vary depending on how far we're nesting the relevant Commands
                currentIndentRect.x += howFarToIndent;
                currentIndentRect.width = _indentSize + _indentPadding;
                currentIndentRect.y -= _yDownwardOffset;
                currentIndentRect.height += _heightPadding;
                result.Add(currentIndentRect);
            }

            return result;
        }

        protected static readonly float _indentSize = 20, _indentPadding = 1,
            _yDownwardOffset = 2, _heightPadding = 5;

        protected virtual Rect CalculateLabelRect(Rect row, int level)
        {
            Rect result = row;
            float howFarToIndent = level * _indentSize;
            result.x += howFarToIndent;
            result.y -= _yDownwardOffset;
            result.width -= howFarToIndent;
            result.height += _heightPadding;
            return result;
        }

        protected virtual Rect CalculateSummaryRect(Rect labelRect, string commandName)
        {
            Rect result = labelRect;
            result.x += _summaryRectXOffset;
            return result;
        }

        protected static readonly float _summaryRectXOffset = 100;

        protected virtual Rect CalculateIconRect(Rect labelRect, Command command)
        {
            Rect result = labelRect;
            result.x += result.width - _iconWidth - 5;
            result.width = result.height = _iconWidth;
            result.y += (labelRect.height - result.height) * 0.25f;
            return result;
        }

        protected virtual void DrawExecutingIcon(Rect iconRect, Command cmd)
        {
            if (iconRect == Rect.zero || !ShouldDrawExecutingIcon(cmd))
            {
                return;
            }

            float alpha = 1f;
            if (!cmd.IsExecuting)
            {
                float timeRemaining = cmd.ExecutionIconTimer - Time.realtimeSinceStartup;
                alpha = Mathf.Clamp01(timeRemaining / HyphlowConstants.ExecutingIconFadeTime);
            }

            var prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(iconRect, HyphlowEditorSysAssets.PlaySmall, ScaleMode.ScaleToFit, true);
            GUI.color = prevColor;
        }

        protected virtual bool ShouldDrawExecutingIcon(Command cmd)
        {
            if (cmd == null)
            {
                return false;
            }

            return cmd.IsExecuting || cmd.ExecutionIconTimer > Time.realtimeSinceStartup;
        }

        protected static readonly int _iconWidth = 20;

        protected virtual Color DetermineBackgroundColor(Flowchart flowchart, Command cmd)
        {
            Color result = cmd.GetButtonColor();

            if (flowchart.SelectedCommands.Contains(cmd))
            {
                result = _selectedCmdColor;
            }

            if (!cmd.enabled)
            {
                result = _disabledCmdColor;
            }

            return result;
        }

        protected static readonly Color _disabledCmdColor = Color.grey,
            _selectedCmdColor = Color.green;

        protected virtual string BuildCommandNameLabel(Flowchart f, Command cmd)
        {
            // Get all CommandInfoAttributes on this type (won�t throw)
            var infos = cmd.GetType()
                           .GetCustomAttributes(typeof(CommandInfoAttribute), inherit: false)
                           .OfType<CommandInfoAttribute>();

            // Pick the first available or fall back to the GameObject�s name
            string baseName = infos
                .Select(attr => attr.CommandName)
                .FirstOrDefault()
                ?? cmd.name;

            int lastSlashIndex = baseName.LastIndexOf("/");
            bool needTrim = lastSlashIndex != -1;
            if (needTrim)
            {
                baseName = baseName.Substring(lastSlashIndex + 1);
            }
            FlowchartEditorQol editorQol = f.EditorQol;
            return editorQol != null && editorQol.ShowLineNumbers
                ? $"{cmd.CommandIndex}: {baseName}"
                : baseName;
        }

        /// <summary>
        /// If true, scrolls to the currently selected command in the inspector when the editor is redrawn. A
        /// Automatically resets to false.
        /// </summary>
        public static bool ScrollToCommandOnDraw = false;

        public virtual void DrawCommandList()
        {
            
            if (_summaryStyle == null)
            {
                _summaryStyle = new GUIStyle();
                _summaryStyle.fontSize = 10;
                _summaryStyle.padding.top += 5;
                _summaryStyle.richText = true;
                _summaryStyle.wordWrap = false;
                _summaryStyle.clipping = TextClipping.Clip;
            }

            if (_commandLabelStyle == null)
            {
                _commandLabelStyle = new GUIStyle(GUI.skin.label);
                _commandLabelStyle.normal.background = HyphlowEditorSysAssets.CommandBackground;
                _commandLabelStyle.normal.textColor = Color.black;
                int borderSize = 5;
                _commandLabelStyle.border.top = borderSize;
                _commandLabelStyle.border.bottom = borderSize;
                _commandLabelStyle.border.left = borderSize;
                _commandLabelStyle.border.right = borderSize;
                _commandLabelStyle.alignment = TextAnchor.MiddleLeft;
                _commandLabelStyle.richText = true;
                _commandLabelStyle.fontSize = 11;
                _commandLabelStyle.padding.top -= 1;
                _commandLabelStyle.alignment = TextAnchor.MiddleLeft;
            }

            if (_block.CommandList.Count == 0)
            {
                if (!HyphlowEditorPreferences.suppressHelpBoxes)
                {
                    EditorGUILayout.HelpBox("Press the + button below to add a command to the list.", MessageType.Info); 
                }
            }
            else
            {
                EditorGUI.indentLevel++;
                _list.DoLayoutList();
                EditorGUI.indentLevel--;
            }
        }

        protected GUIStyle _summaryStyle, _commandLabelStyle;

        public float fixedItemHeight;

        public virtual SerializedProperty this[int index]
        {
            get { return _arrayProperty.GetArrayElementAtIndex(index); }
        }

        public virtual SerializedProperty ArrayProperty
        {
            get { return _arrayProperty; }
        }

        private void SelectChanged(ReorderableList list)
        {
            if (_suppressSelectCallback)
            {
                _suppressSelectCallback = false;
                return;
            }

            Event currentEvent = Event.current;
            if (currentEvent != null && currentEvent.type == EventType.MouseDown)
            {
                return;
            }

            Command command = this[list.index].objectReferenceValue as Command;
            if (command == null)
            {
                return;
            }

            Flowchart flowchart = command.GetFlowchart();
            if (flowchart == null)
            {
                return;
            }

            BlockEditor.actionList.Add(delegate
            {
                flowchart.ClearSelectedCommands();
                flowchart.AddSelectedCommand(command);
            });
        }

        private void HandleCommandSelection(Rect clickRect, int index, Command command, Flowchart flowchart)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null ||
                currentEvent.type != EventType.MouseDown ||
                currentEvent.button != 0 ||
                !clickRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            bool shift = currentEvent.shift;
            bool actionKey = EditorGUI.actionKey;
            bool alreadySelected = flowchart.SelectedCommands.Contains(command);

            _suppressSelectCallback = true;
            _list.index = index;

            BlockEditor.actionList.Add(delegate
            {
                if (actionKey)
                {
                    IList<ICommand> newSelection = flowchart.SelectedCommands;
                    if (alreadySelected)
                    {
                        newSelection.Remove(command);
                    }
                    else
                    {
                        newSelection.Add(command);
                    }

                    flowchart.SelectedCommands = newSelection;
                    return;
                }

                if (shift)
                {
                    if (!alreadySelected)
                    {
                        flowchart.AddSelectedCommand(command);
                    }

                    SelectCommandRange(flowchart, index);
                    return;
                }

                flowchart.ClearSelectedCommands();
                flowchart.AddSelectedCommand(command);
            });

            if (shift || actionKey)
            {
                currentEvent.Use();
            }

            GUIUtility.keyboardControl = 0; // Fix for textarea not refreshing (change focus)
        }

        private void SelectCommandRange(Flowchart flowchart, int currentIndex)
        {
            if (flowchart == null || flowchart.SelectedBlock == null)
            {
                return;
            }

            IList<ICommand> selectedCommands = flowchart.SelectedCommands;
            IList<ICommand> commandList = flowchart.SelectedBlock.CommandList;

            int firstSelectedIndex = -1;
            int lastSelectedIndex = -1;

            for (int i = 0; i < commandList.Count; i++)
            {
                if (selectedCommands.Contains(commandList[i]))
                {
                    firstSelectedIndex = i;
                    break;
                }
            }

            for (int i = commandList.Count - 1; i >= 0; i--)
            {
                if (selectedCommands.Contains(commandList[i]))
                {
                    lastSelectedIndex = i;
                    break;
                }
            }

            if (firstSelectedIndex == -1 || lastSelectedIndex == -1)
            {
                firstSelectedIndex = 0;
                lastSelectedIndex = currentIndex;
            }
            else
            {
                if (currentIndex < firstSelectedIndex)
                {
                    firstSelectedIndex = currentIndex;
                }
                if (currentIndex > lastSelectedIndex)
                {
                    lastSelectedIndex = currentIndex;
                }
            }

            int start = Mathf.Min(firstSelectedIndex, lastSelectedIndex);
            int end = Mathf.Max(firstSelectedIndex, lastSelectedIndex);

            for (int i = start; i < end; i++)
            {
                ICommand selectedCommand = commandList[i];
                flowchart.AddSelectedCommand(selectedCommand);
            }
        }
    }
}
