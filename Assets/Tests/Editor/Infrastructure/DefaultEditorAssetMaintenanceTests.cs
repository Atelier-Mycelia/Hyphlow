using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace VScriptingTests.Infrastructure
{
    public class DefaultEditorAssetMaintenanceTests
    {
        private static object InvokePrivateStatic(string methodName)
        {
            MethodInfo method = typeof(AtMycelia.Hyphlow.EditorExt.DefaultEditorAssetMaintenance).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(method, $"Could not find method '{methodName}' via reflection.");
            return method.Invoke(null, null);
        }

        private static FieldInfo GetFramesToWaitField()
        {
            FieldInfo field = typeof(AtMycelia.Hyphlow.EditorExt.DefaultEditorAssetMaintenance).GetField(
                "_framesToWait",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(field, "Could not find field '_framesToWait' via reflection.");
            return field;
        }

        [Test]
        public void EnsureHyphlowEditorResourcesAsset_ReturnsExistingInstance()
        {
            var expected = AtMycelia.Hyphlow.EditorExt.HyphlowEditorSysAssets.S;
            Assert.IsNotNull(expected, "Expected an existing HyphlowEditorSysAssets asset in the project.");

            var result = InvokePrivateStatic("EnsureHyphlowEditorResourcesAsset")
                as AtMycelia.Hyphlow.EditorExt.HyphlowEditorSysAssets;

            Assert.IsNotNull(result);
            Assert.AreSame(expected, result,
                "EnsureHyphlowEditorResourcesAsset should return the same cached instance rather " +
                "than creating a duplicate.");
        }

        [Test]
        public void EnsureHyphlowEditorResourcesAsset_IsIdempotentAcrossRepeatedCalls()
        {
            var first = InvokePrivateStatic("EnsureHyphlowEditorResourcesAsset")
                as AtMycelia.Hyphlow.EditorExt.HyphlowEditorSysAssets;
            var second = InvokePrivateStatic("EnsureHyphlowEditorResourcesAsset")
                as AtMycelia.Hyphlow.EditorExt.HyphlowEditorSysAssets;

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.AreSame(first, second,
                "Repeated calls should not create additional HyphlowEditorSysAssets instances.");
        }

        [Test]
        public void EnsureFcWindowConfig_ReturnsNonNullConfigMatchingSysAssets()
        {
            var result = InvokePrivateStatic("EnsureFcWindowConfig")
                as AtMycelia.Hyphlow.EditorExt.FlowchartWindowConfig;

            Assert.IsNotNull(result, "EnsureFcWindowConfig should never return null.");
            Assert.AreEqual(AtMycelia.Hyphlow.EditorExt.HyphlowEditorSysAssets.FcwConfig, result,
                "EnsureFcWindowConfig should return the config referenced by HyphlowEditorSysAssets " +
                "once ensured.");
        }

        [Test]
        public void InitializeAfterAssembliesLoaded_ResetsFramesToWaitCounter()
        {
            FieldInfo framesField = GetFramesToWaitField();
            framesField.SetValue(null, 0);
            Assert.AreEqual(0, (int)framesField.GetValue(null));

            AtMycelia.Hyphlow.EditorExt.DefaultEditorAssetMaintenance.InitializeAfterAssembliesLoaded();

            Assert.AreEqual(10, (int)framesField.GetValue(null),
                "InitializeAfterAssembliesLoaded should reset the frame-wait counter back to its default.");
        }

        [Test]
        public void HyphlowEditorSysAssets_EditorTexture_ReturnsProTextureWhenProSkin()
        {
            var proTex = new Texture2D(1, 1);
            var freeTex = new Texture2D(1, 1);

            try
            {
                var editorTexture = new AtMycelia.Hyphlow.EditorExt.HyphlowEditorSysAssets.EditorTexture(
                    freeTex, proTex);

                Texture2D result = editorTexture.Texture2D;

                if (UnityEditor.EditorGUIUtility.isProSkin)
                {
                    Assert.AreSame(proTex, result);
                }
                else
                {
                    Assert.AreSame(freeTex, result);
                }
            }
            finally
            {
                Object.DestroyImmediate(proTex);
                Object.DestroyImmediate(freeTex);
            }
        }

        [Test]
        public void HyphlowEditorSysAssets_EditorTexture_FallsBackToFreeWhenProMissing()
        {
            var freeTex = new Texture2D(1, 1);

            try
            {
                var editorTexture = new AtMycelia.Hyphlow.EditorExt.HyphlowEditorSysAssets.EditorTexture(
                    freeTex, null);

                Assert.AreSame(freeTex, editorTexture.Texture2D);
            }
            finally
            {
                Object.DestroyImmediate(freeTex);
            }
        }
    }
}
