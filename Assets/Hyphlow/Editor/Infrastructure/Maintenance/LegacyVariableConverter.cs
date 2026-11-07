using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Brings variables saved by earlier versions up to date, keeping their key, id, scope
    /// and value so that anything pointing at them by id keeps working. Shared by the
    /// automatic migrator and the Hyphration Control Panel entry.
    ///
    /// Two kinds of variable get migrated:
    /// - Legacy (Fungus-style) Variable components, which become Muscariables
    /// - Int, Float and Double Muscariables, which become NumberMuscariables. (Legacy
    ///   integers, floats and doubles skip the middle step and go straight to that.)
    /// </summary>
    public static class LegacyVariableConverter
    {
        /// <summary>
        /// What happened to one variable during a migration.
        /// </summary>
        public sealed class Migration
        {
            public string Key { get; set; }
            public string FromType { get; set; }
            public string ToType { get; set; }

            /// <summary>
            /// The Flowchart the variable was on, or empty for Variable Sets.
            /// </summary>
            public string Source { get; set; }
        }

        /// <summary>
        /// How many variables <see cref="MigrateFlowchart"/> would convert.
        /// </summary>
        public static int CountMigratable(Flowchart flowchart)
        {
            if (flowchart == null)
            {
                return 0;
            }

            return CollectMigratable(flowchart).Count + CollectSupersededNumerics(flowchart.Variables).Count;
        }

        /// <summary>
        /// Converts the flowchart's out-of-date variables and destroys the legacy components
        /// they came from. Legacy variables that are part of a prefab instance are left alone,
        /// since a scene can't remove components that belong to the prefab; the prefab itself
        /// is where those get migrated. The converted variables end up last in the list.
        /// Returns how many were converted, and describes each one in
        /// <paramref name="migrations"/> if it's given.
        /// </summary>
        public static int MigrateFlowchart(Flowchart flowchart, bool recordUndo,
            List<Migration> migrations = null)
        {
            if (flowchart == null)
            {
                return 0;
            }

            List<Variable> legacyVars = CollectMigratable(flowchart);
            List<Muscariable> numerics = CollectSupersededNumerics(flowchart.Variables);
            if (legacyVars.Count == 0 && numerics.Count == 0)
            {
                return 0;
            }

            if (recordUndo)
            {
                Undo.RecordObject(flowchart, "Migrate variables");
            }

            for (int i = 0; i < legacyVars.Count; i++)
            {
                Variable legacy = legacyVars[i];

                // If this legacy var is still registered, remove it first so ID/key can be reused.
                flowchart.RemoveVariable(legacy);
                Muscariable converted = ConvertKeepingIdentity(legacy, legacy.Scope);
                flowchart.AddVariable(converted);
                migrations?.Add(Describe(legacy.Key, legacy, converted, flowchart.name));

                if (recordUndo)
                {
                    Undo.DestroyObjectImmediate(legacy);
                }
                else
                {
                    UnityObj.DestroyImmediate(legacy);
                }
            }

            for (int i = 0; i < numerics.Count; i++)
            {
                Muscariable old = numerics[i];
                flowchart.RemoveVariable(old);
                NumberMuscariable number = ToNumber(old, old.Scope);
                flowchart.AddVariable(number);
                migrations?.Add(Describe(old.Key, old, number, flowchart.name));
            }

            EditorUtility.SetDirty(flowchart);
            VariableManagerComponent manager = flowchart.GetComponent<VariableManagerComponent>();
            if (manager != null)
            {
                EditorUtility.SetDirty(manager);
            }

            return legacyVars.Count + numerics.Count;
        }

        /// <summary>
        /// How many variables <see cref="MigrateSet"/> would convert.
        /// </summary>
        public static int CountMigratable(VariableSet set)
        {
            if (set == null)
            {
                return 0;
            }

            return CollectLegacy(set).Count + CollectSupersededNumerics(set.Variables).Count;
        }

        /// <summary>
        /// Converts the set's out-of-date variables. The legacy components they came from
        /// aren't destroyed, since those live on assets that the set doesn't own. The
        /// converted variables end up last in the list. Returns how many were converted, and
        /// describes each one in <paramref name="migrations"/> if it's given.
        /// </summary>
        public static int MigrateSet(VariableSet set, List<Migration> migrations = null)
        {
            if (set == null)
            {
                return 0;
            }

            List<Variable> legacyVars = CollectLegacy(set);
            List<Muscariable> numerics = CollectSupersededNumerics(set.Variables);

            // Sets are global-only, whatever scope the variable had
            for (int i = 0; i < legacyVars.Count; i++)
            {
                Variable legacy = legacyVars[i];
                set.RemoveVariable(legacy);
                Muscariable converted = ConvertKeepingIdentity(legacy, AccessScope.Global);
                set.AddVariable(converted);
                migrations?.Add(Describe(legacy.Key, legacy, converted, string.Empty));
            }

            for (int i = 0; i < numerics.Count; i++)
            {
                Muscariable old = numerics[i];
                set.RemoveVariable(old);
                NumberMuscariable number = ToNumber(old, AccessScope.Global);
                set.AddVariable(number);
                migrations?.Add(Describe(old.Key, old, number, string.Empty));
            }

            int migrated = legacyVars.Count + numerics.Count;
            if (migrated > 0)
            {
                EditorUtility.SetDirty(set);
            }

            return migrated;
        }

        /// <summary>
        /// Whether the variable is of one of the types that NumberMuscariable replaces.
        /// </summary>
        public static bool IsSupersededNumeric(IVariable variable)
        {
            return variable is IntMuscariable ||
                variable is FloatMuscariable ||
                variable is DoubleMuscariable;
        }

        private static Migration Describe(string key, IVariable from, IVariable to, string source)
        {
            return new Migration
            {
                Key = key,
                FromType = from.GetType().Name,
                ToType = to.GetType().Name,
                Source = source
            };
        }

        private static Muscariable ConvertKeepingIdentity(Variable legacy, AccessScope scope)
        {
            Muscariable migrated = legacy.ToMuscariable();
            migrated.Key = legacy.Key;
            migrated.ItemId = legacy.ItemId;
            migrated.Scope = scope;
            migrated.BoxedValue = legacy.BoxedValue;

            return IsSupersededNumeric(migrated) ? ToNumber(migrated, scope) : migrated;
        }

        /// <summary>
        /// Integers become whole-numbers-only Numbers, so that they keep behaving like
        /// integers. Floats and doubles are taken as they are.
        /// </summary>
        private static NumberMuscariable ToNumber(Muscariable old, AccessScope scope)
        {
            double start, current;
            bool wholeNumsOnly = false;
            switch (old)
            {
                case IntMuscariable intVar:
                    start = intVar.StartValue;
                    current = intVar.Value;
                    wholeNumsOnly = true;
                    break;

                case FloatMuscariable floatVar:
                    start = floatVar.StartValue;
                    current = floatVar.Value;
                    break;

                case DoubleMuscariable doubleVar:
                    start = doubleVar.StartValue;
                    current = doubleVar.Value;
                    break;

                default:
                    throw new System.ArgumentException(
                        $"{old.GetType().Name} isn't one of the types NumberMuscariable replaces.",
                        nameof(old));
            }

            var number = new NumberMuscariable
            {
                Key = old.Key,
                ItemId = old.ItemId,
                Scope = scope,
                WholeNumsOnly = wholeNumsOnly
            };
            number.Init(start); // Sets the start value, and the current one along with it
            number.Value = current;
            return number;
        }

        private static List<Muscariable> CollectSupersededNumerics(IReadOnlyList<IVariable> variables)
        {
            var results = new List<Muscariable>();
            for (int i = 0; i < variables.Count; i++)
            {
                if (IsSupersededNumeric(variables[i]))
                {
                    results.Add((Muscariable)variables[i]);
                }
            }
            return results;
        }

        private static List<Variable> CollectLegacy(VariableSet set)
        {
            var results = new List<Variable>();
            IReadOnlyList<IVariable> all = set.Variables;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is Variable legacy && legacy != null)
                {
                    results.Add(legacy);
                }
            }
            return results;
        }

        private static List<Variable> CollectMigratable(Flowchart flowchart)
        {
            var results = new List<Variable>();
            Variable[] found = flowchart.GetComponentsInChildren<Variable>(true);
            for (int i = 0; i < found.Length; i++)
            {
                Variable candidate = found[i];
                if (candidate == null || results.Contains(candidate))
                {
                    continue;
                }

                if (PrefabUtility.IsPartOfPrefabInstance(candidate))
                {
                    Debug.LogWarning($"Skipped legacy variable '{candidate.Key}' on " +
                        $"'{flowchart.name}': it belongs to a prefab instance, so it has to be " +
                        "migrated in the prefab itself.", candidate);
                    continue;
                }

                results.Add(candidate);
            }

            return results;
        }
    }
}
