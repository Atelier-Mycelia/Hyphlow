using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObj = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AtMycelia.Hyphlow
{
    [Serializable]
    public sealed class CommandManager : ICommandSource, IDisposable, IHasName
    {
        // Keep this serializable for current MB-backed Commands.
        // If/when POCO Commands become primary, this can be migrated to a serialize-reference strategy.
        [SerializeField] private List<Command> _legacyCommands = new List<Command>();
        [SerializeField] private ushort _nextValidCommandId = 1;

        /// <summary>
        /// Optional owner for naming/context (e.g., Flowchart, Block, or BlockLogicManagerComponent).
        /// </summary>
        public IBlockManager CommandOwner
        {
            get => _commandOwner;
            set => _commandOwner = value;
        }

        private IBlockManager _commandOwner;

        public IReadOnlyList<ICommand> Commands
        {
            get
            {
                if (_legacyCommands == null || _lookup == null || _lookup.Count != _legacyCommands.Count)
                {
                    Refresh();
                }

                return _legacyCommands;
            }
        }

        private Dictionary<ushort, ICommand> _lookup = new Dictionary<ushort, ICommand>();

        /// <summary>
        /// Rebuilds lookup and ensures IDs are valid and unique.
        /// </summary>
        public void Refresh()
        {
            _legacyCommands ??= new List<Command>();
            _lookup ??= new Dictionary<ushort, ICommand>();
            _lookup.Clear();

            for (int i = _legacyCommands.Count - 1; i >= 0; i--)
            {
                if (_legacyCommands[i] == null)
                {
                    _legacyCommands.RemoveAt(i);
                }
            }

            for (int i = 0; i < _legacyCommands.Count; i++)
            {
                ICommand current = _legacyCommands[i];
                EnsureValidIdFor(current);
                current.CommandIndex = (byte)i;
                _lookup[current.ItemId] = current;
            }
        }

        private void EnsureValidIdFor(ICommand command)
        {
            if (command == null)
            {
                Debug.LogError("Cannot ensure valid Command ID for a null command.");
                return;
            }

            while (command.ItemId == 0 || _lookup.ContainsKey(command.ItemId))
            {
                command.ItemId = NextValidCommandId();
            }
        }

        public ushort NextValidCommandId()
        {
            if (_nextValidCommandId == 0)
            {
                _nextValidCommandId = 1;
            }

            ushort result = _nextValidCommandId;
            _nextValidCommandId++;
            return result;
        }

        public bool Contains(ICommand cmd)
        {
            if (cmd == null)
            {
                return false;
            }

            if (_lookup.TryGetValue(cmd.ItemId, out ICommand registered))
            {
                return ReferenceEquals(registered, cmd);
            }

            return false;
        }

        public ICommand GetCommandWithId(ushort id)
        {
            _lookup.TryGetValue(id, out ICommand result);
            return result;
        }

        public void Add(ICommand cmd)
        {
            Add(cmd, true);
        }

        public bool Add(ICommand cmd, bool triggerSignals)
        {
            if (cmd == null)
            {
                Debug.LogError("Cannot add null Command to CommandManager.");
                return false;
            }

            if (Contains(cmd))
            {
                return false;
            }

            EnsureValidIdFor(cmd);

            if (triggerSignals)
            {
                PreCommandAdded(cmd);
            }

            if (cmd is Command legacy)
            {
                _legacyCommands.Add(legacy);
            }
            else
            {
                Debug.LogWarning($"Added non-MonoBehaviour ICommand of type {cmd.GetType().Name}. " +
                    "It will be tracked in lookup only until POCO serialization is introduced.");
            }

            _lookup[cmd.ItemId] = cmd;
            MarkOwnerAsDirty();

            if (triggerSignals)
            {
                CommandAdded(cmd);
            }

            return true;
        }

        public event Action<ICommand> PreCommandAdded = delegate { };
        public event Action<ICommand> CommandAdded = delegate { };

        public bool Remove(ICommand cmd)
        {
            return Remove(cmd, true);
        }

        public bool Remove(ICommand cmd, bool triggerSignals)
        {
            if (cmd == null)
            {
                return false;
            }

            if (triggerSignals)
            {
                PreCommandRemoved(cmd);
            }

            bool removedFromList = false;
            if (cmd is Command legacy)
            {
                removedFromList = _legacyCommands.Remove(legacy);
            }

            bool removedFromLookup = false;
            if (_lookup.TryGetValue(cmd.ItemId, out ICommand registered) &&
                ReferenceEquals(registered, cmd))
            {
                removedFromLookup = _lookup.Remove(cmd.ItemId);
            }
            else
            {
                removedFromLookup = RemoveLookupEntryByReference(cmd);
            }

            if (!removedFromList && !removedFromLookup)
            {
                return false;
            }

            ReindexCommands();
            MarkOwnerAsDirty();

            if (triggerSignals)
            {
                CommandRemoved(cmd);
            }

            return true;
        }

        public event Action<ICommand> PreCommandRemoved = delegate { };
        public event Action<ICommand> CommandRemoved = delegate { };

        public bool RemoveCommandWithId(ushort id)
        {
            return RemoveCommandWithId(id, true);
        }

        public bool RemoveCommandWithId(ushort id, bool triggerSignals)
        {
            ICommand toRemove = GetCommandWithId(id);
            if (toRemove == null)
            {
                return false;
            }

            return Remove(toRemove, triggerSignals);
        }

        public bool RemoveAllCommands()
        {
            return ClearCommands(true);
        }

        public bool ClearCommands(bool triggerSignals = true)
        {
            bool anyRemoved = _legacyCommands.Count > 0 || _lookup.Count > 0;

            while (_legacyCommands.Count > 0)
            {
                ICommand next = _legacyCommands[0];
                if (!Remove(next, triggerSignals))
                {
                    // Safety fallback to avoid infinite loops with stale state.
                    _legacyCommands.RemoveAt(0);
                }
            }

            // If lookup still has non-legacy commands, clear those too.
            if (_lookup.Count > 0)
            {
                _lookup.Clear();
            }

            return anyRemoved;
        }

        private bool RemoveLookupEntryByReference(ICommand cmd)
        {
            ushort keyToRemove = 0;
            bool found = false;

            foreach (var pair in _lookup)
            {
                if (ReferenceEquals(pair.Value, cmd))
                {
                    keyToRemove = pair.Key;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            _lookup.Remove(keyToRemove);
            return true;
        }

        private void ReindexCommands()
        {
            for (int i = 0; i < _legacyCommands.Count; i++)
            {
                Command command = _legacyCommands[i];
                if (command != null)
                {
                    command.CommandIndex = (byte)i;
                }
            }
        }

        private void MarkOwnerAsDirty()
        {
#if UNITY_EDITOR
            if (_commandOwner is UnityObj ownerUnityObj)
            {
                EditorUtility.SetDirty(ownerUnityObj);
            }
#endif
        }

        public void Dispose()
        {
            _legacyCommands?.Clear();
            _lookup?.Clear();
            _commandOwner = null;
        }

        public string Name
        {
            get
            {
                string ownerName = _commandOwner != null ?
                    _commandOwner.Name :
                    null;
                return string.IsNullOrEmpty(ownerName) ?
                    _defaultName :
                    ownerName;
            }
            set
            {
                Debug.LogWarning("CommandManager.Name is read-only and cannot be set.");
            }
        }

        private static readonly string _defaultName = "CommandManager";

        public int CommandCount => _legacyCommands.Count;
    }
}