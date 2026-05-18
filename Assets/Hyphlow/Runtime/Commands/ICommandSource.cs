using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    public interface ICommandRemovable : IHasCommands
    {
        /// <summary>
        /// Returns true if the Command was successfully removed, false if it 
        /// was not found in this source.
        /// </summary>
        bool Remove(ICommand cmd, bool triggerSignals);

        /// <summary>
        /// Returns true if the Command was successfully removed, false if one
        /// with the given id was not found in this source.
        /// </summary>
        bool RemoveCommandWithId(ushort id, bool triggerSignals);

        /// <summary>
        /// Removes all commands from this source. Returns true if any Commands were removed,
        /// false if there weren't any to remove.
        /// </summary>
        bool RemoveAllCommands(bool triggerSignals);

    }

    public interface IHasCommands
    {
        IReadOnlyList<ICommand> Commands { get; }
        ICommand GetCommandWithId(ushort id);
        bool Contains(ICommand cmd);
    }

    public interface ICommandSource : IHasCommands, ICommandRemovable
    {
        bool Add(ICommand cmd, bool triggerSignals);

    }
}