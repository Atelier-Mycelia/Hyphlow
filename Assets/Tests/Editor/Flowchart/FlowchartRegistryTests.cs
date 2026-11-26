using System;
using System.Collections.Generic;
using System.Reflection;
using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.FlowchartStructure
{
    /// <summary>
    /// A prefab asset, its scene instances and its prefab-mode copy all share one UniqueId.
    /// These check that the registry picks the right one to represent that id, and that
    /// unregistering one of them leaves the others alone.
    ///
    /// Setting up real prefab assets and prefab stages is slow and fragile, so the tests
    /// register plain scene Flowcharts into the registry's buckets directly.
    /// </summary>
    public class FlowchartRegistryTests
    {
        private const string SharedUid = "FlowchartRegistryTests-shared-uid";
        private readonly List<GameObject> _toDestroy = new List<GameObject>();

        private Flowchart _sceneFc, _assetFc, _prefabModeFc;

        [SetUp]
        public void SetUp()
        {
            _sceneFc = CreateFlowchartWithUid("RegistryTests_Scene");
            _assetFc = CreateFlowchartWithUid("RegistryTests_Asset");
            _prefabModeFc = CreateFlowchartWithUid("RegistryTests_PrefabMode");

            // The Flowcharts registered themselves under their original ids on enable
            FlowchartRegistry.Clear();
        }

        private Flowchart CreateFlowchartWithUid(string name)
        {
            GameObject holder = new GameObject(name);
            _toDestroy.Add(holder);
            Flowchart fc = holder.AddComponent<Flowchart>();
            fc.UniqueId = SharedUid;
            return fc;
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
            FlowchartRegistry.EnsureInitialized(true);
        }

        [Test]
        public void Lookup_AssetRegisteredAfterScene_StillReturnsScene()
        {
            Register(_sceneFc, SceneBucket);
            Register(_assetFc, AssetBucket);

            Assert.AreSame(_sceneFc, FlowchartRegistry.GetFChartWith(SharedUid));
        }

        [Test]
        public void Lookup_SceneRegisteredAfterAsset_ReturnsScene()
        {
            Register(_assetFc, AssetBucket);
            Register(_sceneFc, SceneBucket);

            Assert.AreSame(_sceneFc, FlowchartRegistry.GetFChartWith(SharedUid));
        }

        [Test]
        public void Lookup_PrefabModeRegistered_WinsOverSceneAndAsset()
        {
            Register(_prefabModeFc, PrefabModeBucket);
            Register(_sceneFc, SceneBucket);
            Register(_assetFc, AssetBucket);

            Assert.AreSame(_prefabModeFc, FlowchartRegistry.GetFChartWith(SharedUid));
        }

        [Test]
        public void Lookup_OnlyAssetRegistered_ReturnsAsset()
        {
            Register(_assetFc, AssetBucket);

            Assert.AreSame(_assetFc, FlowchartRegistry.GetFChartWith(SharedUid));
        }

        [Test]
        public void GetFlowcharts_SharedUid_ListsOnlyThePreferredOne()
        {
            Register(_sceneFc, SceneBucket);
            Register(_assetFc, AssetBucket);

            IReadOnlyList<Flowchart> all = FlowchartRegistry.GetFlowcharts();

            CollectionAssert.Contains(all, _sceneFc);
            CollectionAssert.DoesNotContain(all, _assetFc);
        }

        [Test]
        public void Unregister_PrefabModeFlowchart_KeepsSceneAndAssetRegistered()
        {
            Register(_sceneFc, SceneBucket);
            Register(_assetFc, AssetBucket);
            Register(_prefabModeFc, PrefabModeBucket);

            Unregister(_prefabModeFc);

            Assert.AreSame(_sceneFc, FlowchartRegistry.GetFChartWith(SharedUid));
            CollectionAssert.Contains(FlowchartRegistry.GetSceneFlowcharts(), _sceneFc);
            CollectionAssert.Contains(FlowchartRegistry.GetAssetFlowcharts(), _assetFc);
            CollectionAssert.DoesNotContain(FlowchartRegistry.GetPrefabModeFlowcharts(), _prefabModeFc);
        }

        [Test]
        public void Unregister_SceneFlowchart_FallsBackToAsset()
        {
            Register(_sceneFc, SceneBucket);
            Register(_assetFc, AssetBucket);

            Unregister(_sceneFc);

            Assert.AreSame(_assetFc, FlowchartRegistry.GetFChartWith(SharedUid));
            CollectionAssert.Contains(FlowchartRegistry.GetAssetFlowcharts(), _assetFc);
        }

        [Test]
        public void Unregister_LastFlowchartWithUid_RemovesLookupEntry()
        {
            Register(_sceneFc, SceneBucket);

            Unregister(_sceneFc);

            Assert.IsNull(FlowchartRegistry.GetFChartWith(SharedUid));
        }

        [Test]
        public void Unregister_FlowchartNotInAnyBucket_LeavesOthersAlone()
        {
            // Sharing the id isn't enough to take another Flowchart's place out
            Register(_sceneFc, SceneBucket);

            Unregister(_assetFc);

            Assert.AreSame(_sceneFc, FlowchartRegistry.GetFChartWith(SharedUid));
            CollectionAssert.Contains(FlowchartRegistry.GetSceneFlowcharts(), _sceneFc);
        }

        [Test]
        public void DestroyingPrefabModeFlowchart_FallsBackToSceneFlowchart()
        {
            Register(_sceneFc, SceneBucket);
            Register(_prefabModeFc, PrefabModeBucket);

            // Destroying raises FlowchartDestroyed, which unregisters it
            UnityObj.DestroyImmediate(_prefabModeFc.gameObject);

            Assert.AreSame(_sceneFc, FlowchartRegistry.GetFChartWith(SharedUid));
        }

        #region Reflection helpers
        private const string SceneBucket = "_sceneFlowcharts";
        private const string AssetBucket = "_assetFlowcharts";
        private const string PrefabModeBucket = "_prefabModeFlowcharts";

        private static readonly Type _registryType = typeof(FlowchartRegistry);
        private const BindingFlags _privateStatic = BindingFlags.NonPublic | BindingFlags.Static;

        private static void Register(Flowchart fc, string bucketFieldName)
        {
            FieldInfo bucketField = _registryType.GetField(bucketFieldName, _privateStatic);
            Assert.IsNotNull(bucketField, $"FlowchartRegistry has no field {bucketFieldName}.");
            object bucket = bucketField.GetValue(null);

            MethodInfo register = _registryType.GetMethod("RegisterFlowchart", _privateStatic);
            Assert.IsNotNull(register, "FlowchartRegistry has no RegisterFlowchart method.");
            register.Invoke(null, new[] { fc, bucket });
        }

        private static void Unregister(Flowchart fc)
        {
            MethodInfo unregister = _registryType.GetMethod("UnregisterFlowchart", _privateStatic);
            Assert.IsNotNull(unregister, "FlowchartRegistry has no UnregisterFlowchart method.");
            unregister.Invoke(null, new object[] { fc });
        }
        #endregion
    }
}
