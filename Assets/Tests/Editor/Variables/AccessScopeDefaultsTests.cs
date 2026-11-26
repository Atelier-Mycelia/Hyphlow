using AtMycelia.Hyphlow;
using NUnit.Framework;

namespace VScriptingTests.Variables
{
    public class AccessScopeDefaultsTests
    {
        [TestCase(AccessScope.Public, true)]
        [TestCase(AccessScope.ReadOnly, true)]
        [TestCase(AccessScope.Global, true)]
        [TestCase(AccessScope.Private, false)]
        [TestCase(AccessScope.Protected, false)]   // Protected (3) shares bits with Public | Global
        [TestCase(AccessScope.Null, false)]        // So does Null (99)
        public void IsVisibleToOutsiders_MatchesScope(AccessScope scope, bool expected)
        {
            Assert.AreEqual(expected, AccessScopeDefaults.IsVisibleToOutsiders(scope));
        }
    }
}
