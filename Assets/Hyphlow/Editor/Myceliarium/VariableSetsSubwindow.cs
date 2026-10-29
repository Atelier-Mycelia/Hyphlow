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
    /// selected set's own inspector on the right. Asset operations are raised as
    /// requests for the entry to carry out.
    /// </summary>
    public sealed class VariableSetsSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml =>
            "Editor/Uxml/Myceliarium/VariableSetsSubwindow";

        private static readonly string _itemTemplatePath =
            "Editor/Uxml/Myceliarium/VariableSetListItem";

        internal VariableSetsSubwindow(IReadOnlyList<VariableSetRecord> records)
        {
            _records = records;
        }

        private readonly IReadOnlyList<VariableSetRecord> _records;
        private readonly List<VariableSetRecord> _visibleRecords = new List<VariableSetRecord>();

        internal event Action<string> CreateRequested = delegate { };
        internal event Action<VariableSetRecord, string> RenameRequested = delegate { };
        internal event Action<VariableSetRecord> DuplicateRequested = delegate { };
        internal event Action<VariableSetRecord> DeleteRequested = delegate { };
        internal event Action<VariableSetRecord> MoveRequested = delegate { };
        internal event Action MoveAllRequested = delegate { };

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
            _locationNotice = Root.Q("LocationNotice");
            _locationNoticeText = Root.Q<UitkLabel>("LocationNoticeText");
            _moveButton = Root.Q<Button>("MoveButton");
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
        private VisualElement _locationNotice;
        private UitkLabel _locationNoticeText;
        private Button _moveButton;
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

            _nameField.isDelayed = true; // Renaming touches the disk, so not on every keystroke

            string folderTooltip = $"New sets are saved to {VariableSetAssetOps.VsaFolder}";
            _createButton.tooltip = folderTooltip;
            _moveButton.tooltip = _moveAllButton.tooltip =
                $"Moves to {VariableSetAssetOps.VsaFolder}. References to the set are kept.";
            _showInProjectButton.tooltip = "Highlights this set in the Project window. " +
                "(Double-clicking it in the list does the same.)";
        }

        private const float ItemHeight = 44f;

        private VisualElement MakeItem()
        {
            VisualElement root = _itemTemplate.CloneTree();
            root.style.flexGrow = 1; // Else the TemplateContainer shrinks to fit, defeating the row's height: 100%
            var row = new ItemRow(root.Q<UitkLabel>("NameLabel"), root.Q<UitkLabel>("InfoLabel"),
                root.Q<UitkLabel>("Badge"));
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
            element.tooltip = record.Path;
            ApplyBadge(row.Badge, record.Location);
        }

        private static string VariableCountPhrase(int count)
        {
            return count == 1 ? "1 variable" : $"{count} variables";
        }

        private static string FolderPhrase(VariableSetRecord record)
        {
            string folder = VariableSetAssetOps.FolderOf(record.Path);
            switch (record.Location)
            {
                case VariableSetLocation.VsaFolder:
                    return "VSA folder";
                case VariableSetLocation.VsaSubfolder:
                    return "VSA/" + folder.Substring(VariableSetAssetOps.VsaFolder.Length + 1);
                default:
                    return folder.StartsWith("Assets/", StringComparison.Ordinal) ?
                        folder.Substring("Assets/".Length) :
                        folder;
            }
        }

        /// <summary>
        /// Only the exceptions get badges, so they stand out instead of every row
        /// saying the same thing. The text matters as much as the color, for those who
        /// can't easily tell the colors apart.
        /// </summary>
        private static void ApplyBadge(UitkLabel badge, VariableSetLocation location)
        {
            badge.RemoveFromClassList(BadgeOutsideClass);
            badge.RemoveFromClassList(BadgeReadOnlyClass);

            switch (location)
            {
                case VariableSetLocation.Elsewhere:
                    badge.text = "Outside";
                    badge.tooltip = $"Not in {VariableSetAssetOps.VsaFolderDisplay}. " +
                        "Select it to move it there.";
                    badge.AddToClassList(BadgeOutsideClass);
                    break;
                case VariableSetLocation.ReadOnly:
                    badge.text = "Read-only";
                    badge.tooltip = "Inside a package. Duplicate it to get an editable copy.";
                    badge.AddToClassList(BadgeReadOnlyClass);
                    break;
                default:
                    badge.text = string.Empty;
                    badge.tooltip = string.Empty;
                    break;
            }

            badge.style.display = string.IsNullOrEmpty(badge.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

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
                _moveButton.clicked += OnMoveClicked;
                _moveAllButton.clicked += OnMoveAllClicked;
                RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
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
                _moveButton.clicked -= OnMoveClicked;
                _moveAllButton.clicked -= OnMoveAllClicked;
                UnregisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }

        #region Creating
        private void OnCreateClicked()
        {
            string name = _newSetNameField.value?.Trim();
            if (VariableSetAssetOps.ValidateNameForCreate(name, _records) != null)
            {
                return; // The hint is already telling them why
            }

            // Treated as a suggestion from here on, so a successful create's Refresh swaps
            // in a fresh one, while a failed create leaves the name there to fix.
            _lastSuggestedName = _newSetNameField.value;
            CreateRequested(name);
        }

        private void OnNewSetNameChanged(ChangeEvent<string> evt)
        {
            RefreshCreateValidation();
        }

        private void RefreshCreateValidation()
        {
            string name = _newSetNameField.value?.Trim();
            string error = VariableSetAssetOps.ValidateNameForCreate(name, _records);
            bool isValid = error == null;

            _createButton.SetEnabled(isValid && !EditorApplication.isPlaying);
            _createHint.EnableInClassList(ErrorClass, !isValid);
            _createHint.text = isValid ?
                $"Saved to {VariableSetAssetOps.VsaFolderDisplay}/{name}.asset" :
                error;
        }

        private const string ErrorClass = "vsets-hint--error";

        private void OnNewSetNameKeyDown(KeyDownEvent evt)
        {
            if (IsSubmitKey(evt.keyCode))
            {
                OnCreateClicked();
                evt.StopPropagation();
            }
        }

        private static bool IsSubmitKey(KeyCode key)
        {
            return key == KeyCode.Return || key == KeyCode.KeypadEnter;
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

            _lastSuggestedName = VariableSetAssetOps.SuggestName(VariableSetAssetOps.DefaultName, _records);
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
                    record.Path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                if (matches)
                {
                    _visibleRecords.Add(record);
                }
            }

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
                    _selectedGuid = record.Guid;
                    _lastSelectedIndex = _listView.selectedIndex;
                    RefreshDetails();
                }
                return;
            }
        }

        // Tracked by GUID since records get rebuilt whenever the project changes
        private string _selectedGuid;
        private int _lastSelectedIndex;

        private void OnItemsChosen(IEnumerable<object> chosen)
        {
            OnShowInProjectClicked();
        }

        private void RestoreSelection()
        {
            bool selectionGone = FindRecord(_selectedGuid) == null;
            if (selectionGone)
            {
                // Lands on whatever took the old one's place, so deleting several in a
                // row doesn't need a click in between.
                _selectedGuid = _visibleRecords.Count > 0 ?
                    _visibleRecords[Mathf.Clamp(_lastSelectedIndex, 0, _visibleRecords.Count - 1)].Guid :
                    null;
            }

            int index = IndexOfVisible(_selectedGuid);
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

        private VariableSetRecord FindRecord(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].Guid == guid)
                {
                    return _records[i];
                }
            }

            return null;
        }

        private int IndexOfVisible(string guid)
        {
            for (int i = 0; i < _visibleRecords.Count; i++)
            {
                if (_visibleRecords[i].Guid == guid)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshSummary()
        {
            int outsideCount = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].Location == VariableSetLocation.Elsewhere)
                {
                    outsideCount++;
                }
            }

            string total = _records.Count == 1 ? "1 set" : $"{_records.Count} sets";
            _summaryLabel.text = outsideCount > 0 ?
                $"{total}  ·  {outsideCount} outside the VSA folder" :
                total;

            _moveAllButton.style.display = outsideCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _moveAllButton.text = outsideCount == 1 ?
                "Move It to the VSA Folder" :
                $"Move All {outsideCount} to the VSA Folder";
        }

        private void OnMoveAllClicked()
        {
            MoveAllRequested();
        }
        #endregion

        #region Details
        private VariableSetRecord SelectedRecord => FindRecord(_selectedGuid);

        private void RefreshDetails(bool forceRebuildInspector = false)
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

            _pathLabel.text = record.Path;
            RefreshLocationNotice(record);

            // Rebuilding drops focus and scroll position, which would make editing a
            // variable impossible: saving it changes the project, which leads here.
            bool needsNewInspector = forceRebuildInspector || _inspectedSet != record.Asset;
            if (needsNewInspector)
            {
                RebuildEmbeddedInspector(record.Asset);
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

        private void RefreshLocationNotice(VariableSetRecord record)
        {
            string notice = null;
            bool canMove = false;

            switch (record.Location)
            {
                case VariableSetLocation.Elsewhere:
                    notice = $"This set is outside {VariableSetAssetOps.VsaFolderDisplay}. Moving it " +
                        "there keeps your shared variables in one place, and anything that " +
                        "uses this set keeps working.";
                    canMove = true;
                    break;
                case VariableSetLocation.ReadOnly:
                    notice = "This set is inside a package, so it can't be renamed, moved, deleted " +
                        "or edited here. Duplicate it to get an editable copy in " +
                        $"{VariableSetAssetOps.VsaFolderDisplay}.";
                    break;
            }

            _locationNotice.style.display = notice != null ? DisplayStyle.Flex : DisplayStyle.None;
            _locationNoticeText.text = notice ?? string.Empty;
            _moveButton.style.display = canMove ? DisplayStyle.Flex : DisplayStyle.None;
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
            SaveInspectedSetIfDirty();
            _inspectorHolder.Clear();

            if (_embeddedEditor != null)
            {
                UnityObj.DestroyImmediate(_embeddedEditor);
            }

            _embeddedEditor = null;
            _inspectedSet = null;
        }

        /// <summary>
        /// Some of the set's fields (the toggles, for one) only mark it dirty, which
        /// would leave the change unsaved until the project is. Never done in Play Mode,
        /// where that would make temporary values permanent.
        /// </summary>
        private void SaveInspectedSetIfDirty()
        {
            if (_inspectedSet != null && !EditorApplication.isPlaying)
            {
                AssetDatabase.SaveAssetIfDirty(_inspectedSet);
            }
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

            string error = VariableSetAssetOps.ValidateName(newName, _records, record);
            if (error != null)
            {
                ShowNameError(error + "  (Esc to undo.)");
                return;
            }

            HideNameError();
            SaveInspectedSetIfDirty(); // Renaming reimports the asset
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
            if (record?.Asset == null)
            {
                return;
            }

            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(record.Asset);
        }

        private void OnDuplicateClicked()
        {
            RaiseForSelected(DuplicateRequested);
        }

        private void RaiseForSelected(Action<VariableSetRecord> request)
        {
            VariableSetRecord record = SelectedRecord;
            if (record != null)
            {
                SaveInspectedSetIfDirty();
                request(record);
            }
        }

        private void OnDeleteClicked()
        {
            RaiseForSelected(DeleteRequested);
        }

        private void OnMoveClicked()
        {
            RaiseForSelected(MoveRequested);
        }
        #endregion

        #region Play Mode
        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (panel == null)
            {
                return;
            }

            // Leaving Play Mode restores the sets' pre-play values, which the inspector
            // wouldn't otherwise know about.
            bool valuesWereRestored = change == PlayModeStateChange.EnteredEditMode;
            RefreshDetails(forceRebuildInspector: valuesWereRestored);
            RefreshCreateValidation();
            RefreshEditability();
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
            bool canEdit = !isPlaying && record != null && !record.IsReadOnly;
            _nameField.SetEnabled(canEdit);
            _deleteButton.SetEnabled(canEdit);
            _moveButton.SetEnabled(canEdit);
            _inspectorHolder.SetEnabled(canEdit);
            _duplicateButton.SetEnabled(!isPlaying && record != null);
        }
        #endregion

        /// <summary>
        /// Called by the entry whenever the records have been reloaded.
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
        /// Refreshes, then selects (and scrolls to) the set with the given asset GUID.
        /// A search that would hide it gets cleared.
        /// </summary>
        internal void Refresh(string guidToSelect)
        {
            _selectedGuid = guidToSelect;
            _scrollToSelection = true;

            ApplyFilter();
            if (IndexOfVisible(guidToSelect) < 0)
            {
                _searchField.SetValueWithoutNotify(string.Empty);
            }

            Refresh();
        }

        public override void Hide()
        {
            SaveInspectedSetIfDirty();
            base.Hide();
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            // The next Refresh rebuilds it if this gets shown again
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
            public ItemRow(UitkLabel name, UitkLabel info, UitkLabel badge)
            {
                Name = name;
                Info = info;
                Badge = badge;
            }

            public UitkLabel Name { get; }
            public UitkLabel Info { get; }
            public UitkLabel Badge { get; }
        }
    }
}
