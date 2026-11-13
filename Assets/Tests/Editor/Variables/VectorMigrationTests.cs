using System.Collections.Generic;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using UnityEngine;

namespace VScriptingTests.VariableOperations
{
    public class VectorMigrationTests
    {
        private VariableSet _set;

        [SetUp]
        public void SetUp()
        {
            _set = ScriptableObject.CreateInstance<VariableSet>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_set);
        }

        [Test]
        public void MigrateSet_ConvertsVectorTwo_KeepingIdentityAndValue()
        {
            var old = new VectorTwoMuscariable { Key = "pos", ItemId = 7, Value = new Vector2(1, 2) };
            _set.AddVariable(old);

            var migrations = new List<LegacyVariableConverter.Migration>();
            int count = LegacyVariableConverter.MigrateSet(_set, migrations);

            Assert.AreEqual(1, count);
            var result = _set.Variables[_set.Variables.Count - 1] as VectorMuscariable;
            Assert.IsNotNull(result);
            Assert.AreEqual("pos", result.Key);
            Assert.AreEqual(7, result.ItemId);
            Assert.AreEqual(new Vector4(1, 2, 0, 0), result.Value);
            Assert.AreEqual(VectorDimension.TwoD, result.Dimension);
            Assert.AreEqual(nameof(VectorTwoMuscariable), migrations[0].FromType);
            Assert.AreEqual(nameof(VectorMuscariable), migrations[0].ToType);
        }

        [Test]
        public void MigrateSet_ConvertsVectorThreeAndFour_WithMatchingDimensions()
        {
            _set.AddVariable(new VectorThreeMuscariable { Key = "three", Value = new Vector3(1, 2, 3) });
            _set.AddVariable(new VectorFourMuscariable { Key = "four", Value = new Vector4(1, 2, 3, 4) });

            Assert.AreEqual(2, LegacyVariableConverter.CountMigratable(_set));
            Assert.AreEqual(2, LegacyVariableConverter.MigrateSet(_set));
            Assert.AreEqual(0, LegacyVariableConverter.CountMigratable(_set));

            VectorMuscariable three = null, four = null;
            foreach (IVariable variable in _set.Variables)
            {
                if (variable.Key == "three") three = variable as VectorMuscariable;
                if (variable.Key == "four") four = variable as VectorMuscariable;
            }

            Assert.AreEqual(VectorDimension.ThreeD, three.Dimension);
            Assert.AreEqual(new Vector4(1, 2, 3, 0), three.Value);
            Assert.AreEqual(VectorDimension.FourD, four.Dimension);
            Assert.AreEqual(new Vector4(1, 2, 3, 4), four.Value);
        }
    }
}
