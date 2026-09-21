using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace VScriptingTests.VariableOperations
{
    public class VariableDataTypeRegistryTests
    {
        [SetUp]
        public void SetUp() => VariableDataTypeRegistry.Clear();

        [TearDown]
        public void TearDown()
        {
            Debug.unityLogger.logEnabled = true;

            // Restore the registry to a real, fully-discovered state so that other
            // suites relying on it aren't affected by this suite running first.
            VariableTypeDiscovery.DiscoverAndRegister();
        }

        [Test]
        public void Register_MapsVariableTypeToDataType()
        {
            var fakeDataType = typeof(FakeIntVariableData);

            VariableDataTypeRegistry.Register(fakeDataType);

            var linked = VariableDataTypeRegistry.GetDataTypeLinkedToVarType(typeof(IntMuscariable));
            Assert.AreEqual(typeof(FakeIntVariableData), linked);
        }

        [Test]
        public void Register_WithNullType_LogsWarningAndDoesNotThrow()
        {
            LogAssert.Expect(LogType.Warning, "Passed null varDataType to VariableDataRegistry Register func");

            Assert.DoesNotThrow(() => VariableDataTypeRegistry.Register(null));
            Assert.AreEqual(0, VariableDataTypeRegistry.TypeMap.Count);
        }

        [Test]
        public void Register_TypeWithoutAttribute_LogsWarningAndSkips()
        {
            Debug.unityLogger.logEnabled = false;

            VariableDataTypeRegistry.Register(typeof(UnattributedVariableData));

            Assert.AreEqual(0, VariableDataTypeRegistry.TypeMap.Count);
        }

        [Test]
        public void Register_SameVariableTypeTwice_KeepsFirstRegistration()
        {
            VariableDataTypeRegistry.Register(typeof(FakeIntVariableData));
            VariableDataTypeRegistry.Register(typeof(DuplicateIntVariableData));

            var linked = VariableDataTypeRegistry.GetDataTypeLinkedToVarType(typeof(IntMuscariable));
            Assert.AreEqual(typeof(FakeIntVariableData), linked);
        }

        [Test]
        public void GetDataTypeLinkedToVarType_ResolvesDerivedTypeViaPolymorphism()
        {
            VariableDataTypeRegistry.Register(typeof(BaseIntVariableData));

            var linked = VariableDataTypeRegistry.GetDataTypeLinkedToVarType(typeof(DerivedIntMuscariable));

            Assert.AreEqual(typeof(BaseIntVariableData), linked);
        }

        [Test]
        public void GetDataTypeLinkedToVarType_UnknownType_ReturnsNull()
        {
            var linked = VariableDataTypeRegistry.GetDataTypeLinkedToVarType(typeof(UnityEngine.Random));

            Assert.IsNull(linked);
        }

        [Test]
        public void CreateForVar_Generic_ReturnsInstance()
        {
            VariableDataTypeRegistry.Register(typeof(FakeIntVariableData));

            var data = VariableDataTypeRegistry.CreateForVar<IntMuscariable>();

            Assert.IsInstanceOf<FakeIntVariableData>(data);
        }

        [Test]
        public void CreateForVar_UnknownType_ReturnsNullAndLogs()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(
                @"Couldn't make an instance for .*"));

            var data = VariableDataTypeRegistry.CreateForVar(typeof(UnityEngine.Random));

            Assert.IsNull(data);
        }

        [Test]
        public void CreateForContentType_ReturnsMatchingInstance()
        {
            VariableDataTypeRegistry.Register(typeof(FakeIntVariableData));

            var data = VariableDataTypeRegistry.CreateForContentType(typeof(int));

            Assert.IsInstanceOf<FakeIntVariableData>(data);
        }

        [Test]
        public void CreateForContentType_UnknownContentType_ReturnsNullAndLogs()
        {
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(
                @"Couldn't find a variable data type for content type .*"));

            var data = VariableDataTypeRegistry.CreateForContentType(typeof(string));

            Assert.IsNull(data);
        }

        [Test]
        public void Clear_RemovesAllRegistrations()
        {
            VariableDataTypeRegistry.Register(typeof(FakeIntVariableData));

            VariableDataTypeRegistry.Clear();

            Assert.AreEqual(0, VariableDataTypeRegistry.TypeMap.Count);
        }
    }

    /// <summary>
    /// A second data type mapped to the same variable type as
    /// <see cref="FakeIntVariableData"/> (declared in
    /// VariableDataFactoryTests.cs), used to test that registering a
    /// duplicate mapping does not overwrite the original one.
    /// </summary>
    [VariableData(typeof(int), typeof(IntMuscariable))]
    [System.Serializable]
    public class DuplicateIntVariableData : VariableData<int>
    {
        [VariableProperty("<Value>", typeof(IntMuscariable))]
        [SerializeField] protected IntMuscariable _intRef = new IntMuscariable();

        public DuplicateIntVariableData() : base(default) { }

        public override IVariable VarRef
        {
            get { return _intRef; }
            set
            {
                if (value == null) { _intRef = null; return; }
                _intRef.BoxedValue = (int)value.BoxedValue;
            }
        }
    }

    /// <summary>
    /// A data type that lacks <see cref="VariableDataAttribute"/> entirely,
    /// used to test that registering it is a no-op that logs a warning.
    /// </summary>
    [System.Serializable]
    public class UnattributedVariableData : VariableData<int>
    {
        [VariableProperty("<Value>", typeof(IntMuscariable))]
        [SerializeField] protected IntMuscariable _intRef = new IntMuscariable();

        public UnattributedVariableData() : base(default) { }

        public override IVariable VarRef
        {
            get { return _intRef; }
            set
            {
                if (value == null) { _intRef = null; return; }
                _intRef.BoxedValue = (int)value.BoxedValue;
            }
        }
    }

    /// <summary>
    /// A subclass of <see cref="IntMuscariable"/> used to verify that
    /// <see cref="VariableDataTypeRegistry.GetDataTypeLinkedToVarType"/>
    /// resolves data types registered against base variable types via
    /// polymorphism (IsAssignableFrom).
    ///
    /// Marked as a test-only VariableInfo so global type discovery
    /// (VariableTypeDiscovery) excludes it outside of test scenes. Without
    /// this, it would inherit IntMuscariable's VariableInfoAttribute
    /// (content type int) and could get picked up as THE muscariable type
    /// for int elsewhere in the app, since VariableInfoAttribute doesn't
    /// specify Inherited = false.
    /// </summary>
    [VariableInfo("Numeric", "Derived Integer (Test)", typeof(int), isTest: true)]
    public class DerivedIntMuscariable : IntMuscariable
    {
    }

    /// <summary>
    /// A data type registered against the base <see cref="IntMuscariable"/>
    /// type, used to test polymorphic resolution against
    /// <see cref="DerivedIntMuscariable"/>.
    /// </summary>
    [VariableData(typeof(int), typeof(IntMuscariable))]
    [System.Serializable]
    public class BaseIntVariableData : VariableData<int>
    {
        [VariableProperty("<Value>", typeof(IntMuscariable))]
        [SerializeField] protected IntMuscariable _intRef = new IntMuscariable();

        public BaseIntVariableData() : base(default) { }

        public override IVariable VarRef
        {
            get { return _intRef; }
            set
            {
                if (value == null) { _intRef = null; return; }
                _intRef.BoxedValue = (int)value.BoxedValue;
            }
        }
    }
}