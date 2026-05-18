using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Runtime-agnostic contract for Block-like logic units.
    /// Current MonoBehaviour Block should implement this.
    /// Future POCO blocks can implement this as well.
    /// </summary>
    public interface IBlock : IHasKey, IHasItemId<byte>, ICommandSource, IRefreshable
    {
        new string Key { get; set; }
        Color Tint { get; set; }
        bool UseCustomTint { get; set; }
        bool Enabled { get; set; }
        bool IsSelected { get; set; }
        bool IsControlSelected { get; set; }

        bool Insert(ICommand cmd, byte index, bool triggerSignals);

        FilteredState FilteredState { get; set; }
        Rect _NodeRect { get; set; }
        Component Owner { get; set; }
        AccessScope Scope { get; set; }
        bool IncludeInSaves { get; set; }
        int LoadPriority { get; set; }

        string BlockName { get; set; }
        string Description { get; }

        ExecutionState State { get; }
        ICommand ActiveCommand { get; }
        int PreviousActiveCommandIndex { get; }
        float ExecutingIconTimer { get; set; }

        /// <summary>
        /// Current systems still use this concrete list in several places.
        /// Keep for now; can be narrowed later.
        /// </summary>
        IList<ICommand> CommandList { get; }

        int JumpToCommandIndex { set; }

        IEventHandler EventHandler { get; set; }

        Flowchart GetFlowchart();

        bool IsExecuting { get; }
        int GetExecutionCount();

        void StartExecution();
        IEnumerator Execute(int commandIndex = 0, Action onComplete = null);
        void Stop();

        IList<IBlock> GetConnectedBlocks();
        void RefreshConnectedBlockCache(ref IList<IBlock> toRefresh);

        Type GetPreviousActiveCommandType();
        int GetPreviousActiveCommandIndent();
        Command GetPreviousActiveCommand();

        void UpdateIndentLevels();
        int GetLabelIndex(string labelKey);

        bool SuppressNextAutoSelection { get; set; }

        HideFlags HideFlags { get; set; }
    }

    public enum FilteredState { Full, Partial, None }
}