using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// RPG Maker event-window-style replacement for the BlockInspector. Meant to be used as a
    /// floating window locked to 1280x800 or 1280x1000; docking isn't supported.
    /// </summary>
    public class BlockWindow : EditorWindow
    {
        [MenuItem("Window/Atelier Mycelia/Hyphlow/Block Window", priority = 2)]
        public static void BringUp()
        {
            BlockWindow window = GetWindow<BlockWindow>();
            window.titleContent = new GUIContent("Block Window");
            window.LockSize();
            window.Show();
            window.Focus();
        }

        private void LockSize()
        {
            Rect mainWindow = EditorGUIUtility.GetMainWindowPosition();
            // The taller layout only when it fits inside the editor with room for the title bar
            Vector2 size = mainWindow.height >= _tallSize.y + 40 ?
                _tallSize :
                _shortSize;

            minSize = size;
            maxSize = size;

            Vector2 topLeft = mainWindow.center - (size / 2f);
            position = new Rect(topLeft, size);
        }

        private static readonly Vector2 _tallSize = new Vector2(1280, 1000);
        private static readonly Vector2 _shortSize = new Vector2(1280, 800);

        /// <summary>
        /// The open BlockWindow, or null if there isn't one.
        /// </summary>
        public static BlockWindow S => _s;
        private static BlockWindow _s;

        /// <summary>
        /// Null when nothing is selected.
        /// </summary>
        public ICommand SelectedCommand => _selectedCommand;
        private ICommand _selectedCommand;

        protected virtual void OnEnable()
        {
            _s = this;
            BlockWindowSignals.Opened(this);
        }

        protected virtual void CreateGUI()
        {
            VisualTreeAsset shell = Resources.Load<VisualTreeAsset>(_shellPath);
            if (shell == null)
            {
                var warningLabel = new UitkLabel($"BlockWindow: no UXML found at Resources/{_shellPath}.");
                rootVisualElement.Add(warningLabel);
                return;
            }

            shell.CloneTree(rootVisualElement);
            _root = rootVisualElement.Q("BlockWindowRoot");
            _root.style.flexGrow = 1;

            _entryTemplate = Resources.Load<VisualTreeAsset>(_defaultEntryPath);
            _holderTemplate = Resources.Load<VisualTreeAsset>(_defaultHolderPath);

            QueryElements();
            InitFields();
            SetUpCommandList();
            ShowPreviewContent();
        }

        private const string _shellPath = "Editor/Uxml/BlockWindow/BlockWindow";
        private const string _defaultEntryPath = "Editor/Uxml/BlockWindow/DefaultCommandEntry";
        private const string _defaultHolderPath = "Editor/Uxml/BlockWindow/DefaultCommandInputHolder";

        private VisualElement _root;
        private VisualTreeAsset _entryTemplate;
        private VisualTreeAsset _holderTemplate;

        private void QueryElements()
        {
            _scopeField = _root.Q<EnumField>("ScopeField");
            _triggerFoldout = _root.Q<Foldout>("TriggerFoldout");
            _descriptionFoldout = _root.Q<Foldout>("DescriptionFoldout");
            _descriptionField = _root.Q<TextField>("DescriptionField");
            _eventHandlerButton = _root.Q<Button>("EventHandlerButton");
            _triggerHolderSlot = _root.Q("TriggerHolderSlot");
            _commandNameLabel = _root.Q<UitkLabel>("CommandNameLabel");
            _commandHelpLabel = _root.Q<UitkLabel>("CommandHelpLabel");
            _inputHolderSlot = _root.Q("InputHolderSlot");
            _commandList = _root.Q<ListView>("CommandList");
            _itemIdLabel = _root.Q<UitkLabel>("ItemIdLabel");
        }

        private EnumField _scopeField;
        private Foldout _triggerFoldout;
        private Foldout _descriptionFoldout;
        private TextField _descriptionField;
        private Button _eventHandlerButton;
        private VisualElement _triggerHolderSlot;
        private UitkLabel _commandNameLabel;
        private UitkLabel _commandHelpLabel;
        private VisualElement _inputHolderSlot;
        private ListView _commandList;
        private UitkLabel _itemIdLabel;

        private void InitFields()
        {
            _scopeField?.Init(AccessScope.Public);

            _descriptionField?.RegisterValueChangedCallback(evt => RefreshDescriptionPreview(evt.newValue));
        }

        private void RefreshDescriptionPreview(string description)
        {
            if (_descriptionFoldout == null)
            {
                return;
            }

            string firstLine = string.IsNullOrEmpty(description) ?
                "" :
                description.Split('\n')[0];
            _descriptionFoldout.text = string.IsNullOrEmpty(firstLine) ?
                "Description" :
                $"Description: {firstLine}";
        }

        private void SetUpCommandList()
        {
            if (_commandList == null)
            {
                return;
            }

            _commandList.makeItem = MakeEntry;
            _commandList.bindItem = BindEntry;
            _commandList.selectionChanged += OnSelectionChanged;
        }

        private VisualElement MakeEntry()
        {
            return _entryTemplate != null ?
                _entryTemplate.CloneTree() :
                new UitkLabel("No entry template");
        }

        private const float _indentStep = 16f;

        private void BindEntry(VisualElement entry, int index)
        {
            if (index < 0 || index >= _previewCommands.Count)
            {
                return;
            }

            PreviewCommand command = _previewCommands[index];

            SetLabel("IndexLabel", index.ToString());
            SetLabel("NameLabel", command.Name);
            entry.Q<TextField>("SummaryField")?.SetValueWithoutNotify(command.Summary);

            VisualElement indentSpacer = entry.Q("IndentSpacer");
            if (indentSpacer != null)
            {
                indentSpacer.style.width = command.IndentLevel * _indentStep;
            }

            // Only the default entry gets the solid GetButtonColor() fill; custom ones style their own
            VisualElement background = entry.Q("Background");
            if (background != null)
            {
                background.style.backgroundColor = command.Color;
            }

            VisualElement entryRoot = entry.Q("CommandEntryRoot");
            entryRoot?.EnableInClassList("command-entry--disabled", !command.IsEnabled);
            entryRoot?.EnableInClassList("command-entry--executing", command.IsExecuting);
            entryRoot?.EnableInClassList("command-entry--error", command.HasError);

            void SetLabel(string name, string text)
            {
                UitkLabel label = entry.Q<UitkLabel>(name);
                if (label != null)
                {
                    label.text = text;
                }
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            PreviewCommand selected = null;
            foreach (object item in selection)
            {
                selected = item as PreviewCommand;
                break;
            }

            ShowCommandInInspector(selected);

            ICommand previous = _selectedCommand;
            _selectedCommand = selected?.Command;
            BlockWindowSignals.CommandSelectionChanged(this, previous, _selectedCommand);
        }

        private void ShowCommandInInspector(PreviewCommand command)
        {
            _root.EnableInClassList("block-window--no-selection", command == null);
            _inputHolderSlot?.Clear();

            if (command == null)
            {
                return;
            }

            if (_commandNameLabel != null)
            {
                _commandNameLabel.text = command.Name;
            }

            if (_commandHelpLabel != null)
            {
                _commandHelpLabel.text = command.HelpText;
            }

            _inputHolderSlot?.Add(MakePlaceholderHolder($"{command.Name}'s serialized fields go here."));
        }

        private VisualElement MakePlaceholderHolder(string message)
        {
            if (_holderTemplate == null)
            {
                return new UitkLabel("No input holder template");
            }

            VisualElement holder = _holderTemplate.CloneTree();
            IMGUIContainer imgui = holder.Q<IMGUIContainer>("ImguiFields");
            if (imgui != null)
            {
                imgui.onGUIHandler = () => EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            return holder;
        }

        // OnDestroy rather than OnDisable, since the latter also runs before every domain reload
        protected virtual void OnDestroy()
        {
            if (_s == this)
            {
                _s = null;
            }

            BlockWindowSignals.Closed(this);
        }

        #region Preview
        // Stand-in content so the layout can be judged in the editor before Blocks are wired up.
        // Remove once the window binds to a real Block.

        private List<PreviewCommand> _previewCommands = new List<PreviewCommand>();

        private class PreviewCommand
        {
            /// <summary>
            /// Always null in the preview; there for the selection signal until real Commands are bound.
            /// </summary>
            public ICommand Command;
            public string Name;
            public string Summary;
            public string HelpText;
            public Color Color;
            public int IndentLevel;
            public bool IsEnabled = true;
            public bool IsExecuting;
            public bool HasError;
        }

        private void ShowPreviewContent()
        {
            _previewCommands = new List<PreviewCommand>
            {
                new PreviewCommand
                {
                    Name = "Set Variable",
                    Summary = "introSeen = true",
                    HelpText = "Sets a Boolean, Integer, Float or String variable to a new value.",
                    Color = CommandColors.Variables,
                },
                new PreviewCommand
                {
                    Name = "If",
                    Summary = "trialVersion == true",
                    HelpText = "If the test expression is true, execute the following command block.",
                    Color = CommandColors.ConditionalLogic,
                },
                new PreviewCommand
                {
                    Name = "Play Sound",
                    Summary = "Save (volume 0.9)",
                    HelpText = "Plays a once-off sound effect.",
                    Color = CommandColors.Audio,
                    IndentLevel = 1,
                },
                new PreviewCommand
                {
                    Name = "Say",
                    Summary = "This trial version has some limits:\n" +
                        "  - You can only clear the first dungeon\n" +
                        "  - Clearing it returns you to the title screen\n" +
                        "Thanks for playing!\n" +
                        "This fifth line should still show.\n" +
                        "This sixth line should be cut off.",
                    HelpText = "Writes text in a dialog box.",
                    Color = CommandColors.Narrative,
                    IndentLevel = 1,
                    IsExecuting = true,
                },
                new PreviewCommand
                {
                    Name = "Wait",
                    Summary = "2.5 seconds",
                    HelpText = "Waits for period of time before executing the next command in the block.",
                    Color = CommandColors.Flow,
                    IndentLevel = 1,
                },
                new PreviewCommand
                {
                    Name = "End",
                    Summary = "",
                    HelpText = "Marks the end of a conditional block.",
                    Color = CommandColors.ConditionalLogic,
                },
                new PreviewCommand
                {
                    Name = "Call",
                    Summary = "Error: No target block selected",
                    HelpText = "Execute another block in the same Flowchart as the command, or in a different Flowchart.",
                    Color = CommandColors.Flow,
                    HasError = true,
                },
                new PreviewCommand
                {
                    Name = "Load Scene",
                    Summary = "Town",
                    HelpText = "Loads a new Unity scene and displays an optional loading image.",
                    Color = CommandColors.Scene,
                    IsEnabled = false,
                },
            };

            _commandList.itemsSource = _previewCommands;
            _commandList.Rebuild();

            ShowCommandInInspector(null);

            if (_eventHandlerButton != null)
            {
                _eventHandlerButton.text = "Key Pressed";
            }

            if (_triggerFoldout != null)
            {
                _triggerFoldout.text = "Trigger: Key Pressed (Space)";
            }

            _triggerHolderSlot?.Add(MakePlaceholderHolder("The EventHandler's serialized fields go here."));

            _descriptionField?.SetValueWithoutNotify("Opening cutscene; sets up the intro flags.\nSecond line of the description.");
            RefreshDescriptionPreview(_descriptionField?.value);

            if (_itemIdLabel != null)
            {
                _itemIdLabel.text = "ItemId: 12";
            }
            _root.AddToClassList("block-window--debug");
        }

        #endregion
    }
}
