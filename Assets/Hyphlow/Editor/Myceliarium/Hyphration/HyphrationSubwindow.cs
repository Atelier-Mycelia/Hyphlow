using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UitkLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// The Hyphration page. It has two views: an overview (what's in the project, the
    /// Migrate button and the log of past migrations) and the picker that Migrate opens,
    /// where the user chooses which scenes and Variable Sets to migrate.
    /// </summary>
    public sealed class HyphrationSubwindow : ControlPanelSubwindow
    {
        public override string PathToUxml => "Editor/Uxml/Myceliarium/HyphrationSubwindow";

        private const string TargetItemPath = "Editor/Uxml/Myceliarium/HyphrationTargetItem";
        private const string LogItemPath = "Editor/Uxml/Myceliarium/HyphrationLogItem";
        private const string MigrationItemPath = "Editor/Uxml/Myceliarium/HyphrationMigrationItem";
        private const float ListItemHeight = 48f;
        private const float MigrationItemHeight = 38f;

        internal event Action RescanRequested = delegate { };
        internal event Action MigrateRequested = delegate { };

        /// <summary>
        /// The logic keeps the same list instances for as long as it lives, so this is
        /// called just once per initialization.
        /// </summary>
        internal void UseLogic(HyphrationLogic logic)
        {
            if (_logic != null)
            {
                _logic.Changed -= Refresh;
            }

            _logic = logic;
            if (_logic == null)
            {
                return;
            }

            _logic.Changed += Refresh;
            _scenesList.itemsSource = _logic.Scenes;
            _setsList.itemsSource = _logic.VariableSets;
            _logList.itemsSource = _logic.Log;
            Refresh();
        }

        public void ShowOverview()
        {
            _overviewView.style.display = DisplayStyle.Flex;
            _selectionView.style.display = DisplayStyle.None;
        }

        private void ShowSelection()
        {
            _overviewView.style.display = DisplayStyle.None;
            _selectionView.style.display = DisplayStyle.Flex;
        }

        public override void Show()
        {
            base.Show();
            Refresh();
        }

        // Toggles inside the lists aren't edits that the entry's undo history should hear about
        protected override IEnumerable<IFieldEditTracker> CreateEditTrackers()
        {
            yield break;
        }

        #region Visual elements
        private HyphrationLogic _logic;
        private VisualTreeAsset _targetItemTemplate;
        private VisualTreeAsset _logItemTemplate;

        private VisualElement _playModeBanner;
        private VisualElement _overviewView;
        private VisualElement _selectionView;

        private UitkLabel _scenesCountLabel;
        private UitkLabel _setsCountLabel;
        private UitkLabel _needingMigrationLabel;
        private Button _rescanButton;
        private Button _openPickerButton;

        private UitkLabel _logSummaryLabel;
        private UitkLabel _logEmptyLabel;
        private Button _clearLogButton;
        private ListView _logList;
        private UitkLabel _detailsTitleLabel;
        private UitkLabel _detailsEmptyLabel;
        private ListView _detailsList;
        private HyphrationLogEntry _selectedLogEntry;
        private VisualTreeAsset _migrationItemTemplate;
        private static readonly List<LegacyVariableConverter.Migration> _noMigrations =
            new List<LegacyVariableConverter.Migration>();

        private ListView _scenesList;
        private ListView _setsList;
        private UitkLabel _scenesEmptyLabel;
        private UitkLabel _setsEmptyLabel;
        private Button _scenesAllButton;
        private Button _scenesNoneButton;
        private Button _setsAllButton;
        private Button _setsNoneButton;
        private UitkLabel _selectionSummaryLabel;
        private Button _backButton;
        private Button _runButton;

        protected override void RegisterVisualElements()
        {
            _playModeBanner = Root.Q("PlayModeBanner");
            _overviewView = Root.Q("OverviewView");
            _selectionView = Root.Q("SelectionView");

            _scenesCountLabel = Root.Q<UitkLabel>("ScenesCountLabel");
            _setsCountLabel = Root.Q<UitkLabel>("SetsCountLabel");
            _needingMigrationLabel = Root.Q<UitkLabel>("NeedingMigrationLabel");
            _rescanButton = Root.Q<Button>("RescanButton");
            _openPickerButton = Root.Q<Button>("MigrateButton");

            _logSummaryLabel = Root.Q<UitkLabel>("LogSummaryLabel");
            _logEmptyLabel = Root.Q<UitkLabel>("LogEmptyLabel");
            _clearLogButton = Root.Q<Button>("ClearLogButton");
            _logList = Root.Q<ListView>("LogListView");
            _detailsTitleLabel = Root.Q<UitkLabel>("DetailsTitleLabel");
            _detailsEmptyLabel = Root.Q<UitkLabel>("DetailsEmptyLabel");
            _detailsList = Root.Q<ListView>("DetailsListView");

            _scenesList = Root.Q<ListView>("ScenesListView");
            _setsList = Root.Q<ListView>("SetsListView");
            _scenesEmptyLabel = Root.Q<UitkLabel>("ScenesEmptyLabel");
            _setsEmptyLabel = Root.Q<UitkLabel>("SetsEmptyLabel");
            _scenesAllButton = Root.Q<Button>("ScenesAllButton");
            _scenesNoneButton = Root.Q<Button>("ScenesNoneButton");
            _setsAllButton = Root.Q<Button>("SetsAllButton");
            _setsNoneButton = Root.Q<Button>("SetsNoneButton");
            _selectionSummaryLabel = Root.Q<UitkLabel>("SelectionSummaryLabel");
            _backButton = Root.Q<Button>("BackButton");
            _runButton = Root.Q<Button>("RunButton");

            _targetItemTemplate = Resources.Load<VisualTreeAsset>(TargetItemPath);
            _logItemTemplate = Resources.Load<VisualTreeAsset>(LogItemPath);
            _migrationItemTemplate = Resources.Load<VisualTreeAsset>(MigrationItemPath);
            if (_targetItemTemplate == null || _logItemTemplate == null ||
                _migrationItemTemplate == null)
            {
                Debug.LogError("Hyphration couldn't load its list item templates " +
                    $"('{TargetItemPath}', '{LogItemPath}' and '{MigrationItemPath}').");
            }
        }

        protected override void ConfigureVisualElements()
        {
            ConfigureList(_scenesList, MakeTargetItem,
                (element, index) => BindTargetItem(element, _logic?.Scenes, index));
            ConfigureList(_setsList, MakeTargetItem,
                (element, index) => BindTargetItem(element, _logic?.VariableSets, index));
            ConfigureList(_logList, MakeLogItem, BindLogItem);
            _logList.selectionType = SelectionType.Single; // Picks what the details pane shows

            ConfigureList(_detailsList, MakeMigrationItem, BindMigrationItem);
            _detailsList.fixedItemHeight = MigrationItemHeight;
            _detailsList.itemsSource = _noMigrations;

            ShowOverview();
        }

        private static void ConfigureList(ListView list, Func<VisualElement> makeItem,
            Action<VisualElement, int> bindItem)
        {
            list.makeItem = makeItem;
            list.bindItem = bindItem;
            list.fixedItemHeight = ListItemHeight;
            list.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            list.selectionType = SelectionType.None;
            list.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
        }
        #endregion

        #region List items
        private VisualElement MakeTargetItem()
        {
            VisualElement root = _targetItemTemplate.CloneTree();
            var row = new TargetRow(root);
            root.userData = row;

            row.SelectToggle.RegisterValueChangedCallback(evt =>
            {
                if (row.Target != null)
                {
                    _logic.SetSelected(row.Target, evt.newValue);
                }
            });

            // The whole row is a target, not just the checkbox
            root.RegisterCallback<ClickEvent>(evt =>
            {
                bool clickedToggle = evt.target is VisualElement clicked &&
                    (clicked == row.SelectToggle || row.SelectToggle.Contains(clicked));
                if (clickedToggle || row.Target == null || !row.Target.IsSelectable)
                {
                    return;
                }

                _logic.SetSelected(row.Target, !row.Target.IsSelected);
            });

            return root;
        }

        private static void BindTargetItem(VisualElement element, List<HyphrationTarget> source,
            int index)
        {
            if (element.userData is not TargetRow row)
            {
                return;
            }

            if (source == null || index < 0 || index >= source.Count)
            {
                return;
            }

            row.Bind(source[index]);
        }

        private VisualElement MakeLogItem()
        {
            VisualElement root = _logItemTemplate.CloneTree();
            root.userData = new LogRow(root);
            return root;
        }

        private void BindLogItem(VisualElement element, int index)
        {
            if (element.userData is not LogRow row || index < 0 || index >= _logic.Log.Count)
            {
                return;
            }

            row.Bind(_logic.Log[index]);
        }

        private VisualElement MakeMigrationItem()
        {
            VisualElement root = _migrationItemTemplate.CloneTree();
            root.userData = new MigrationRow(root);
            return root;
        }

        private void BindMigrationItem(VisualElement element, int index)
        {
            var shown = _detailsList.itemsSource as List<LegacyVariableConverter.Migration>;
            if (element.userData is not MigrationRow row || shown == null ||
                index < 0 || index >= shown.Count)
            {
                return;
            }

            row.Bind(shown[index]);
        }

        private sealed class MigrationRow
        {
            public MigrationRow(VisualElement root)
            {
                _keyLabel = root.Q<UitkLabel>("KeyLabel");
                _typesLabel = root.Q<UitkLabel>("TypesLabel");
            }

            private readonly UitkLabel _keyLabel;
            private readonly UitkLabel _typesLabel;

            public void Bind(LegacyVariableConverter.Migration migration)
            {
                _keyLabel.text = string.IsNullOrEmpty(migration.Source) ?
                    migration.Key :
                    $"{migration.Key}  •  Flowchart '{migration.Source}'";
                _typesLabel.text = $"{migration.FromType}  →  {migration.ToType}";
                _typesLabel.tooltip = _typesLabel.text;
            }
        }

        private sealed class TargetRow
        {
            public TargetRow(VisualElement root)
            {
                Root = root.Q("ItemRoot") ?? root;
                SelectToggle = root.Q<Toggle>("SelectToggle");
                _nameLabel = root.Q<UitkLabel>("NameLabel");
                _pathLabel = root.Q<UitkLabel>("PathLabel");
                _badge = root.Q<UitkLabel>("Badge");
            }

            public VisualElement Root { get; }
            public Toggle SelectToggle { get; }
            public HyphrationTarget Target { get; private set; }

            private readonly UitkLabel _nameLabel;
            private readonly UitkLabel _pathLabel;
            private readonly UitkLabel _badge;

            public void Bind(HyphrationTarget target)
            {
                Target = target;
                _nameLabel.text = target.DisplayName;
                _pathLabel.text = target.Path;
                _pathLabel.tooltip = target.Path;

                SelectToggle.SetValueWithoutNotify(target.IsSelected);
                SelectToggle.SetEnabled(target.IsSelectable);
                Root.EnableInClassList("hy-item--disabled", !target.IsSelectable);
                Root.EnableInClassList("hy-item--selected", target.IsSelected);

                _badge.text = BadgeText(target);
                _badge.EnableInClassList("hy-badge--ready", target.MigratableCount > 0);
                _badge.EnableInClassList("hy-badge--idle", target.MigratableCount == 0);
                _badge.EnableInClassList("hy-badge--unknown", !target.CountIsKnown);
            }

            private static string BadgeText(HyphrationTarget target)
            {
                if (!target.CountIsKnown)
                {
                    return "Checked when migrated";
                }

                if (target.MigratableCount == 0)
                {
                    return "Nothing to migrate";
                }

                return target.MigratableCount == 1 ?
                    "1 variable" :
                    $"{target.MigratableCount} variables";
            }
        }

        private sealed class LogRow
        {
            public LogRow(VisualElement root)
            {
                _root = root.Q("LogItemRoot") ?? root;
                _nameLabel = root.Q<UitkLabel>("NameLabel");
                _detailLabel = root.Q<UitkLabel>("DetailLabel");
                _countLabel = root.Q<UitkLabel>("CountLabel");
            }

            private readonly VisualElement _root;
            private readonly UitkLabel _nameLabel;
            private readonly UitkLabel _detailLabel;
            private readonly UitkLabel _countLabel;

            public void Bind(HyphrationLogEntry entry)
            {
                string kind = entry.Kind == HyphrationTargetKind.Scene ? "Scene" : "Variable Set";
                _nameLabel.text = entry.DisplayName;

                _root.EnableInClassList("hy-log-item--failed", !entry.Succeeded);
                _root.EnableInClassList("hy-log-item--done", entry.Succeeded && entry.MigratedCount > 0);
                _root.EnableInClassList("hy-log-item--noop", entry.Succeeded && entry.MigratedCount == 0);

                _detailLabel.text = entry.Succeeded ?
                    $"{kind}  •  {entry.Time:g}" :
                    $"{kind}  •  {entry.Time:g}  •  {entry.FailureMessage}";
                _detailLabel.tooltip = entry.Path;

                if (!entry.Succeeded)
                {
                    _countLabel.text = "Failed";
                }
                else if (entry.MigratedCount == 0)
                {
                    _countLabel.text = "Nothing to migrate";
                }
                else
                {
                    _countLabel.text = entry.MigratedCount == 1 ?
                        "1 migrated" :
                        $"{entry.MigratedCount} migrated";
                }
            }
        }
        #endregion

        #region Refreshing
        public override void Refresh()
        {
            if (_logic == null)
            {
                return;
            }

            _scenesCountLabel.text = _logic.Scenes.Count.ToString();
            _setsCountLabel.text = _logic.VariableSets.Count.ToString();
            _needingMigrationLabel.text = _logic.SetsNeedingMigration.ToString();

            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
            _playModeBanner.style.display = playing ? DisplayStyle.Flex : DisplayStyle.None;

            int selected = _logic.SelectedCount;
            _selectionSummaryLabel.text = selected == 0 ?
                "Nothing selected" :
                $"{_logic.SelectedSceneCount} scene(s) and {_logic.SelectedSetCount} " +
                $"Variable Set(s) selected";
            _runButton.text = selected == 0 ? "Migrate" : $"Migrate ({selected})";
            _runButton.SetEnabled(selected > 0 && !playing);
            _openPickerButton.SetEnabled(!playing);

            SetShown(_scenesEmptyLabel, _logic.Scenes.Count == 0);
            SetShown(_setsEmptyLabel, _logic.VariableSets.Count == 0);
            SetShown(_logEmptyLabel, _logic.Log.Count == 0);
            _clearLogButton.SetEnabled(_logic.Log.Count > 0);
            _logSummaryLabel.text = LogSummary();

            _scenesList.RefreshItems();
            _setsList.RefreshItems();
            _logList.RefreshItems();
            SyncLogSelection();
        }

        // New entries go in at the top, which shifts the list's selection (kept by index)
        // onto a different entry than the one the details pane is showing
        private void SyncLogSelection()
        {
            int index = _selectedLogEntry == null ? -1 : _logic.Log.IndexOf(_selectedLogEntry);
            if (index < 0)
            {
                _selectedLogEntry = null;
                _logList.SetSelectionWithoutNotify(Array.Empty<int>());
            }
            else
            {
                _logList.SetSelectionWithoutNotify(new[] { index });
            }

            UpdateDetails();
        }

        private void OnLogSelectionChanged(IEnumerable<object> selected)
        {
            _selectedLogEntry = null;
            foreach (object item in selected)
            {
                _selectedLogEntry = item as HyphrationLogEntry;
                break;
            }

            UpdateDetails();
        }

        /// <summary>
        /// Selects the newest log entry, so that its variables are what the details show.
        /// </summary>
        public void ShowLatestLogEntry()
        {
            if (_logic != null && _logic.Log.Count > 0)
            {
                _logList.SetSelection(0);
            }
        }

        private void UpdateDetails()
        {
            HyphrationLogEntry entry = _selectedLogEntry;
            string emptyMessage = null;

            if (entry == null)
            {
                emptyMessage = "Select a log entry to see which variables it migrated.";
            }
            else if (!entry.Succeeded)
            {
                emptyMessage = $"This migration failed: {entry.FailureMessage}";
            }
            else if (entry.Migrations.Count == 0)
            {
                emptyMessage = "Nothing in this one needed migrating.";
            }

            _detailsTitleLabel.text = entry == null || entry.Migrations.Count == 0 ?
                "Migrated variables" :
                $"Migrated variables ({entry.Migrations.Count})";
            _detailsEmptyLabel.text = emptyMessage ?? string.Empty;
            SetShown(_detailsEmptyLabel, emptyMessage != null);

            _detailsList.itemsSource = emptyMessage == null ?
                entry.Migrations :
                _noMigrations;
            _detailsList.RefreshItems();
        }

        private string LogSummary()
        {
            int total = 0;
            for (int i = 0; i < _logic.Log.Count; i++)
            {
                total += _logic.Log[i].MigratedCount;
            }

            return _logic.Log.Count == 0 ?
                string.Empty :
                $"{total} variable(s) migrated across {_logic.Log.Count} item(s)";
        }

        private static void SetShown(VisualElement element, bool shown)
        {
            element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }
        #endregion

        #region Subscriptions
        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            if (wantsSubsActive)
            {
                _rescanButton.clicked += RaiseRescanRequested;
                _openPickerButton.clicked += ShowSelection;
                _backButton.clicked += ShowOverview;
                _runButton.clicked += RaiseMigrateRequested;
                _clearLogButton.clicked += ClearLog;
                _scenesAllButton.clicked += SelectAllScenes;
                _scenesNoneButton.clicked += SelectNoScenes;
                _setsAllButton.clicked += SelectAllSets;
                _setsNoneButton.clicked += SelectNoSets;
                _logList.selectionChanged += OnLogSelectionChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
            else
            {
                _rescanButton.clicked -= RaiseRescanRequested;
                _openPickerButton.clicked -= ShowSelection;
                _backButton.clicked -= ShowOverview;
                _runButton.clicked -= RaiseMigrateRequested;
                _clearLogButton.clicked -= ClearLog;
                _scenesAllButton.clicked -= SelectAllScenes;
                _scenesNoneButton.clicked -= SelectNoScenes;
                _setsAllButton.clicked -= SelectAllSets;
                _setsNoneButton.clicked -= SelectNoSets;
                _logList.selectionChanged -= OnLogSelectionChanged;
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }

        private void RaiseRescanRequested()
        {
            RescanRequested();
        }

        private void RaiseMigrateRequested()
        {
            MigrateRequested();
        }

        private void ClearLog()
        {
            _logic?.ClearLog();
        }

        private void SelectAllScenes()
        {
            _logic?.SetAllSelected(HyphrationTargetKind.Scene, true);
        }

        private void SelectNoScenes()
        {
            _logic?.SetAllSelected(HyphrationTargetKind.Scene, false);
        }

        private void SelectAllSets()
        {
            _logic?.SetAllSelected(HyphrationTargetKind.VariableSet, true);
        }

        private void SelectNoSets()
        {
            _logic?.SetAllSelected(HyphrationTargetKind.VariableSet, false);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            Refresh();
        }
        #endregion

        public override void Dispose()
        {
            if (_logic != null)
            {
                _logic.Changed -= Refresh;
                _logic = null;
            }

            SetSubs(false);
            base.Dispose();
        }
    }
}
