using AtMycelia.Hyphlow.EditorExt;
using System;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    internal enum HyphrationTargetKind
    {
        Scene,
        VariableSet
    }

    /// <summary>
    /// Something Hyphration can migrate: a scene (whose Flowcharts get migrated) or a
    /// Variable Set.
    /// </summary>
    internal sealed class HyphrationTarget
    {
        /// <summary>
        /// Scenes can't be checked without opening them, so their count is unknown until
        /// they're migrated.
        /// </summary>
        public const int UnknownCount = -1;

        public HyphrationTargetKind Kind { get; set; }
        public string Path { get; set; }
        public string DisplayName { get; set; }
        public int MigratableCount { get; set; } = UnknownCount;
        public bool IsSelected { get; set; }

        /// <summary>
        /// Sets with nothing to migrate are listed, but can't be picked.
        /// </summary>
        public bool IsSelectable => MigratableCount != 0;
        public bool CountIsKnown => MigratableCount != UnknownCount;
    }

    internal sealed class HyphrationLogEntry
    {
        public HyphrationTargetKind Kind { get; set; }
        public string DisplayName { get; set; }
        public string Path { get; set; }

        /// <summary>
        /// Each variable that was converted, with its original and new type.
        /// </summary>
        public List<LegacyVariableConverter.Migration> Migrations { get; } =
            new List<LegacyVariableConverter.Migration>();

        public int MigratedCount => Migrations.Count;
        public bool Succeeded { get; set; } = true;
        public string FailureMessage { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;
    }
}
