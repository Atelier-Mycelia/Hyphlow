using System.Collections.Generic;

namespace AtMycelia.Hyphlow
{
    public interface ICommandSource 
    {
        IReadOnlyList<Command> Commands { get; }

        bool Contains(Command cmd);
        Command GetCommandWithId(ushort id);

        void Add(Command cmd);

        /// <summary>
        /// Returns true if the Command was successfully removed, false if it 
        /// was not found in this source.
        /// </summary>
        bool Remove(Command cmd);

        /// <summary>
        /// Returns true if the Command was successfully removed, false if one
        /// with the given id was not found in this source.
        /// </summary>
        bool RemoveCommandWithId(ushort id);

        /// <summary>
        /// Removes all commands from this source. Returns true if any Commands were removed,
        /// false if there weren't any to remove.
        /// </summary>
        bool RemoveAllCommands();
    }
}