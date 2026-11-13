using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;
using System.Globalization;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Brings variables saved by earlier versions up to date, keeping their key, id, scope
    /// and value so that anything pointing at them by id keeps working. Shared by the
    /// automatic migrator and the Hyphration Control Panel entry.
    ///
    /// Three kinds of variable get migrated:
    /// - Legacy (Fungus-style) Variable components, which become Muscariables. Only Flowcharts
    ///   have these; Variable Sets are ScriptableObjects and can't hold components.
    /// - Int, Float and Double Muscariables, which become NumberMuscariables. (Legacy
    ///   integers, floats and doubles skip the middle step and go straight to that.)
    /// - VectorTwo, VectorThree and VectorFour Muscariables, which become VectorMuscariables.
    ///   (Legacy Vector2s, Vector3s and Vector4s skip the middle step here too.)
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

            int migratableCount = CollectMigratable(flowchart).Count;
            int supersededCount = CollectSuperseded(flowchart.Variables).Count;
            return migratableCount + supersededCount;
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
            List<Muscariable> numerics = CollectSuperseded(flowchart.Variables);
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
                Muscariable replacement = Supersede(old, old.Scope);
                flowchart.AddVariable(replacement);
                migrations?.Add(Describe(old.Key, old, replacement, flowchart.name));
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

            return CollectSuperseded(set.Variables).Count;
        }

        /// <summary>
        /// Converts the set's out-of-date Muscariables. Legacy (MonoBehaviour) Variables
        /// aren't handled, since a ScriptableObject can't hold components. The converted
        /// variables end up last in the list. Returns how many were converted, and
        /// describes each one in <paramref name="migrations"/> if it's given.
        /// </summary>
        public static int MigrateSet(VariableSet set, List<Migration> migrations = null)
        {
            if (set == null)
            {
                return 0;
            }

            List<Muscariable> numerics = CollectSuperseded(set.Variables);

            #region Migrate Muscariables
            for (int i = 0; i < numerics.Count; i++)
            {
                Muscariable old = numerics[i];
                set.RemoveVariable(old);
                Muscariable replacement = Supersede(old, AccessScope.Global);
                set.AddVariable(replacement);
                migrations?.Add(Describe(old.Key, old, replacement, string.Empty));
            }
            #endregion

            int migrated = numerics.Count;
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

        /// <summary>
        /// Whether the variable is of one of the types that VectorMuscariable replaces.
        /// </summary>
        public static bool IsSupersededVector(IVariable variable)
        {
            return variable is VectorTwoMuscariable ||
                variable is VectorThreeMuscariable ||
                variable is VectorFourMuscariable;
        }

        private static bool IsSuperseded(IVariable variable)
        {
            return IsSupersededNumeric(variable) || IsSupersededVector(variable);
        }

        private static Muscariable Supersede(Muscariable old, AccessScope scope)
        {
            return IsSupersededVector(old) ? 
            ToVector(old, scope) : 
            ToNumber(old, scope);
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

            return IsSuperseded(migrated) ? Supersede(migrated, scope) : migrated;
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
                    start = FloatToDouble(floatVar.StartValue);
                    current = FloatToDouble(floatVar.Value);
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

        /// <summary>
        /// Keeps the dimension the old type had, and pads Vector2s and Vector3s with zeroes the
        /// way VectorMuscariable does.
        /// </summary>
        private static VectorMuscariable ToVector(Muscariable old, AccessScope scope)
        {
            Vector4 start, current;
            VectorDimension dimension;
            switch (old)
            {
                case VectorTwoMuscariable twoVar:
                    start = new Vector4(twoVar.StartValue.x, twoVar.StartValue.y, 0f, 0f);
                    current = new Vector4(twoVar.Value.x, twoVar.Value.y, 0f, 0f);
                    dimension = VectorDimension.TwoD;
                    break;

                case VectorThreeMuscariable threeVar:
                    start = new Vector4(threeVar.StartValue.x, threeVar.StartValue.y,
                        threeVar.StartValue.z, 0f);
                    current = new Vector4(threeVar.Value.x, threeVar.Value.y, threeVar.Value.z, 0f);
                    dimension = VectorDimension.ThreeD;
                    break;

                case VectorFourMuscariable fourVar:
                    start = fourVar.StartValue;
                    current = fourVar.Value;
                    dimension = VectorDimension.FourD;
                    break;

                default:
                    throw new System.ArgumentException(
                        $"{old.GetType().Name} isn't one of the types VectorMuscariable replaces.",
                        nameof(old));
            }

            var vector = new VectorMuscariable
            {
                Key = old.Key,
                ItemId = old.ItemId,
                Scope = scope,
                Dimension = dimension
            };
            vector.Init(start); // Sets the start value, and the current one along with it
            vector.Value = current;
            return vector;
        }

        /// <summary>
        /// A plain cast would turn a float like 0.4 into 0.4000000059604645, since that's the
        /// double closest to the float's exact binary value. Going through the float's shortest
        /// round-trip text gives the number the user actually typed.
        /// </summary>
        private static double FloatToDouble(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return value;
            }

            return double.Parse(value.ToString("R", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture);
        }

        private static List<Muscariable> CollectSuperseded(IReadOnlyList<IVariable> variables)
        {
            var results = new List<Muscariable>();
            for (int i = 0; i < variables.Count; i++)
            {
                var current = variables[i];
                if (IsSuperseded(current))
                {
                    results.Add((Muscariable)current);
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
