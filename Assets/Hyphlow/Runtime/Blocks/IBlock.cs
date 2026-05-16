using System;
using System.Collections;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Runtime-agnostic contract for Block-like logic units.
    /// Current MonoBehaviour Block should implement this.
    /// Future POCO blocks can implement this as well.
    /// </summary>
    public interface IBlock : IHasKey, IHasItemId<ushort>, ICommandSource, IRefreshable
    {
        AccessScope Scope { get; set; }
        bool IncludeInSaves { get; set; }
        int LoadPriority { get; set; }

        string BlockName { get; set; }
        string Description { get; }

        ExecutionState State { get; }
        Command ActiveCommand { get; }
        int PreviousActiveCommandIndex { get; }
        float ExecutingIconTimer { get; set; }

        /// <summary>
        /// Current systems still use this concrete list in several places.
        /// Keep for now; can be narrowed later.
        /// </summary>
        List<Command> CommandList { get; }

        int JumpToCommandIndex { set; }

        EventHandler _EventHandler { get; set; }

        Flowchart GetFlowchart();

        bool IsExecuting();
        int GetExecutionCount();

        void StartExecution();
        IEnumerator Execute(int commandIndex = 0, Action onComplete = null);
        void Stop();

        List<Block> GetConnectedBlocks();
        void RefreshConnectedBlockCache(ref List<Block> toRefresh);

        Type GetPreviousActiveCommandType();
        int GetPreviousActiveCommandIndent();
        Command GetPreviousActiveCommand();

        void UpdateIndentLevels();
        int GetLabelIndex(string labelKey);
    }
}