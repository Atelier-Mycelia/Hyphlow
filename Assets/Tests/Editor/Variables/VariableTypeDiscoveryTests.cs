using System;
using System.Linq;
using System.Reflection;
using AtMycelia.Hyphlow;
using NUnit.Framework;

namespace VScriptingTests.VariableOperations
{
    public class VariableTypeDiscoveryTests
    {
        [SetUp]
        public void SetUp()
        {
            VariableTypeRegistry.Clear();
            VariableDataTypeRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Restore the registries to a real, fully-discovered state so that other
            // suites relying on them aren't affected by this suite running first.
            VariableTypeDiscovery.DiscoverAndRegister();
        }

        private static void InvokeDiscoverAndRegister()
        {
            VariableTypeDiscovery.DiscoverAndRegister();
        }

        private static bool InvokeShouldExcludeDueToBeingForTests(Type typeToCheck)
        {
            MethodInfo method = typeof(VariableTypeDiscovery).GetMethod(
                "ShouldExcludeDueToBeingForTests",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(method, "Could not find method 'ShouldExcludeDueToBeingForTests' via reflection.");
            return (bool)method.Invoke(null, new object[] { typeToCheck });
        }

        [Test]
        public void DiscoverAndRegister_PopulatesVariableTypeRegistryWithConcreteMuscariables()
        {
            InvokeDiscoverAndRegister();

            Assert.Contains(typeof(IntMuscariable), VariableTypeRegistry.AllMuscariableTypes.ToList(),
                "Expected IntMuscariable to be discovered and registered as a muscariable type.");
        }

        [Test]
        public void DiscoverAndRegister_PopulatesVariableDataTypeRegistry()
        {
            InvokeDiscoverAndRegister();

            Assert.IsTrue(VariableDataTypeRegistry.TypeMap.Count > 0,
                "Expected at least one VariableData type to be discovered and registered.");
        }

        [Test]
        public void DiscoverAndRegister_RegisteredMuscariableHasWorkingActions()
        {
            InvokeDiscoverAndRegister();

            bool gotActions = VariableTypeRegistry.TryGetTypeActionsFor(typeof(IntMuscariable), out var actions);

            Assert.IsTrue(gotActions);
            Assert.IsNotNull(actions);
            Assert.IsNotNull(actions.CompareFunc);
            Assert.IsNotNull(actions.DescFunc);
            Assert.IsNotNull(actions.SetFunc);
        }

        [Test]
        public void ShouldExcludeDueToBeingForTests_ReturnsFalse_ForTypeWithoutAttribute()
        {
            // GenericMuscariable has no VariableInfoAttribute at all, or if it does,
            // it isn't marked as a test type - either way it should not be excluded
            // purely due to this check when no attribute is present.
            bool result = InvokeShouldExcludeDueToBeingForTests(typeof(IntMuscariable));

            Assert.IsFalse(result,
                "A type whose VariableInfoAttribute has IsTest=false should never be excluded.");
        }

        [Test]
        public void ShouldExcludeDueToBeingForTests_ExcludesTestMarkedTypeWhenInEditor()
        {
            // Application.isEditor is true when running editor tests, so per the
            // implementation, IsTest types are excluded regardless of active scene name
            // while running in the Editor.
            bool result = InvokeShouldExcludeDueToBeingForTests(typeof(FakeTestOnlyMuscariable));

            Assert.IsTrue(result,
                "Types marked IsTest=true should be excluded while running in the Editor.");
        }

        [Test]
        public void RefreshVariableDataTypeRegistry_SkipsTypesWithoutVariableDataAttribute()
        {
            InvokeDiscoverAndRegister();

            // FakeVariableDataWithoutAttribute has no [VariableData] attribute, so it should
            // never end up linked in the type map.
            bool isPresent = VariableDataTypeRegistry.TypeMap.Values
                .Contains(typeof(FakeVariableDataWithoutAttribute));

            Assert.IsFalse(isPresent,
                "VariableData types without a VariableDataAttribute should not be registered.");
        }
    }

    [VariableInfo("Test", "TestOnly", typeof(int), isTest: true)]
    public class FakeTestOnlyMuscariable : IntMuscariable
    {
    }

    public class FakeVariableDataWithoutAttribute : VariableData<int>
    {
        public FakeVariableDataWithoutAttribute() : base(default) { }
    }
}
