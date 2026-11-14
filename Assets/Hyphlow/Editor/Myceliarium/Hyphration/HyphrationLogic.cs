using AtMycelia.Myceliarium;
using System;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Holds what Hyphration found, which of it the user picked, and the log of what past
    /// migrations did. Migrating works on assets and scenes directly, so none of this
    /// goes on the entry's undo history or counts as unsaved changes.
    /// </summary>
    internal sealed class HyphrationLogic : ControlPanelEntryLogic
    {
        // The subwindow's lists show these very instances, so they're refilled, not replaced
        public List<HyphrationTarget> Scenes { get; } = new List<HyphrationTarget>();
        public List<HyphrationTarget> VariableSets { get; } = new List<HyphrationTarget>();

        /// <summary>
        /// Newest first.
        /// </summary>
        public List<HyphrationLogEntry> Log { get; } = new List<HyphrationLogEntry>();

        public event Action Changed = delegate { };

        public int SetsNeedingMigration
        {
            get
            {
                int count = 0;
                for (int i = 0; i < VariableSets.Count; i++)
                {
                    count += VariableSets[i].IsSelectable ? 1 : 0;
                }
                return count;
            }
        }

        protected override void AdoptState(object loadResult)
        {
            // A rescan shouldn't make the user pick everything again
            HashSet<string> previouslySelected = SelectedPaths();

            Scenes.Clear();
            VariableSets.Clear();

            var found = loadResult as IReadOnlyList<HyphrationTarget> ??
                Array.Empty<HyphrationTarget>();
            for (int i = 0; i < found.Count; i++)
            {
                HyphrationTarget target = found[i];
                target.IsSelected = target.IsSelectable && previouslySelected.Contains(target.Path);

                List<HyphrationTarget> list = target.Kind == HyphrationTargetKind.Scene ?
                    Scenes :
                    VariableSets;
                list.Add(target);
            }

            Changed();
        }

        private HashSet<string> SelectedPaths()
        {
            var paths = new HashSet<string>();
            AddSelectedPaths(Scenes, paths);
            AddSelectedPaths(VariableSets, paths);
            return paths;
        }

        private static void AddSelectedPaths(List<HyphrationTarget> targets, HashSet<string> paths)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].IsSelected)
                {
                    paths.Add(targets[i].Path);
                }
            }
        }

        public void SetSelected(HyphrationTarget target, bool selected)
        {
            target.IsSelected = selected && target.IsSelectable;
            Changed();
        }

        public void SetAllSelected(HyphrationTargetKind kind, bool selected)
        {
            List<HyphrationTarget> list = kind == HyphrationTargetKind.Scene ? Scenes : VariableSets;
            for (int i = 0; i < list.Count; i++)
            {
                list[i].IsSelected = selected && list[i].IsSelectable;
            }
            Changed();
        }

        public int SelectedCount => CountSelected(Scenes) + CountSelected(VariableSets);

        public int SelectedSceneCount => CountSelected(Scenes);
        public int SelectedSetCount => CountSelected(VariableSets);

        private static int CountSelected(List<HyphrationTarget> targets)
        {
            int count = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                count += targets[i].IsSelected ? 1 : 0;
            }
            return count;
        }

        /// <summary>
        /// Sets first, then scenes, so the log reads in the order the runner works.
        /// </summary>
        public List<HyphrationTarget> GetSelected()
        {
            var selected = new List<HyphrationTarget>();
            for (int i = 0; i < VariableSets.Count; i++)
            {
                if (VariableSets[i].IsSelected)
                {
                    selected.Add(VariableSets[i]);
                }
            }
            for (int i = 0; i < Scenes.Count; i++)
            {
                if (Scenes[i].IsSelected)
                {
                    selected.Add(Scenes[i]);
                }
            }
            return selected;
        }

        public void AddToLog(IReadOnlyList<HyphrationLogEntry> entries)
        {
            // Newest first, with each run's entries kept in their own order
            Log.InsertRange(0, entries);
            Changed();
        }

        public void ClearLog()
        {
            Log.Clear();
            Changed();
        }
    }
}
