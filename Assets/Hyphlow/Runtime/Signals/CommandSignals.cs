using System;

namespace AtMycelia.Hyphlow
{
    public static class CommandSignals
    {
        public static Action<ICommand> CommandSelected = delegate { };
        public static Action<ICommand> CommandDeselected = delegate { };

        public static Action<ICommand, IBlock> PreCommandAdded = delegate { };
        /// <summary>
        /// Raised when a command is added to a block. The first 
        /// parameter is the command that was added, and the 
        /// second parameter is the block to which it was added.
        /// </summary>
        public static Action<ICommand, IBlock> CommandAdded = delegate { };

        public static Action<ICommand, IBlock> PreCommandRemoved = delegate { };
        public static Action<ICommand, IBlock> CommandRemoved = delegate { };
    }

    public interface ICommandSelectionResponder
    {
        void OnCommandSelected(ICommand command);
    }
}