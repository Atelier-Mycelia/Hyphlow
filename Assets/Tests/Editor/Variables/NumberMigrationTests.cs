using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using UnityEngine;

namespace VScriptingTests.VariableOperations
{
    public class NumberMigrationTests
    {
        [Test]
        public void MigrateSet_FloatKeepsItsDecimalValue_InsteadOfWideningNoise()
        {
            var set = ScriptableObject.CreateInstance<VariableSet>();
            try
            {
                var old = new FloatMuscariable { Key = "speed" };
                old.Init(0.4f);
                set.AddVariable(old);

                LegacyVariableConverter.MigrateSet(set);

                var result = set.Variables[set.Variables.Count - 1] as NumberMuscariable;
                Assert.IsNotNull(result);
                Assert.AreEqual(0.4d, result.Value);
                Assert.AreEqual(0.4d, result.StartValue);
            }
            finally
            {
                Object.DestroyImmediate(set);
            }
        }
    }
}
