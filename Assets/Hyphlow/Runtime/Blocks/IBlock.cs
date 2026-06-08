using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Runtime-agnostic contract for Block-like logic units.
    /// Current MonoBehaviour Block should implement this.
    /// Future POCO blocks can implement this as well.
    /// </summary>
    public interface IBlock : IHasKey, IHasItemId<byte>, ICommandSource, IRefreshable,
        ICommandResetter
    {
        ICommand GetCommandAtIndex(byte index);
        void ReplaceCommandAtIndex(byte index, ICommand newCommand);
        Flowchart ParentFlowchart { get; }
        bool SuppressAllAutoSelections { get; set; }
        ExecutionState ExecutionState { get; set; }
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
        ICommand ActiveCommand { get; set; }
        int PreviousActiveCommandIndex { get; set; }
        float ExecutingIconTimer { get; set; }

        /// <summary>
        /// Current systems still use this concrete list in several places.
        /// Keep for now; can be narrowed later.
        /// </summary>
        IReadOnlyList<ICommand> CommandList { get; }

        /// <summary>
        /// The index of the command to jump to on the next execution step.
        /// When negative, no jump will occur and execution will proceed to 
        /// the next command in the list.
        /// </summary>
        int NextExecCmdIndex { get; set; }

        IEventHandler EventHandler { get; set; }

        Flowchart GetFlowchart();

        bool IsExecuting { get; }
        event Action<IBlock> ExecStarted;
        event Action<IBlock> ExecEnded;
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
        int ExecutionCount { get; set; }
    }

    public enum FilteredState { Full, Partial, None }
}