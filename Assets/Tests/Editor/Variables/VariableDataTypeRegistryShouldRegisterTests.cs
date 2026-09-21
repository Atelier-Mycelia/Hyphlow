using System;
using System.Reflection;
using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// Exercises the private ShouldRegister heuristic on
    /// <see cref="VariableDataTypeRegistry"/>, which silently skips
    /// registration for types belonging to test assemblies/namespaces
    /// unless the active scene looks like a test scene. This logic is
    /// risky because a bug here can cause variable data types to
    /// mysteriously fail to register in production builds, so it's
    /// covered independently of the general registry tests.
    /// </summary>
    public class VariableDataTypeRegistryShouldRegisterTests
    {
        private readonly static Type _registryType = typeof(VariableDataTypeRegistry);
        private readonly static BindingFlags _bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;

        private static readonly MethodInfo ShouldRegisterMethod =
            _registryType.GetMethod("ShouldRegister", _bindingFlags);

        private string _originalSceneName;

        [SetUp]
        public void SetUp()
        {
            VariableDataTypeRegistry.Clear();
            _originalSceneName = SceneManager.GetActiveScene().name;
        }

        [TearDown]
        public void TearDown()
        {
            SetActiveSceneName(_originalSceneName);
            Debug.unityLogger.logEnabled = true;

            // Restore the registry to a real, fully-discovered state so that other
            // suites relying on it aren't affected by this suite running first.
            VariableTypeDiscovery.DiscoverAndRegister();
        }

        private static void SetActiveSceneName(string name)
        {
            var scene = SceneManager.GetActiveScene();
            scene.name = name;
        }

        private static bool InvokeShouldRegister(System.Type varDataType)
        {
            return (bool)ShouldRegisterMethod.Invoke(null, new object[] { varDataType });
        }

        [Test]
        public void ShouldRegister_WhenSceneNameIsEmpty_ReturnsTrue()
        {
            SetActiveSceneName(string.Empty);

            bool result = InvokeShouldRegister(typeof(LocalFakeIntVariableData));

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldRegister_WhenSceneNameContainsTest_ReturnsTrue()
        {
            SetActiveSceneName("MyTestScene");

            bool result = InvokeShouldRegister(typeof(LocalFakeIntVariableData));

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldRegister_WhenSceneNameDoesNotLookLikeATestScene_AndTypeIsFromTestAssembly_ReturnsFalse()
        {
            // LocalFakeIntVariableData is declared in this test assembly,
            // whose assembly name contains "Tests", so once the "is a test
            // scene" escape hatch is closed, ShouldRegister must fall back
            // to the assembly-name check and return false.
            SetActiveSceneName("Gameplay");

            bool result = InvokeShouldRegister(typeof(LocalFakeIntVariableData));

            Assert.IsFalse(result);
        }

        [Test]
        public void Register_WhenSceneDoesNotLookLikeATestScene_SkipsTypesFromTestAssembly()
        {
            SetActiveSceneName("Gameplay");

            VariableDataTypeRegistry.Register(typeof(LocalFakeIntVariableData));

            var linked = VariableDataTypeRegistry.GetDataTypeLinkedToVarType(typeof(IntMuscariable));
            Assert.IsNull(linked);
        }

        [Test]
        public void Register_WhenSceneLooksLikeATestScene_RegistersTypesFromTestAssembly()
        {
            SetActiveSceneName("MyTestScene");

            VariableDataTypeRegistry.Register(typeof(LocalFakeIntVariableData));

            var linked = VariableDataTypeRegistry.GetDataTypeLinkedToVarType(typeof(IntMuscariable));
            Assert.AreEqual(typeof(LocalFakeIntVariableData), linked);
        }

        [Test]
        public void ShouldRegister_WhenNonTestAssemblyType_AndSceneDoesNotLookLikeATestScene_ReturnsTrue()
        {
            // IntegerData ships in the runtime (non-test) assembly, so it
            // should register regardless of the active scene's name.
            SetActiveSceneName("Gameplay");

            bool result = InvokeShouldRegister(typeof(IntegerData));

            Assert.IsTrue(result);
        }
    }

    /// <summary>
    /// A minimal fake VariableData type declared locally in this test file
    /// (this test assembly's name contains "Tests"), used to exercise the
    /// ShouldRegister assembly-name based skip heuristic.
    /// </summary>
    [VariableData(typeof(int), typeof(IntMuscariable))]
    [System.Serializable]
    public class LocalFakeIntVariableData : VariableData<int>
    {
        [VariableProperty("<Value>", typeof(IntMuscariable))]
        [SerializeField] protected IntMuscariable _intRef = new IntMuscariable();

        public LocalFakeIntVariableData() : base(default) { }

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
