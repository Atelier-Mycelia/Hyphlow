using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.VariableOperations
{
    public class VariableSourceAssetSavingTests
    {
        private const string TestAssetPath = "Assets/TestVariableSourceSaving.asset";

        private VariableSourceAsset _source;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestAssetPath);

            _source = ScriptableObject.CreateInstance<VariableSourceAsset>();
            AssetDatabase.CreateAsset(_source, TestAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            if (_source != null)
            {
                UnityObj.DestroyImmediate(_source, true);
            }

            AssetDatabase.DeleteAsset(TestAssetPath);
            _source = null;
        }

        [Test]
        public void SavedAsset_RoundTripsStringVariable()
        {
            const string key = "greeting";
            const string val = "hello";

            var stringVar = _source.AddNewVariableOfContentType<string>(key);
            stringVar.Value = val;

            EditorUtility.SetDirty(_source);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var reloaded = AssetDatabase.LoadAssetAtPath<VariableSourceAsset>(TestAssetPath);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(1, reloaded.Variables.Count);

            var reloadedVar = reloaded.GetVariableByName(key);
            Assert.IsNotNull(reloadedVar);
            Assert.AreEqual(val, reloadedVar.GetValueAs<string>());
        }

        [Test]
        public void SavedAsset_RoundTripsMultipleVariablesOfDifferentTypes()
        {
            _source.AddNewVariableOfContentType<string>("someString").Value = "abc";
            _source.AddNewVariableOfContentType<int>("someInt").Value = 42;
            _source.AddNewVariableOfContentType<bool>("someBool").Value = true;
            _source.AddNewVariableOfContentType<float>("someFloat").Value = 3.14f;

            EditorUtility.SetDirty(_source);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var reloaded = AssetDatabase.LoadAssetAtPath<VariableSourceAsset>(TestAssetPath);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(4, reloaded.Variables.Count);

            Assert.AreEqual("abc", reloaded.GetVariableByName("someString").GetValueAs<string>());
            Assert.AreEqual(42, reloaded.GetVariableByName("someInt").GetValueAs<int>());
            Assert.AreEqual(true, reloaded.GetVariableByName("someBool").GetValueAs<bool>());
            Assert.AreEqual(3.14f, reloaded.GetVariableByName("someFloat").GetValueAs<float>(), 0.0001f);
        }

        [Test]
        public void SavedAsset_RoundTripsUniqueId()
        {
            _source.UniqueId = System.Guid.NewGuid().ToString();
            string expectedId = _source.UniqueId;

            EditorUtility.SetDirty(_source);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var reloaded = AssetDatabase.LoadAssetAtPath<VariableSourceAsset>(TestAssetPath);
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(expectedId, reloaded.UniqueId);
        }

        [Test]
        public void ModifyingVariable_MarksAssetDirty()
        {
            AssetDatabase.SaveAssets();
            EditorUtility.ClearDirty(_source);
            Assert.IsFalse(EditorUtility.IsDirty(_source));

            _source.AddNewVariableOfContentType<string>("newVar").Value = "value";
            EditorUtility.SetDirty(_source);

            Assert.IsTrue(EditorUtility.IsDirty(_source));
        }

        [Test]
        public void SavedAsset_RoundTripsIncludeInSavesFlag()
        {
            _source.IncludeInSaves = false;

            EditorUtility.SetDirty(_source);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var reloaded = AssetDatabase.LoadAssetAtPath<VariableSourceAsset>(TestAssetPath);
            Assert.IsNotNull(reloaded);
            Assert.IsFalse(reloaded.IncludeInSaves);
        }
    }
}
