using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.Variables
{
    public class StringVarSubstitutionServiceTests
    {
        private readonly List<UnityObj> _toDestroy = new List<UnityObj>();
        private StringVarSubstitutionService _service;
        private Flowchart _localFc, _otherFc;

        [SetUp]
        public void SetUp()
        {
            VariableTypeDiscovery.DiscoverAndRegister();
            _service = new StringVarSubstitutionService();
            _localFc = CreateFlowchart("SubstitutionTests_Local");
            _otherFc = CreateFlowchart("SubstitutionTests_Other");
        }

        private Flowchart CreateFlowchart(string name)
        {
            GameObject holder = new GameObject(name);
            _toDestroy.Add(holder);
            return holder.AddComponent<Flowchart>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _toDestroy.Count; i++)
            {
                if (_toDestroy[i] != null)
                {
                    UnityObj.DestroyImmediate(_toDestroy[i]);
                }
            }

            _toDestroy.Clear();
            VariableRegistry.Rebuild(); // Undoes the sources the tests swapped in
        }

        [Test]
        public void SubstituteVariables_VarOnlyInLaterVariableSet_IsFound()
        {
            // The search used to stop at the first set, whether or not it had the var
            VariableSet withoutVar = CreateVariableSet();
            VariableSet withVar = CreateVariableSet();
            withVar.AddNewVariableOfContentType<string>("laterSetVar", "found it");
            UseRegisteredSources(withoutVar, withVar);

            string result = _service.SubstituteVariables("Value: {$laterSetVar}", _localFc);

            Assert.AreEqual("Value: found it", result);
        }

        [Test]
        public void SubstituteVariables_NullVariableSetInSources_IsSkipped()
        {
            VariableSet withVar = CreateVariableSet();
            withVar.AddNewVariableOfContentType<string>("afterNullVar", "ok");
            UseRegisteredSources(null, withVar);

            string result = null;
            Assert.DoesNotThrow(() => result = _service.SubstituteVariables("{$afterNullVar}", _localFc));
            Assert.AreEqual("ok", result);
        }

        [Test]
        public void SubstituteVariables_PublicVarInOtherFlowchart_IsSubstituted()
        {
            _otherFc.AddNewVariable<int>("sharedNumber", 42, AccessScope.Public);

            string result = _service.SubstituteVariables("{$sharedNumber}", _localFc);

            Assert.AreEqual("42", result);
        }

        [TestCase(AccessScope.Private)]
        [TestCase(AccessScope.Protected)]  // Used to slip through the bitmask check
        [TestCase(AccessScope.Null)]       // So did this
        public void SubstituteVariables_HiddenVarInOtherFlowchart_IsNotSubstituted(AccessScope scope)
        {
            _otherFc.AddNewVariable<int>("hiddenNumber", 42, scope);
            UseRegisteredSources(); // So no set can supply it either
            LogAssert.Expect(LogType.Error, new Regex("hiddenNumber"));

            string result = _service.SubstituteVariables("{$hiddenNumber}", _localFc);

            Assert.AreEqual("{$hiddenNumber}", result);
        }

        private VariableSet CreateVariableSet()
        {
            VariableSet set = ScriptableObject.CreateInstance<VariableSet>();
            _toDestroy.Add(set);
            return set;
        }

        /// <summary>
        /// Sets which VariableSets the registry hands out, without making assets for them
        /// (or depending on which sets the project happens to have, and in what order).
        /// </summary>
        private static void UseRegisteredSources(params VariableSet[] sources)
        {
            FieldInfo field = typeof(VariableRegistry).GetField("_registeredSources",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "VariableRegistry has no _registeredSources field.");

            var registered = (List<VariableSet>)field.GetValue(null);
            registered.Clear();
            registered.AddRange(sources);
        }
    }
}
