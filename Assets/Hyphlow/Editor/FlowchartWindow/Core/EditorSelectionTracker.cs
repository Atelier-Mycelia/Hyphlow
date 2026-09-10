using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Centralizes editor-side knowledge of which Flowchart/Blocks/Commands are currently selected.
    /// Removes legacy AmanitaState components so selection changes are tracked exclusively here.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorSelectionTracker
    {
        private const string LastSelectedFlowchartUidKey = "AtMycelia.Hyphlow.Editor.LastSelectedFlowchartUid";

        public static Flowchart ActiveFlowchart => _activeFlowchart;
        private static Flowchart _activeFlowchart;
        public static Flowchart LastActiveFlowchart
        {
            get
            {
                if (_activeFlowchart != null)
                {
                    return _activeFlowchart;
                }

                Flowchart fromSelection = FindFlowchartFromSelection();
                if (fromSelection != null)
                {
                    return fromSelection;
                }

                Flowchart basedOnCache = FindFlowchartWithCachedId();
                if (basedOnCache != null)
                {
                    return basedOnCache;
                }

                Flowchart inScene = FindFlowchartInScene();
                return inScene;
            }
        }

        private static bool HasSameUidAsCache(Flowchart fc)
        {
            return fc != null && fc.UniqueId == GetCachedFlowchartUid();
        }

        public static IReadOnlyList<IBlock> CurrentBlocks => (IReadOnlyList<IBlock>)_blockSelection;
        private static readonly IList<IBlock> _blockSelection = new List<IBlock>();
        public static IReadOnlyList<ICommand> CurrentCommands => (IReadOnlyList<ICommand>)_commandSelection;
        private static readonly IList<ICommand> _commandSelection = new List<ICommand>();

        /// <summary>
        /// The "primary" block is the first block in the selection, and is the one that will 
        /// be used for things like inspector display.
        /// </summary>
        public static IBlock PrimaryBlock { get; private set; }

        /// <summary>
        /// The "primary" command is the first command in the selection, and is the one that will 
        /// be used for things like inspector display.
        /// </summary>
        public static ICommand PrimaryCommand { get; private set; }

        public static event System.Action<IReadOnlyList<IBlock>> BlockSelectionChanged = delegate { };
        public static event System.Action<IBlock, IBlock> PrimaryBlockChanged = delegate { };
        public static event System.Action<IReadOnlyList<ICommand>> CommandSelectionChanged = delegate { };
        public static event System.Action<ICommand, ICommand> PrimaryCommandChanged = delegate { };

        static EditorSelectionTracker()
        {
            DestroyLegacyStateInstances();
            AttemptInitialHydration();
            ToggleSubs(false);
            ToggleSubs(true);
        }

        private static void SelectFlowchartBasedOnCache()
        {
            if (string.IsNullOrEmpty(GetCachedFlowchartUid()))
            {
                return;
            }
            Flowchart toSelect = FindFlowchartWithCachedId();
            if (toSelect != null)
            {
                //Debug.Log($"Selecting flowchart based on cache: {toSelect.name} (uid: {toSelect.UniqueId})");
                SetActiveFlowchart(toSelect);
            }
        }

        private static Flowchart FindFlowchartWithCachedId()
        {
            if (string.IsNullOrEmpty(GetCachedFlowchartUid()))
            {
                return null;
            }

            Flowchart[] allInScene = UnityObj.FindObjectsByType<Flowchart>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Flowchart result = allInScene.Where(HasSameUidAsCache).FirstOrDefault(IsFlowchartInAllowedContext);
            return result;
        }

        private static void DestroyLegacyStateInstances()
        {
            LegacyFungusState[] legacyStates = UnityObj.FindObjectsByType<LegacyFungusState>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (LegacyFungusState state in legacyStates)
            {
                if (state == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityObj.Destroy(state);
                }
                else
                {
                    UnityObj.DestroyImmediate(state);
                }
            }
        }

        private static void AttemptInitialHydration()
        {
            Flowchart fc = FindFlowchartFromSelection();
            if (fc != null)
            {
                SetActiveFlowchart(fc);
                return;
            }
        }

        private static Flowchart FindFlowchartFromSelection()
        {
            GameObject activeObject = Selection.activeGameObject;
            if (activeObject == null)
            {
                return null;
            }

            Flowchart selected = activeObject.GetComponent<Flowchart>();
            if (selected == null)
            {
                selected = activeObject.GetComponentInParent<Flowchart>(true);
            }

            if (!IsFlowchartInAllowedContext(selected))
            {
                return null;
            }

            return selected;
        }

        private static Flowchart FindFlowchartInScene()
        {
            Flowchart[] allInScene = UnityObj.FindObjectsByType<Flowchart>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Flowchart result = allInScene.FirstOrDefault(IsFlowchartInAllowedContext);
            return result;
        }

        private static bool IsFlowchartInAllowedContext(Flowchart flowchart)
        {
            if (flowchart == null || flowchart.gameObject == null)
            {
                return false;
            }

            if (IsFlowchartInPrefabStage(flowchart))
            {
                return true;
            }

            Scene scene = flowchart.gameObject.scene;
            return scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene);
        }

        private static bool IsFlowchartInPrefabStage(Flowchart flowchart)
        {
            if (flowchart == null || flowchart.gameObject == null)
            {
                return false;
            }
            PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(flowchart.gameObject);
            return prefabStage != null;
        }

        private static void SetActiveFlowchart(Flowchart flowchart)
        {
            if (flowchart != null && !IsFlowchartInAllowedContext(flowchart))
            {
                flowchart = null;
            }

            bool alreadySelected = ReferenceEquals(_activeFlowchart, flowchart);
            if (alreadySelected)
            {
                return;
            }

            Flowchart previous = _activeFlowchart;
            _activeFlowchart = flowchart;
            UpdateSelectionCache(flowchart);
            SyncSelectionsFromFlowchart(flowchart);
            SelectedFlowchartChanged(previous, flowchart);
        }

        private static void UpdateSelectionCache(Flowchart flowchart)
        {
            SetCachedFlowchartUid(flowchart != null ? flowchart.UniqueId : string.Empty);
        }

        private static string GetCachedFlowchartUid()
        {
            return EditorPrefs.GetString(LastSelectedFlowchartUidKey, string.Empty);
        }

        private static void SetCachedFlowchartUid(string uid)
        {
            EditorPrefs.SetString(LastSelectedFlowchartUidKey, uid ?? string.Empty);
        }

        private static void SyncSelectionsFromFlowchart(Flowchart flowchart)
        {
            SyncBlockSelectionFromFlowchart(flowchart);
            SyncCommandSelectionFromFlowchart(flowchart);
        }

        private static void SyncBlockSelectionFromFlowchart(Flowchart flowchart)
        {
            var toReplaceWith = flowchart != null ?
                flowchart.SelectedBlocks :
                null;
            ReplaceBlockSelection(toReplaceWith);
        }

        private static void ReplaceBlockSelection(IEnumerable<IBlock> toReplaceWith)
        {
            _blockSelection.Clear();
            if (toReplaceWith != null)
            {
                foreach (IBlock block in toReplaceWith)
                {
                    if (block != null)
                    {
                        _blockSelection.Add(block);
                    }
                }
            }

            IBlock previous = PrimaryBlock;
            PrimaryBlock = _blockSelection.Count > 0 ?
                _blockSelection[0] :
                null;

            BlockSelectionChanged(CurrentBlocks);
            if (!ReferenceEquals(previous, PrimaryBlock))
            {
                PrimaryBlockChanged(previous, PrimaryBlock);
            }
        }

        private static void SyncCommandSelectionFromFlowchart(Flowchart flowchart)
        {
            var toReplaceWith = flowchart != null ?
                flowchart.SelectedCommands :
                null;
            ReplaceCommandSelection(toReplaceWith);
        }

        private static void ReplaceCommandSelection(IEnumerable<ICommand> toReplaceWith)
        {
            _commandSelection.Clear();
            if (toReplaceWith != null)
            {
                foreach (ICommand cmd in toReplaceWith)
                {
                    if (cmd != null)
                    {
                        _commandSelection.Add(cmd);
                    }
                }
            }

            ICommand previous = PrimaryCommand;
            PrimaryCommand = _commandSelection.Count > 0 ?
                _commandSelection[0] :
                null;

            CommandSelectionChanged(CurrentCommands);
            if (!ReferenceEquals(previous, PrimaryCommand))
            {
                PrimaryCommandChanged(previous, PrimaryCommand);
            }
        }

        private static void ToggleSubs(bool on)
        {
            if (on)
            {
                Selection.selectionChanged += OnUnitySelectionChanged;

                FlowchartWindowSignals.EmptySpaceLeftClicked += OnEmptySpaceClicked;

                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                EditorApplication.hierarchyChanged += OnHierarchyChanged;
                PrefabStage.prefabStageOpened += OnPrefabStageOpened;
                PrefabStage.prefabStageClosing += OnPrefabStageClosing;
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
                EditorApplication.quitting += Cleanup;
            }
            else
            {
                Selection.selectionChanged -= OnUnitySelectionChanged;

                FlowchartWindowSignals.EmptySpaceLeftClicked -= OnEmptySpaceClicked;

                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.hierarchyChanged -= OnHierarchyChanged;
                PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
                PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
                AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
                EditorApplication.quitting -= Cleanup;
            }
        }

        private static void OnUnitySelectionChanged()
        {
            Flowchart flowchart = FindFlowchartFromSelection();

            if (flowchart != null)
            {
                SetActiveFlowchart(flowchart);
                return;
            }

            if (_activeFlowchart == null || !IsFlowchartInAllowedContext(_activeFlowchart))
            {
                SetActiveFlowchart(null);
            }
        }

        private static void OnEmptySpaceClicked(PointerEventInfo _)
        {
            ClearBlockSelectionInternal();
            ClearCommandSelectionInternal();
        }

        private static void ClearBlockSelectionInternal()
        {
            if (_blockSelection.Count == 0 && PrimaryBlock == null)
            {
                return;
            }

            _blockSelection.Clear();
            IBlock previous = PrimaryBlock;
            PrimaryBlock = null;

            BlockSelectionChanged(CurrentBlocks);
            if (previous != null)
            {
                PrimaryBlockChanged(previous, null);
            }
        }

        private static void ClearCommandSelectionInternal()
        {
            if (_commandSelection.Count == 0 && PrimaryCommand == null)
            {
                return;
            }

            _commandSelection.Clear();
            ICommand previous = PrimaryCommand;
            PrimaryCommand = null;

            CommandSelectionChanged(CurrentCommands);
            if (previous != null)
            {
                PrimaryCommandChanged(previous, null);
            }
        }

        /// <summary>
        /// Raised when the user selects a different Flowchart-having GameObject than before.
        /// Params: previous Flowchart, new Flowchart
        /// </summary>
        public static event System.Action<Flowchart, Flowchart> SelectedFlowchartChanged = delegate { };

        public static Flowchart ResolveActiveFlowchart(bool attemptSceneFallback = true)
        {
            if (_activeFlowchart != null && !IsFlowchartInAllowedContext(_activeFlowchart))
            {
                SetActiveFlowchart(null);
            }

            if (_activeFlowchart != null)
            {
                return _activeFlowchart;
            }

            Flowchart fromSelection = FindFlowchartFromSelection();
            if (fromSelection != null)
            {
                SetActiveFlowchart(fromSelection);
                return _activeFlowchart;
            }

            Flowchart basedOnCache = FindFlowchartWithCachedId();
            if (basedOnCache != null)
            {
                SetActiveFlowchart(basedOnCache);
                return _activeFlowchart;
            }

            if (attemptSceneFallback)
            {
                Flowchart fallback = FindFlowchartInScene();
                if (fallback != null)
                {
                    SetActiveFlowchart(fallback);
                    return _activeFlowchart;
                }
            }

            return null;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SelectFlowchartBasedOnCache();
            }
        }

        private static void OnHierarchyChanged()
        {
            ClearActiveFlowchartIfNull();
        }

        private static void OnPrefabStageOpened(PrefabStage _)
        {
            ClearActiveFlowchartIfNull();
        }

        private static void OnPrefabStageClosing(PrefabStage _)
        {
            ClearActiveFlowchartIfNull();
        }

        private static void ClearActiveFlowchartIfNull()
        {
            if (_activeFlowchart == null || !IsFlowchartInAllowedContext(_activeFlowchart))
            {
                SetActiveFlowchart(null);
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            Cleanup();
        }

        private static void Cleanup()
        {
            // Why do this check? Because in some cases (entering play mode, for example), the
            // cleanup method can be called multiple times, and we only want to run this
            // logic once per "cleanup event".
            if (_isCleaningUp)
            {
                return;
            }

            _isCleaningUp = true;

            ToggleSubs(false);
        }

        private static bool _isCleaningUp;

    }
}