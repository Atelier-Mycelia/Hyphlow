using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Central authority for creating, showing, and clearing the hidden BlockInspector ScriptableObject.
    /// Automatically reacts to Flowchart/Block selection signals so every editor surface
    /// stays in sync without relying on FlowchartWindow’s static field directly.
    /// </summary>
    [InitializeOnLoad]
    public static class BlockInspectorManager
    {
        private const bool _debugSelectionRouting = false;

        static BlockInspectorManager()
        {
            ListenForEvents();
        }

        private static void ListenForEvents()
        {
            BlockSignals.BlockSelected += OnBlockSelected;
            BlockSignals.BlockDeselected += OnBlockDEselected;
            BlockSignals.MultiBlocksSelected += OnMultiBlocksSelected;

            FlowchartWindowSignals.EmptySpaceLeftClicked += OnEmptySpaceLeftClicked;
            FlowchartWindowSignals.ChangedFlowchart += OnFlowchartChanged;

            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            AssemblyReloadEvents.beforeAssemblyReload += DisposeInspector;
            EditorApplication.quitting += DisposeInspector;
        }

        private static void OnBlockSelected(IBlock block)
        {
            Show(block);
        }

        public static void Show(IBlock block)
        {
            LogSelection(nameof(Show), $"Requested show for block: {DescribeBlock(block)}");

            if (block == null)
            {
                LogSelection(nameof(Show), "Incoming block is null -> Clear()");
                Clear();
                return;
            }

            Flowchart flowchart = block.GetFlowchart();
            if (flowchart == null)
            {
                LogSelection(nameof(Show), "Block has no Flowchart -> ignoring");
                return;
            }

            TrackedFlowchart = flowchart;
            ShowInspectorFor(flowchart, block);
        }

        public static void Clear()
        {
            ClearInternal(trackedFlowchart);
        }

        private static Flowchart trackedFlowchart;

        /// <summary>
        /// Clears the BlockInspector and resets selection state.
        /// </summary>
        private static void ClearInternal(Flowchart flowchart)
        {
            bool hiddenInspectorWasSelected =
                inspectorInstance != null && Selection.activeObject == inspectorInstance;

            if (inspectorInstance != null)
            {
                inspectorInstance._block = null;
            }

            if (flowchart != null)
            {
                flowchart.ClearSelectedCommands();

                if (flowchart.gameObject != null)
                {
                    Selection.activeObject = flowchart.gameObject;
                }
            }
            else if (hiddenInspectorWasSelected)
            {
                // Recover from blank inspector state by selecting something visible.
                if (Selection.activeGameObject != null)
                {
                    Selection.activeObject = Selection.activeGameObject;
                }
                else
                {
                    Selection.activeObject = null;
                }
            }

            lastShownBlock = null;
            InspectorTargetChanged(null);
        }

        private static BlockInspector inspectorInstance;
        private static IBlock lastShownBlock;
        public static event Action<IBlock> InspectorTargetChanged = delegate { };


        private static Flowchart TrackedFlowchart
        {
            set => trackedFlowchart = value;
        }

        private static void ShowInspectorFor(Flowchart flowchart, IBlock block)
        {
            LogSelection(nameof(ShowInspectorFor), $"flowchart={(flowchart != null ? flowchart.name : "null")}, block={DescribeBlock(block)}");

            if (flowchart == null || block == null)
            {
                LogSelection(nameof(ShowInspectorFor), "Null flowchart or block -> return");
                return;
            }

            bool blockBelongsToFlowchart = ReferenceEquals(block.GetFlowchart(), flowchart);
            if (!blockBelongsToFlowchart)
            {
                LogSelection(nameof(ShowInspectorFor), "Block belongs to different flowchart");
            }

            BlockInspector inspector = EnsureInspector();
            bool inspectorAlreadyShowing = ReferenceEquals(inspector._block, block);

            if (!inspectorAlreadyShowing)
            {
                flowchart.ClearSelectedCommands();
                inspector._block = block as Block;

                if (block.ActiveCommand != null)
                {
                    flowchart.AddSelectedCommand(block.ActiveCommand);
                }
            }

            if (Selection.activeObject != inspector)
            {
                LogSelection(nameof(ShowInspectorFor), "Switching Selection.activeObject -> BlockInspector");
                Selection.activeObject = inspector;
            }

            lastShownBlock = block;
            InspectorTargetChanged(block);
        }

        private static BlockInspector EnsureInspector()
        {
            if (inspectorInstance == null)
            {
                inspectorInstance = ScriptableObject.CreateInstance<BlockInspector>();
                inspectorInstance.hideFlags = HideFlags.DontSave;
                EditorUtility.SetDirty(inspectorInstance);
            }

            return inspectorInstance;
        }

        public static Flowchart CurrentFlowchart => trackedFlowchart;

        public static BlockInspector Inspector => EnsureInspector();

        public static IBlock LastShownBlock => lastShownBlock;

        private static void OnBlockDEselected(IBlock block)
        {
            Flowchart flowchart = block != null ? 
                block.GetFlowchart() : 
                trackedFlowchart;
            if (flowchart == null)
            {
                Clear();
                return;
            }

            if (flowchart.SelectedBlockCount == 0)
            {
                ClearInternal(flowchart);
                return;
            }

            IBlock selectedBlock = GetPrimarySelectedBlock(flowchart);
            if (selectedBlock != null)
            {
                ShowInspectorFor(flowchart, selectedBlock);
            }
            else
            {
                ClearInternal(flowchart);
            }
        }

        private static IBlock GetPrimarySelectedBlock(Flowchart flowchart)
        {
            if (flowchart == null || flowchart.UIModel == null)
            {
                LogSelection(nameof(GetPrimarySelectedBlock), "Flowchart or UIModel is null");
                return null;
            }

            IBlock selected = flowchart.UIModel.SelectedBlock;
            LogSelection(nameof(GetPrimarySelectedBlock), $"UIModel.SelectedBlock={DescribeBlock(selected)}, SelectedBlockCount={flowchart.SelectedBlockCount}");

            return selected;
        }

        private static void OnMultiBlocksSelected(IList<IBlock> blocks)
        {
            if (blocks == null)
            {
                Clear();
                return;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                IBlock candidate = blocks[i];
                if (candidate != null)
                {
                    Show(candidate);
                    return;
                }
            }

            Clear();
        }

        private static void OnEmptySpaceLeftClicked(PointerEventInfo _)
        {
            ClearInternal(trackedFlowchart);
        }

        private static void OnFlowchartChanged(Flowchart previous, Flowchart next)
        {
            TrackedFlowchart = next;

            if (next == null)
            {
                ClearInternal(null);
                return;
            }

            IBlock selectedBlock = GetPrimarySelectedBlock(next);
            if (selectedBlock != null)
            {
                ShowInspectorFor(next, selectedBlock);
            }
            else
            {
                ClearInternal(next);
            }
        }

        private static void DisposeInspector()
        {
            if (inspectorInstance != null)
            {
                ScriptableObject.DestroyImmediate(inspectorInstance);
                inspectorInstance = null;
            }

            trackedFlowchart = null;
            lastShownBlock = null;
        }

        private static void LogSelection(string source, string message)
        {
            if (!_debugSelectionRouting)
            {
                return;
            }

            string activeObj = Selection.activeObject != null ? $"{Selection.activeObject.name} ({Selection.activeObject.GetType().Name})" : "null";
            string activeGo = Selection.activeGameObject != null ? Selection.activeGameObject.name : "null";
            string tracked = trackedFlowchart != null ? trackedFlowchart.name : "null";

            Debug.Log($"[BlockInspectorManager::{source}] {message} | activeObject={activeObj} | activeGameObject={activeGo} | trackedFlowchart={tracked}");
        }

        private static string DescribeBlock(IBlock block)
        {
            if (block == null)
            {
                return "null";
            }

            Flowchart flowchart = block.GetFlowchart();
            string fcName = flowchart != null ? flowchart.name : "null";
            return $"name={block.BlockName}, id={block.ItemId}, isSelected={block.IsSelected}, flowchart={fcName}";
        }

        private static void OnSelectionChanged()
        {
            if (!_debugSelectionRouting)
            {
                return;
            }

            UnityEngine.Object active = Selection.activeObject;
            bool activeIsHiddenInspector = active is BlockInspector;
            string activeName = active != null ? $"{active.name} ({active.GetType().Name})" : "null";

            string inspectorBlock = "null";
            if (inspectorInstance != null && inspectorInstance._block != null)
            {
                inspectorBlock = $"{inspectorInstance._block.BlockName} (id={inspectorInstance._block.ItemId}, isSelected={inspectorInstance._block.IsSelected})";
            }

            LogSelection(nameof(OnSelectionChanged),
                $"Raw selection changed -> active={activeName}, activeIsBlockInspector={activeIsHiddenInspector}, inspector._block={inspectorBlock}");
        }
    }
}