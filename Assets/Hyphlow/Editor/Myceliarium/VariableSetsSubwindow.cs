using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Master-detail view for Variable Sets: a searchable list on the left, and the
    /// selected set's own inspector on the right. Nothing here changes the project. Edits
    /// land on the records' detached copies, and everything else is raised as a request
    /// for the entry to stage.
    /// </summary>
    public sealed class VariableSetsSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml =>
            "Editor/Uxml/Myceliarium/VariableSetsSubwindow";

        private static readonly string _itemTemplatePath =
            "Editor/Uxml/Myceliarium/VariableSetListItem";

        /// <summary>
        /// The entry's logic owns the records, so the subwindow is handed its list once
        /// that exists. The list is refilled rather than replaced, so this is called just
        /// once per initialization.
        /// </summary>
        internal void UseRecords(IReadOnlyList<VariableSetRecord> records)
        {
            _records = records;
        }

        private IReadOnlyList<VariableSetRecord> _records = Array.Empty<VariableSetRecord>();
        private readonly List<VariableSetRecord> _visibleRecords = new List<VariableSetRecord>();

        internal event Action<string> CreateRequested = delegate { };
        internal event Action<VariableSetRecord, string> RenameRequested = delegate { };
        internal event Action<VariableSetRecord> DuplicateRequested = delegate { };
        internal event Action<VariableSetRecord> DeleteRequested = delegate { };
        internal event Action<VariableSetRecord> KeepRequested = delegate { };
        internal event Action<VariableSetRecord> MoveToggled = delegate { };
        internal event Action MoveAllRequested = delegate { };

        /// <summary>
        /// Raised when the selected set's edits have made it newly different from (or the
        /// same as) what's on disk.
        /// </summary>
        internal event Action StateMayHaveChanged = delegate { };

        protected override void RegisterVisualElements()
        {
            _playModeBanner = Root.Q("PlayModeBanner");

            _createSection = Root.Q("CreateSection");
            _newSetNameField = Root.Q<TextField>("NewSetNameField");
            _createButton = Root.Q<Button>("CreateButton");
            _createHint = Root.Q<UitkLabel>("CreateHint");

            _searchField = Root.Q<ToolbarSearchField>("SearchField");
            _listView = Root.Q<ListView>("SetsListView");
            _emptyListLabel = Root.Q<UitkLabel>("EmptyListLabel");
            _summaryLabel = Root.Q<UitkLabel>("SummaryLabel");
            _moveAllButton = Root.Q<Button>("MoveAllButton");

            _emptyDetails = Root.Q("EmptyDetails");
            _details = Root.Q("Details");
            _nameField = Root.Q<TextField>("NameField");
            _nameError = Root.Q<UitkLabel>("NameError");
            _pathLabel = Root.Q<UitkLabel>("PathLabel");
            _showInProjectButton = Root.Q<Button>("ShowInProjectButton");
            _duplicateButton = Root.Q<Button>("DuplicateButton");
            _deleteButton = Root.Q<Button>("DeleteButton");
            _notice = Root.Q("Notice");
            _noticeText = Root.Q<UitkLabel>("NoticeText");
            _noticeButton = Root.Q<Button>("NoticeButton");
            _inspectorHolder = Root.Q("InspectorHolder");

            _itemTemplate = Resources.Load<VisualTreeAsset>(_itemTemplatePath);
            if (_itemTemplate == null)
            {
                Debug.LogError($"Failed to load Variable Set list item template at {_itemTemplatePath}");
            }
        }

        private VisualElement _playModeBanner;

        private VisualElement _createSection;
        private TextField _newSetNameField;
        private Button _createButton;
        private UitkLabel _createHint;

        private ToolbarSearchField _searchField;
        private ListView _listView;
        private UitkLabel _emptyListLabel;
        private UitkLabel _summaryLabel;
        private Button _moveAllButton;

        private VisualElement _emptyDetails;
        private VisualElement _details;
        private TextField _nameField;
        private UitkLabel _nameError;
        private UitkLabel _pathLabel;
        private Button _showInProjectButton;
        private Button _duplicateButton;
        private Button _deleteButton;
        private VisualElement _notice;
        private UitkLabel _noticeText;
        private Button _noticeButton;
        private VisualElement _inspectorHolder;

        private VisualTreeAsset _itemTemplate;

        protected override void ConfigureVisualElements()
        {
            _listView.makeItem = MakeItem;
            _listView.bindItem = BindItem;
            _listView.itemsSource = _visibleRecords;
            _listView.selectionType = SelectionType.Single;
            _listView.fixedItemHeight = ItemHeight;
            _listView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;

            _nameField.isDelayed = true; // So it's a rename when they're done typing, not a dozen

            _createButton.tooltip = $"New sets are saved to {VariableSetAssetOps.VsaFolder}";
            _moveAllButton.tooltip =
                $"Moves them to {VariableSetAssetOps.VsaFolder} when you save. References to them are kept.";
            _showInProjectButton.tooltip = "Highlights this set in the Project window. " +
                "(Double-clicking it in the list does the same.)";
        }

        private const float ItemHeight = 44f;

        private VisualElement MakeItem()
        {
            VisualElement root = _itemTemplate.CloneTree();
            root.style.flexGrow = 1; // Else the TemplateContainer shrinks to fit, defeating the row's height: 100%
            var row = new ItemRow(root.Q("ItemRoot"), root.Q<UitkLabel>("NameLabel"),
                root.Q<UitkLabel>("InfoLabel"), root.Q<UitkLabel>("Badge"));
            root.userData = row;
            return root;
        }

        private void BindItem(VisualElement element, int index)
        {
            if (element.userData is not ItemRow row || index < 0 || index >= _visibleRecords.Count)
            {
                return;
            }

            VariableSetRecord record = _visibleRecords[index];
            row.Name.text = record.Name;
            row.Info.text = $"{VariableCountPhrase(record.VariableCount)}  ·  {FolderPhrase(record)}";
            row.Root.EnableInClassList(DeletedItemClass, record.PendingDelete);
            element.tooltip = record.IsNew ? "Not saved yet" : record.Path;
            ApplyBadge(row.Badge, record);
        }

        private const string DeletedItemClass = "vsets-item--deleted";

        private static string VariableCountPhrase(int count)
        {
            return count == 1 ? "1 variable" : $"{count} variables";
        }

        private static string FolderPhrase(VariableSetRecord record)
        {
            string folder = record.PendingMove ? VariableSetAssetOps.VsaFolder : record.Folder;
            string phrase;

            if (folder == VariableSetAssetOps.VsaFolder)
            {
                phrase = "VSA folder";
            }
            else if (folder.StartsWith(VariableSetAssetOps.VsaFolder + "/", StringComparison.Ordinal))
            {
                phrase = "VSA/" + folder.Substring(VariableSetAssetOps.VsaFolder.Length + 1);
            }
            else
            {
                phrase = folder.StartsWith("Assets/", StringComparison.Ordinal) ?
                    folder.Substring("Assets/".Length) :
                    folder;
            }

            return record.PendingMove ? "→ " + phrase : phrase;
        }

        /// <summary>
        /// What's staged takes priority over where the set is, and the rest get no badge,
        /// so the exceptions stand out instead of every row saying the same thing. The text
        /// matters as much as the color, for those who can't easily tell the colors apart.
        /// </summary>
        private static void ApplyBadge(UitkLabel badge, VariableSetRecord record)
        {
            badge.RemoveFromClassList(BadgePendingClass);
            badge.RemoveFromClassList(BadgeDeletingClass);
            badge.RemoveFromClassList(BadgeOutsideClass);
            badge.RemoveFromClassList(BadgeReadOnlyClass);

            string text = string.Empty;
            string tooltip = string.Empty;
            string cssClass = null;

            if (record.PendingDelete)
            {
                text = "Deleting";
                tooltip = "Moves to the trash when you save.";
                cssClass = BadgeDeletingClass;
            }
            else if (record.IsNew)
            {
                text = "New";
                tooltip = "Created when you save.";
                cssClass = BadgePendingClass;
            }
            else if (record.PendingMove && record.Location == VariableSetLocation.Elsewhere)
            {
                text = "Moving";
                tooltip = $"Moves to {VariableSetAssetOps.VsaFolderDisplay} when you save.";
                cssClass = BadgePendingClass;
            }
            else if (record.IsRenamed || record.StateChanged)
            {
                text = "Edited";
                tooltip = "Has changes that are saved when you save.";
                cssClass = BadgePendingClass;
            }
            else if (record.Location == VariableSetLocation.Elsewhere)
            {
                text = "Outside";
                tooltip = $"Not in {VariableSetAssetOps.VsaFolderDisplay}. Select it to move it there.";
                cssClass = BadgeOutsideClass;
            }
            else if (record.Location == VariableSetLocation.ReadOnly)
            {
                text = "Read-only";
                tooltip = "Inside a package. Duplicate it to get an editable copy.";
                cssClass = BadgeReadOnlyClass;
            }

            badge.text = text;
            badge.tooltip = tooltip;
            if (cssClass != null)
            {
                badge.AddToClassList(cssClass);
            }

            badge.style.display = text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private const string BadgePendingClass = "vsets-badge--pending";
        private const string BadgeDeletingClass = "vsets-badge--deleting";
        private const string BadgeOutsideClass = "vsets-badge--outside";
        private const string BadgeReadOnlyClass = "vsets-badge--readonly";

        protected override void SetSubs(bool wantsSubsActive)
        {
            if (wantsSubsActive)
            {
                _createButton.clicked += OnCreateClicked;
                _newSetNameField.RegisterValueChangedCallback(OnNewSetNameChanged);
                _newSetNameField.RegisterCallback<KeyDownEvent>(OnNewSetNameKeyDown, TrickleDown.TrickleDown);
                _searchField.RegisterValueChangedCallback(OnSearchChanged);
                _listView.selectionChanged += OnListSelectionChanged;
                _listView.itemsChosen += OnItemsChosen;
                _nameField.RegisterValueChangedCallback(OnNameCommitted);
                _nameField.RegisterCallback<KeyDownEvent>(OnNameKeyDown, TrickleDown.TrickleDown);
                _showInProjectButton.clicked += OnShowInProjectClicked;
                _duplicateButton.clicked += OnDuplicateClicked;
                _deleteButton.clicked += OnDeleteClicked;
                _noticeButton.clicked += OnNoticeButtonClicked;
                _moveAllButton.clicked += OnMoveAllClicked;
                RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

                _pollItem?.Pause();
                _pollItem = schedule.Execute(PollSelectedRecord).Every(PollIntervalMs);
            }
            else
            {
                _createButton.clicked -= OnCreateClicked;
                _newSetNameField.UnregisterValueChangedCallback(OnNewSetNameChanged);
                _newSetNameField.UnregisterCallback<KeyDownEvent>(OnNewSetNameKeyDown, TrickleDown.TrickleDown);
                _searchField.UnregisterValueChangedCallback(OnSearchChanged);
                _listView.selectionChanged -= OnListSelectionChanged;
                _listView.itemsChosen -= OnItemsChosen;
                _nameField.UnregisterValueChangedCallback(OnNameCommitted);
                _nameField.UnregisterCallback<KeyDownEvent>(OnNameKeyDown, TrickleDown.TrickleDown);
                _showInProjectButton.clicked -= OnShowInProjectClicked;
                _duplicateButton.clicked -= OnDuplicateClicked;
                _deleteButton.clicked -= OnDeleteClicked;
                _noticeButton.clicked -= OnNoticeButtonClicked;
                _moveAllButton.clicked -= OnMoveAllClicked;
                UnregisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

                _pollItem?.Pause();
                _pollItem = null;
            }
        }

        #region Creating
        private void OnCreateClicked()
        {
            string name = _newSetNameField.value?.Trim();
            if (CreateNameError(name) != null)
            {
                return; // The hint is already telling them why
            }

            // Treated as a suggestion from here on, so the refresh that comes of a
            // successful create swaps in a fresh one, while a failed create leaves the
            // name there to fix.
            _lastSuggestedName = _newSetNameField.value;
            CreateRequested(name);
        }

        private string CreateNameError(string name)
        {
            return VariableSetAssetOps.ValidateName(name, _records, null, VariableSetAssetOps.VsaFolder);
        }

        private void OnNewSetNameChanged(ChangeEvent<string> evt)
        {
            RefreshCreateValidation();
        }

        private void RefreshCreateValidation()
        {
            string name = _newSetNameField.value?.Trim();
            string error = CreateNameError(name);
            bool isValid = error == null;

            _createButton.SetEnabled(isValid && !EditorApplication.isPlaying);
            _createHint.EnableInClassList(ErrorClass, !isValid);
            _createHint.text = isValid ?
                $"Created in {VariableSetAssetOps.VsaFolderDisplay} when you save." :
                error;
        }

        private const string ErrorClass = "vsets-hint--error";

        private void OnNewSetNameKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                OnCreateClicked();
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Keeps a ready-to-use name in the field, without clobbering anything the user typed.
        /// </summary>
        private void SuggestNewSetName()
        {
            string current = _newSetNameField.value;
            bool userTypedSomething = !string.IsNullOrEmpty(current) && current != _lastSuggestedName;
            if (userTypedSomething)
            {
                return;
            }

            _lastSuggestedName = VariableSetAssetOps.SuggestName(VariableSetAssetOps.DefaultName,
                _records, VariableSetAssetOps.VsaFolder);
            _newSetNameField.SetValueWithoutNotify(_lastSuggestedName);
        }

        private string _lastSuggestedName;
        #endregion

        #region List
        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            Refresh();
        }

        private void ApplyFilter()
        {
            _visibleRecords.Clear();
            string search = _searchField.value?.Trim();
            bool hasSearch = !string.IsNullOrEmpty(search);

            for (int i = 0; i < _records.Count; i++)
            {
                var record = _records[i];
                bool matches = !hasSearch ||
                    record.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (record.Path?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
                if (matches)
                {
                    _visibleRecords.Add(record);
                }
            }

            // By their staged names, so a renamed or new set lands where it'll be once saved
            _visibleRecords.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            RefreshEmptyListState(hasSearch, search);
        }

        private void RefreshEmptyListState(bool hasSearch, string search)
        {
            bool isEmpty = _visibleRecords.Count == 0;
            _listView.style.display = isEmpty ? DisplayStyle.None : DisplayStyle.Flex;
            _emptyListLabel.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;
            _emptyListLabel.text = hasSearch ?
                $"No Variable Sets match \"{search}\"." :
                "No Variable Sets yet.\nCreate one above to share variables across " +
                "every Flowchart and scene.";
        }

        private void OnListSelectionChanged(IEnumerable<object> selected)
        {
            foreach (var item in selected)
            {
                if (item is VariableSetRecord record)
                {
                    PollSelectedRecordNow(); // Last chance to catch what was edited in the previous one
                    _selectedKey = record.Key;
                    _lastSelectedIndex = _listView.selectedIndex;
                    RefreshDetails();
                }
                return;
            }
        }

        // Tracked by key since the records get rebuilt whenever the project changes
        private string _selectedKey;
        private int _lastSelectedIndex;

        internal VariableSetRecord SelectedRecord => FindRecord(_selectedKey);

        /// <summary>
        /// For when the records get replaced, and the selection needs to point at the new
        /// one. Takes effect on the next Refresh.
        /// </summary>
        internal void SelectKey(string key)
        {
            _selectedKey = key;
        }

        private void OnItemsChosen(IEnumerable<object> chosen)
        {
            OnShowInProjectClicked();
        }

        private void RestoreSelection()
        {
            bool selectionGone = FindRecord(_selectedKey) == null;
            if (selectionGone)
            {
                // Lands on whatever took the old one's place, so deleting several in a
                // row doesn't need a click in between.
                _selectedKey = _visibleRecords.Count > 0 ?
                    _visibleRecords[Mathf.Clamp(_lastSelectedIndex, 0, _visibleRecords.Count - 1)].Key :
                    null;
            }

            int index = IndexOfVisible(_selectedKey);
            if (index < 0)
            {
                _listView.SetSelectionWithoutNotify(Array.Empty<int>());
                return;
            }

            _listView.SetSelectionWithoutNotify(new[] { index });
            _lastSelectedIndex = index;
            if (_scrollToSelection)
            {
                _listView.ScrollToItem(index);
                _scrollToSelection = false;
            }
        }

        private bool _scrollToSelection;

        private VariableSetRecord FindRecord(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].Key == key)
                {
                    return _records[i];
                }
            }

            return null;
        }

        private int IndexOfVisible(string key)
        {
            for (int i = 0; i < _visibleRecords.Count; i++)
            {
                if (_visibleRecords[i].Key == key)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshSummary()
        {
            int movableCount = 0, unsavedCount = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                VariableSetRecord record = _records[i];
                unsavedCount += record.IsDirty ? 1 : 0;

                bool canBeMoved = !record.IsNew && !record.PendingDelete && !record.PendingMove &&
                    record.Location == VariableSetLocation.Elsewhere;
                movableCount += canBeMoved ? 1 : 0;
            }

            string total = _records.Count == 1 ? "1 set" : $"{_records.Count} sets";
            _summaryLabel.text = unsavedCount > 0 ? $"{total}  ·  {unsavedCount} unsaved" : total;

            _moveAllButton.style.display = movableCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _moveAllButton.text = movableCount == 1 ?
                "Move It to the VSA Folder" :
                $"Move All {movableCount} to the VSA Folder";
        }

        private void OnMoveAllClicked()
        {
            MoveAllRequested();
        }
        #endregion

        #region Details
        private void RefreshDetails()
        {
            VariableSetRecord record = SelectedRecord;
            bool hasSelection = record != null;
            _details.style.display = hasSelection ? DisplayStyle.Flex : DisplayStyle.None;
            _emptyDetails.style.display = hasSelection ? DisplayStyle.None : DisplayStyle.Flex;

            if (!hasSelection)
            {
                DestroyEmbeddedInspector();
                RefreshEditability();
                return;
            }

            // Don't yank text out from under someone mid-rename
            if (!IsEditingName())
            {
                _nameField.SetValueWithoutNotify(record.Name);
                HideNameError();
            }

            _pathLabel.text = record.IsNew ?
                $"Will be created at {VariableSetAssetOps.PathIn(record.Folder, record.Name)}" :
                record.Path;
            RefreshNotice(record);

            // Rebuilding drops focus and scroll position, which would make editing a
            // variable impossible, so it's only done for a different set
            VariableSet working = record.EnsureWorking();
            if (_inspectedSet != working)
            {
                RebuildEmbeddedInspector(working);
            }

            RefreshEditability();
        }

        private bool IsEditingName()
        {
            var focused = _nameField.panel?.focusController?.focusedElement as VisualElement;
            return focused != null && (focused == _nameField || _nameField.Contains(focused));
        }

        private void HideNameError()
        {
            _nameError.style.display = DisplayStyle.None;
        }

        private void ShowNameError(string message)
        {
            _nameError.text = message;
            _nameError.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Says what's going to happen to the set (or what could), and offers the way to
        /// take back what's staged, since there's no confirmation dialog to fall back on.
        /// </summary>
        private void RefreshNotice(VariableSetRecord record)
        {
            string text = null, buttonText = null, noticeClass = null;
            Action buttonAction = null;
            VariableSetLocation location = record.Location;

            if (record.PendingDelete)
            {
                text = "This set moves to the trash when you save.";
                buttonText = "Keep It";
                buttonAction = () => KeepRequested(record);
                noticeClass = NoticeDangerClass;
            }
            else if (record.IsNew)
            {
                text = "New set. It's created when you save.";
                noticeClass = NoticeInfoClass;
            }
            else if (record.PendingMove && location == VariableSetLocation.Elsewhere)
            {
                text = $"This set moves to {VariableSetAssetOps.VsaFolderDisplay} when you save. " +
                    "Anything that uses it keeps working.";
                buttonText = "Undo Move";
                buttonAction = () => MoveToggled(record);
                noticeClass = NoticeInfoClass;
            }
            else if (location == VariableSetLocation.Elsewhere)
            {
                text = $"This set is outside {VariableSetAssetOps.VsaFolderDisplay}. Moving it " +
                    "there keeps your shared variables in one place, and anything that " +
                    "uses this set keeps working.";
                buttonText = "Move to the VSA Folder";
                buttonAction = () => MoveToggled(record);
                noticeClass = NoticeWarningClass;
            }
            else if (location == VariableSetLocation.ReadOnly)
            {
                text = "This set is inside a package, so it can't be renamed, moved, deleted " +
                    "or edited here. Duplicate it to get an editable copy in " +
                    $"{VariableSetAssetOps.VsaFolderDisplay}.";
                noticeClass = NoticeWarningClass;
            }

            _notice.RemoveFromClassList(NoticeInfoClass);
            _notice.RemoveFromClassList(NoticeWarningClass);
            _notice.RemoveFromClassList(NoticeDangerClass);
            _notice.style.display = text != null ? DisplayStyle.Flex : DisplayStyle.None;
            _noticeText.text = text ?? string.Empty;

            if (noticeClass != null)
            {
                _notice.AddToClassList(noticeClass);
            }

            _noticeAction = buttonAction;
            _noticeButton.text = buttonText ?? string.Empty;
            _noticeButton.style.display = buttonAction != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private Action _noticeAction;

        private const string NoticeInfoClass = "vsets-notice--info";
        private const string NoticeWarningClass = "vsets-notice--warning";
        private const string NoticeDangerClass = "vsets-notice--danger";

        private void OnNoticeButtonClicked()
        {
            _noticeAction?.Invoke();
        }

        private void RebuildEmbeddedInspector(VariableSet set)
        {
            DestroyEmbeddedInspector();
            if (set == null)
            {
                return;
            }

            // Made by hand rather than through an InspectorElement so that the editor
            // (and its OnDisable cleanup) is gone the moment the selection changes.
            _embeddedEditor = Editor.CreateEditor(set);
            VisualElement gui = _embeddedEditor.CreateInspectorGUI() ??
                new IMGUIContainer(_embeddedEditor.OnInspectorGUI);
            _inspectorHolder.Add(gui);
            _inspectedSet = set;
        }

        private Editor _embeddedEditor;
        private VariableSet _inspectedSet;

        private void DestroyEmbeddedInspector()
        {
            _inspectorHolder.Clear();

            if (_embeddedEditor != null)
            {
                UnityObj.DestroyImmediate(_embeddedEditor);
            }

            _embeddedEditor = null;
            _inspectedSet = null;
        }

        /// <summary>
        /// The inspector is looking at the records' detached copies, so whoever is about to
        /// get rid of those (the entry, when the records are replaced or deleted) has to
        /// call this first. It's rebuilt on the next Refresh.
        /// </summary>
        internal void ReleaseInspector()
        {
            DestroyEmbeddedInspector();
        }

        private void OnNameCommitted(ChangeEvent<string> evt)
        {
            VariableSetRecord record = SelectedRecord;
            if (record == null)
            {
                return;
            }

            string newName = evt.newValue?.Trim();
            if (newName == record.Name)
            {
                HideNameError();
                return;
            }

            string error = VariableSetAssetOps.ValidateName(newName, _records, record, record.Folder);
            if (error != null)
            {
                ShowNameError(error + "  (Esc to undo.)");
                return;
            }

            HideNameError();
            RenameRequested(record, newName);
        }

        private void OnNameKeyDown(KeyDownEvent evt)
        {
            VariableSetRecord record = SelectedRecord;
            if (evt.keyCode != KeyCode.Escape || record == null)
            {
                return;
            }

            _nameField.SetValueWithoutNotify(record.Name);
            HideNameError();
            evt.StopPropagation();
        }

        private void OnShowInProjectClicked()
        {
            VariableSetRecord record = SelectedRecord;
            if (record?.Real == null)
            {
                return; // Not on disk yet
            }

            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(record.Real);
        }

        private void OnDuplicateClicked()
        {
            VariableSetRecord record = SelectedRecord;
            if (record != null)
            {
                PollSelectedRecordNow(); // So the copy starts from what's on screen
                DuplicateRequested(record);
            }
        }

        private void OnDeleteClicked()
        {
            VariableSetRecord record = SelectedRecord;
            if (record != null)
            {
                DeleteRequested(record);
            }
        }
        #endregion

        #region Tracking edits
        /// <summary>
        /// Edits made in the embedded inspector don't announce themselves, and the Save
        /// button (and the prompt for closing the window) depend on knowing about them.
        /// </summary>
        private void PollSelectedRecord()
        {
            if (IsVisible && !EditorApplication.isPlaying)
            {
                PollSelectedRecordNow();
            }
        }

        private void PollSelectedRecordNow()
        {
            VariableSetRecord record = SelectedRecord;
            if (record == null || !record.HasWorking || !record.RefreshStateChanged())
            {
                return;
            }

            _listView.RefreshItems();
            RefreshSummary();
            StateMayHaveChanged();
        }

        private const long PollIntervalMs = 250;
        private IVisualElementScheduledItem _pollItem;
        #endregion

        #region Play Mode
        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (panel == null)
            {
                return;
            }

            RefreshDetails();
            RefreshCreateValidation();
        }

        /// <summary>
        /// Anything done to a set in Play Mode gets reverted afterwards, so the panel goes
        /// read-only rather than let changes vanish without warning.
        /// </summary>
        private void RefreshEditability()
        {
            bool isPlaying = EditorApplication.isPlaying;
            _playModeBanner.style.display = isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            _createSection.SetEnabled(!isPlaying);
            _moveAllButton.SetEnabled(!isPlaying);

            VariableSetRecord record = SelectedRecord;
            bool hasRecord = record != null;
            bool canEdit = !isPlaying && hasRecord && !record.IsReadOnly && !record.PendingDelete;
            _nameField.SetEnabled(canEdit);
            _deleteButton.SetEnabled(canEdit);
            _noticeButton.SetEnabled(!isPlaying);
            _inspectorHolder.SetEnabled(canEdit);
            _duplicateButton.SetEnabled(!isPlaying && hasRecord && !record.PendingDelete);
            _showInProjectButton.SetEnabled(hasRecord && record.Real != null);
        }
        #endregion

        /// <summary>
        /// Called by the entry whenever the records have changed.
        /// </summary>
        public override void Refresh()
        {
            ApplyFilter();
            _listView.Rebuild();
            RestoreSelection();
            RefreshSummary();
            SuggestNewSetName();
            RefreshCreateValidation();
            RefreshDetails();
        }

        /// <summary>
        /// Refreshes, then selects (and scrolls to) the set with the given key. A search
        /// that would hide it gets cleared.
        /// </summary>
        internal void Refresh(string keyToSelect)
        {
            _selectedKey = keyToSelect;
            _scrollToSelection = true;

            ApplyFilter();
            if (IndexOfVisible(keyToSelect) < 0)
            {
                _searchField.SetValueWithoutNotify(string.Empty);
            }

            Refresh();
        }

        public override void Hide()
        {
            PollSelectedRecordNow(); // Leaving the tab means no more polling
            base.Hide();
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            // Window closing. The next Refresh rebuilds the inspector if this gets shown again.
            PollSelectedRecordNow();
            DestroyEmbeddedInspector();
        }

        public override void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            SetSubs(false);
            DestroyEmbeddedInspector();
            base.Dispose();
        }

        private sealed class ItemRow
        {
            public ItemRow(VisualElement root, UitkLabel name, UitkLabel info, UitkLabel badge)
            {
                Root = root;
                Name = name;
                Info = info;
                Badge = badge;
            }

            public VisualElement Root { get; }
            public UitkLabel Name { get; }
            public UitkLabel Info { get; }
            public UitkLabel Badge { get; }
        }
    }
}
