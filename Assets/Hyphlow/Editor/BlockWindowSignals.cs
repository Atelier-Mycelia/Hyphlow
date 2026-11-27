using System;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Lets outside code (the FlowchartWindow, plugins...) react to what happens in the
    /// BlockWindow. The window itself doesn't listen to these; it updates its own parts
    /// directly, so the order things refresh in stays predictable.
    /// </summary>
    public static class BlockWindowSignals
    {
        /// <summary>
        /// Raised when the window is enabled. That includes after each domain reload, so
        /// listeners that came back with the reload still learn the window is up; expect it
        /// more than once per <see cref="Closed"/>.
        /// </summary>
        public static Action<BlockWindow> Opened = delegate { };

        /// <summary>
        /// Raised only when the window is actually closed, not on domain reloads.
        /// </summary>
        public static Action<BlockWindow> Closed = delegate { };

        /// <summary>
        /// Args: the window, the Block it was showing (null if none), the Block it shows now (null if none).
        /// </summary>
        public static Action<BlockWindow, IBlock, IBlock> TargetBlockChanged = delegate { };

        /// <summary>
        /// Args: the window, the previously selected Command, the newly selected one.
        /// Either can be null.
        /// </summary>
        public static Action<BlockWindow, ICommand, ICommand> CommandSelectionChanged = delegate { };

        public static Action<BlockWindow, ICommand> CommandCopied = delegate { };

        /// <summary>
        /// Raised when the window goes from having no unapplied edits to having some, or back.
        /// </summary>
        public static Action<BlockWindow, bool> PendingEditsChanged = delegate { };

        /// <summary>
        /// Raised by both OK and Apply, after the edits have been written to the Block.
        /// </summary>
        public static Action<BlockWindow, IBlock> EditsApplied = delegate { };

        /// <summary>
        /// Raised by Cancel, after the pending edits have been thrown away.
        /// </summary>
        public static Action<BlockWindow, IBlock> EditsDiscarded = delegate { };
    }
}
