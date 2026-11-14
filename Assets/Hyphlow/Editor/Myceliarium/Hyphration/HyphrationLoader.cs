using AtMycelia.Hyphlow.EditorExt;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Finds everything Hyphration can migrate: the project's scenes, and its Variable Sets
    /// along with how many legacy variables each still holds.
    /// </summary>
    internal sealed class HyphrationLoader : ImmediateEntryLoader
    {
        protected override object Read()
        {
            return FindTargets();
        }

        internal static List<HyphrationTarget> FindTargets()
        {
            var found = new List<HyphrationTarget>();
            AddScenes(found);
            AddVariableSets(found);
            return found;
        }

        private static void AddScenes(List<HyphrationTarget> found)
        {
            // Assets only: scenes in packages are read-only
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                found.Add(new HyphrationTarget
                {
                    Kind = HyphrationTargetKind.Scene,
                    Path = path,
                    DisplayName = Path.GetFileNameWithoutExtension(path)
                });
            }
        }

        private static void AddVariableSets(List<HyphrationTarget> found)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(VariableSet)}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var set = AssetDatabase.LoadAssetAtPath<VariableSet>(path);
                if (set == null)
                {
                    continue;
                }

                found.Add(new HyphrationTarget
                {
                    Kind = HyphrationTargetKind.VariableSet,
                    Path = path,
                    DisplayName = set.name,
                    MigratableCount = LegacyVariableConverter.CountMigratable(set)
                });
            }
        }
    }
}
