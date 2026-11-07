using AtMycelia.Hyphlow;
using NUnit.Framework;

namespace VScriptingTests.MuscariableTests.DataOnly
{
    public class NumericMuscariableTests : MuscariableTestsCommon
    {

        [Test]
        public void IntMuscariable_ArithmeticAndComparison()
        {
            var intVar = new IntMuscariable { Key = "num", ItemId = 4 };
            intVar.Init();
            intVar.Value = SampleInt; // 10

            // Addition
            intVar.Apply(SetOperator.Add, 5);
            Assert.AreEqual(15, intVar.Value);

            // Subtraction
            intVar.Apply(SetOperator.Subtract, 3);
            Assert.AreEqual(12, intVar.Value);

            // Multiply & Divide
            intVar.Apply(SetOperator.Multiply, 2);
            Assert.AreEqual(24, intVar.Value);
            intVar.Apply(SetOperator.Divide, 4);
            Assert.AreEqual(6, intVar.Value);

            // Compare operators
            Assert.IsTrue(intVar.Evaluate(CompareOperator.GreaterThan, 5));
            Assert.IsFalse(intVar.Evaluate(CompareOperator.LessThan, 5));
            Assert.IsTrue(intVar.Evaluate(CompareOperator.Equals, 6));
        }

        [Test]
        public void FloatAndDoubleMuscariable_EqualityOperators()
        {
            var fVar = new FloatMuscariable { Key = "float", ItemId = 5 };
            fVar.Init();
            fVar.Value = SampleF; // 2.5f

            var fVarCopy = new FloatMuscariable { Key = "float2", ItemId = 6 };
            fVarCopy.Init();
            fVarCopy.Value = SampleF;

            Assert.IsTrue(fVar == fVarCopy);
            Assert.IsFalse(fVar != fVarCopy);

            var dVar = new DoubleMuscariable { Key = "dbl", ItemId = 7 };
            dVar.Init();
            dVar.Value = SampleD; // 3.5

            var dOther = new DoubleMuscariable { Key = "dbl2", ItemId = 8 };
            dOther.Init();
            dOther.Value = 2.5;

            Assert.IsFalse(dVar == dOther);
            Assert.IsTrue(dVar != dOther);
        }

        [Test]
        public void BoolMuscariable_EqualityOnly()
        {
            var bVar = new BoolMuscariable { Key = "flag", ItemId = 9 };
            bVar.Init();
            bVar.Value = true;

            var bVar2 = new BoolMuscariable { Key = "flag2", ItemId = 10 };
            bVar2.Init();
            bVar2.Value = false;

            Assert.IsTrue(bVar == new BoolMuscariable { Key = "x", ItemId = 11, Value = true });
            Assert.IsFalse(bVar == bVar2);
        }

        [TestCase(2.7, 2.0)]
        [TestCase(-2.7, -2.0)]
        [TestCase(2.9999999999999996, 3.0)] // 0.3 / 0.1
        [TestCase(-2.9999999999999996, -3.0)]
        [TestCase(3000000.0000000005, 3000000.0)]
        [TestCase(0.4, 0.0)]
        public void NumericMuscariable_WholeNumsOnly_DropsFractionWithTolerance(double assigned, double expected)
        {
            var numVar = new NumberMuscariable { Key = "n", ItemId = 12, WholeNumsOnly = true };
            numVar.Init();

            numVar.Value = assigned;
            Assert.AreEqual(expected, numVar.Value);

            numVar.BoxedValue = assigned;
            Assert.AreEqual(expected, numVar.Value);
        }

        [Test]
        public void NumericMuscariable_WholeNumsOnly_AppliesToArithmetic()
        {
            var numVar = new NumberMuscariable { Key = "n", ItemId = 13, WholeNumsOnly = true };
            numVar.Init();
            numVar.Value = 0.3;
            Assert.AreEqual(0.0, numVar.Value);

            numVar.Value = 3;
            numVar.Apply(SetOperator.Divide, 0.1);
            Assert.AreEqual(30.0, numVar.Value);

            numVar.Apply(SetOperator.Divide, 4.0);
            Assert.AreEqual(7.0, numVar.Value); // 7.5 truncated
        }

        [Test]
        public void NumericMuscariable_WholeNumsOnly_Off_KeepsFraction()
        {
            var numVar = new NumberMuscariable { Key = "n", ItemId = 14 };
            numVar.Init();
            numVar.Value = 2.7;
            Assert.AreEqual(2.7, numVar.Value);
        }

        [Test]
        public void NumericMuscariable_TurningWholeNumsOnTruncatesCurrentValue()
        {
            var numVar = new NumberMuscariable { Key = "n", ItemId = 15 };
            numVar.Init();
            numVar.Value = 5.8;

            numVar.WholeNumsOnly = true;
            Assert.AreEqual(5.0, numVar.Value);
        }

        [Test]
        public void NumericMuscariable_Clone_KeepsWholeNumsFlag()
        {
            var numVar = new NumberMuscariable { Key = "n", ItemId = 16, WholeNumsOnly = true };
            numVar.Init();
            numVar.Value = 4.5;

            var clone = (NumberMuscariable)numVar.Clone();
            Assert.IsTrue(clone.WholeNumsOnly);
            Assert.AreEqual(4.0, clone.Value);
            Assert.AreEqual("n", clone.Key);
        }

        [Test]
        public void Init_WithValidKeyAndID_DoesNotThrow()
        {
            var v = new DoubleMuscariable { Key = "ok", ItemId = 99 };
            Assert.DoesNotThrow(() => v.Init());
        }

    }
}