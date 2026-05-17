using System;

namespace AtMycelia.Hyphlow
{
    public static class CommandSignals
    {
        public static Action<ICommand> CommandSelected = delegate { };
    }

    public interface ICommandSelectionResponder
    {
        void OnCommandSelected(ICommand command);
    }
}