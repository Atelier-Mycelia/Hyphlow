using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Runtime-agnostic contract for command-like instruction units.
    /// Intended for both MonoBehaviour Commands and future POCO Commands.
    /// </summary>
    public interface ICommand : IHasItemId<byte>, IRefreshable, IHasName
    {
        bool NonStandardPaste { get; }
        void GetConnectedBlocks(ref IList<IBlock> toPopulate);
        bool Enabled { get; set; }
        void OnCommandAdded(IBlock parentBlock);
        void OnCommandRemoved(IBlock parentBlock);
        IBlock ParentBlock { get; set; }
        string ErrorMessage { get; }

        
        byte CommandIndex { get; set; }

        bool IsExecuting { get; set; }
        float ExecutionIconTimer { get; set; }

        /// <summary>
        /// Whether this command should be considered for execution-state save/load.
        /// </summary>
        bool ReexecutableOnLoad { get; }

        void Execute();

        string GetSummary();
        string GetSearchableContent();
        string GetHelpText();

        bool OpenBlock();
        bool CloseBlock();

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only.
        /// </summary>
        int IndentLevel { get; set; }
#endif
    }

    /// <summary>
    /// Optional execution-flow behavior (Continue / jump / stopping parent).
    /// </summary>
    public interface ICommandFlowControl
    {
        void Continue();
        void Continue(int nextCommandIndex);
        void StopParentBlock();
        void OnStopExecuting();
    }

    /// <summary>
    /// Optional lifecycle callbacks used by editor/runtime orchestration.
    /// </summary>
    public interface ICommandLifecycleCallbacks
    {
        void OnCommandAdded();
        void OnCommandRemoved();
        void OnEnter();
        void OnExit();
        void OnReset();
    }

    /// <summary>
    /// Optional graph-link metadata (references to other blocks).
    /// </summary>
    public interface IBlockConnectedCommand
    {
        void GetConnectedBlocks(ref IList<IBlock> toPopulate);
    }

    /// <summary>
    /// Optional variable-reference metadata.
    /// </summary>
    public interface IVariableReferencingCommand
    {
        bool HasReference(IVariable variable);
    }

    /// <summary>
    /// Optional debug/location metadata.
    /// </summary>
    public interface ICommandLocationInfo
    {
        string GetLocationIdentifier();
    }
}