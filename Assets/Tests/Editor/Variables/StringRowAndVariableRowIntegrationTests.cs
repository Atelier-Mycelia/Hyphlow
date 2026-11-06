using AtMycelia.Hyphlow;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UITKLabel = UnityEngine.UIElements.Label;
using UnityEditor;
using System.Reflection;
using System;
using UnityObj = UnityEngine.Object;
using System.Linq;
using AtMycelia.EditorExt;
using AtMycelia;
using AtMycelia.Hyphlow.EditorExt;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// Integration tests exercising StringRowVisualHandler together with VariableSourceAsset,
    /// ensuring StringMuscariable-backed variables in a VariableSourceAsset stay in sync
    /// when the key and value fields are committed (simulating pressing Enter).
    /// Tests are written in an observer-centric style: they don't directly apply the serialized
    /// changes themselves; instead they publish the same signal the editor wiring listens to
    /// and let the observer (test-registered callback) perform the commit. This mirrors the
    /// production observer pattern where UI change signals cause commits/saves.
    /// </summary>
    public class StringRowAndVariableSourceIntegrationTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clean up any leftover test asset from previous runs
            AssetDatabase.DeleteAsset(TestAssetPath);

            PrepSourceAsset();
            void PrepSourceAsset()
            {
                // We want to make it an actual asset file so that it handles MuscariableHolders 
                // like it should in production.
                _source = ScriptableObject.CreateInstance<VariableSet>();
                AssetDatabase.CreateAsset(_source, TestAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // We want to give the UI something to render right away, hence this initial var
                var stringVar = _source.AddNewVariableOfContentType<string>(initStringVarKey);
                stringVar.Value = initStringVarValue;
            }

            PrepUIElements();
            void PrepUIElements()
            {
                uxml = Resources.Load<VisualTreeAsset>(PathToUxml);
                _uiList = new ListView();
                _countLabel = new UITKLabel();
                _listView = new VariableListView(new VariableListViewInitArgs
                {
                    List = _uiList,
                    CountLabel = _countLabel,
                    VariableSource = _source,
                });

                InitManager();
                void InitManager()
                {
                    VisualElement rootElem = uxml.CloneTree();
                    var list = rootElem.Q<ListView>("rowList");
                    var count = rootElem.Q<UITKLabel>("varCountLabel");
                    var addBtn = rootElem.Q<Button>("addVarButton");

                    VRowManagerInitArgs managerInitArgs = new VRowManagerInitArgs
                    {
                        Root = rootElem,
                        AddButton = addBtn,
                        VariableSource = _source,
                        VariableListView = _listView,
                    };

                    manager?.Dispose();
                    manager = new VariableRowManager();
                    manager.Init(managerInitArgs);
                }

                _listView.ForceMaterializeAllRowsForTests();

            }

            GatherUpDestructables();
            void GatherUpDestructables()
            {
                // I normally wouldn't wrap just one line into a method, but this is
                // here to illustrate the pattern for when more things need to be
                // added later.
                _toDestroy.Add(_source);
            }

            PreTestAssertions();
            void PreTestAssertions()
            {
                Assert.AreEqual(1, _listView.RowCount, "Expected one materialized row for single variable.");

                var handler = HandlerAt(0) as StringRowVisualHandler;
                Assert.IsNotNull(handler, "Handler should be a StringRowVisualHandler.");

                // Locate the KeyInput TextField and ValueField
                var keyField = handler.RowRoot.Q<TextField>("KeyInput");
                var valueField = handler.RowRoot.Q<TextField>("ValueField");
                Assert.IsNotNull(keyField, "KeyInput not found on the row template.");
                Assert.IsNotNull(valueField, "ValueField not found on the row template.");

                // Confirm initial state
                var original = _source.GetVariableByName(initStringVarKey) as StringMuscariable;
                Assert.IsNotNull(original);
                Assert.AreEqual(initStringVarValue, original.Value);
            }
        }

        protected VariableSet _source;
        protected readonly string initStringVarKey = "greeting";
        protected readonly string initStringVarValue = "hello";
        private const string TestAssetPath = "Assets/TestVariableSource.asset";

        protected IRowVisualHandler HandlerAt(int index)
        {
            Assert.IsTrue(_listView.TryGetHandler(_listView.VarsToDisplay[index], out var handler),
                $"No materialized row for the variable at index {index}.");
            return handler;
        }

        protected ListView _uiList;
        protected UITKLabel _countLabel;
        protected VariableListView _listView;
        protected VariableRowManager manager;
        protected string PathToUxml => HyphlowConstants.PathToVariableDisplayEditorUxml;
        protected VisualTreeAsset uxml;

        protected readonly List<UnityObj> _toDestroy = new();

        [TearDown]
        public void TearDown()
        {
            manager?.Dispose();
            manager = null;

            AssetDatabase.DeleteAsset(TestAssetPath);

            try
            {
                _listView?.Dispose();
            }
            catch
            {
                /* best-effort cleanup */
            }

            foreach (var elem in _toDestroy)
            {
                if (Application.isEditor && elem != null)
                {
                    UnityObj.DestroyImmediate(elem);
                }
            }

            _toDestroy.Clear();
            _listView = null;
            _uiList = null;
            _countLabel = null;
            _source = null;
        }

        [Test]
        public void VariableAdded_Event_AddsRowTo_ListView()
        {
            // Add a string muscariable via the source API (raises VariableAdded)
            var added = _source.AddNewVariableOfContentType("myKey", "myVal");
            _listView.ForceMaterializeAllRowsForTests();

            int expectedRowCount = 2; // including the initial one from SetUp
            Assert.AreEqual(expectedRowCount, _listView.RowCount);

            var handler = HandlerAt(0) as StringRowVisualHandler;
            Assert.IsNotNull(handler, "Expected StringRowVisualHandler for added string variable.");

            // Verify the underlying value is present and matches startingVal
            var found = _source.GetVariableByName("myKey") as StringMuscariable;
            Assert.IsNotNull(found);
            Assert.AreEqual("myVal", found.Value);

        }

        [Test]
        public void VariableRemoved_Event_RemovesRowFrom_ListView()
        {
            // seed with one var
            var added = _source.AddNewVariableOfContentType<string>("toRemove", "bye");
            _listView.ForceMaterializeAllRowsForTests();
            int expectedRowCount = 2;
            Assert.AreEqual(expectedRowCount, _listView.RowCount);

            // Remove by key (raises VariableRemoved)
            _source.RemoveVariable("toRemove");
            expectedRowCount--;

            // Manager listens and should remove the corresponding row
            // Force materialization to ensure virtualization cleanup happened
            _listView.ForceMaterializeAllRowsForTests();

            Assert.AreEqual(expectedRowCount, _listView.RowCount);
            Assert.IsNull(_source.GetVariableByName("toRemove"));
        }

        [Test]
        public void Refresh_Event_RemovesNullEntries_And_UIUpdatedByObserver()
        {
            var secondVar = _source.AddNewVariableOfContentType("a", "1");
            var thirdVar = _source.AddNewVariableOfContentType("b", "2");
            _listView.ForceMaterializeAllRowsForTests();
            int expectedRowCount = 3; // including the initial one from SetUp
            Assert.AreEqual(expectedRowCount, _listView.RowCount, "The list view doesn't have as many rows as it should.");

            SimulateCorruptionByInjectingNull();
            void SimulateCorruptionByInjectingNull()
            {
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

                var sourceType = typeof(VariableSet);
                var varManagerInfo = sourceType.GetField("_varManager", flags);
                Assert.IsNotNull(varManagerInfo, "Could not find private '_varManager' field via reflection.");

                var varManager = varManagerInfo.GetValue(_source) as VariableManager;
                Assert.IsNotNull(varManager, "Could not read VariableManager from VariableSourceAsset.");

                var managerType = typeof(VariableManager);
                var muscariInfo = managerType.GetField("_muscariables", flags);
                Assert.IsNotNull(muscariInfo, "Could not find private '_muscariables' field via reflection.");

                var vars = muscariInfo.GetValue(varManager) as IList<Muscariable>;
                Assert.IsNotNull(vars);

                // set the first element to null (simulate external corruption)
                vars[0] = null;
                expectedRowCount--;
            }

            // The production inspector would respond to Refreshed; tests subscribe as an observer
            Action OnRefreshed = () =>
            {
                // Observer updates the UI when the source is refreshed
                _listView.SetVariables(_source.Variables);
                _listView.ForceMaterializeAllRowsForTests();
            };

            // Wire the observer to Refreshed
            _source.Refreshed += OnRefreshed;

            try
            {
                // Trigger Refresh which removes nulls and raises Refreshed event
                _source.Refresh();

                // After observer runs, UI should now reflect 1 remaining valid variable
                Assert.AreEqual(expectedRowCount, _listView.RowCount, "UI does not reflect just the remaining variable.");

                // The surviving variable should be the one whose reference wasn't nulled
                var remaining = _source.Variables;
                Assert.AreEqual(expectedRowCount, remaining.Count, "The wrong amount of vars remains.");
                Assert.IsNotNull(remaining[0], "The remaining var should not be null.");
            }
            finally
            {
                _source.Refreshed -= OnRefreshed;
            }
        }

        [Test]
        public void RemoveButton_RemovesVariableFromSourceAndListView()
        {
            // Arrange: add one var and materialize a row
            var secondVar = _source.AddNewVariableOfContentType("toRemove", "bye");
            _source.MarkDirtyAndSave();
            Assert.AreEqual(2, _listView.RowCount, "There aren't as many rows registered as there should be.");
            _listView.ForceMaterializeAllRowsForTests();
            var handler = HandlerAt(1) as StringRowVisualHandler;
            Assert.IsNotNull(handler);

            // Act: a click on the row's RemoveButton makes the handler call its host, which
            // is the same call made here (a Button can't be clicked outside of a panel)
            Assert.IsNotNull(handler.RowRoot.Q<Button>("RemoveButton"),
                "RemoveButton not found on the row template.");
            Selection.activeObject = _source; // So the list view can register the undo on it properly
            _listView.EditHost.RemoveRequested(handler.Variable);
            _source.Refresh();
            _source.MarkDirtyAndSave();

            // give the list a chance to react / cleanup if virtualized
            _listView.ForceMaterializeAllRowsForTests();

            // Assert: source and UI updated
            Assert.IsNull(_source.GetVariableByName("toRemove"), "Still has the var to remove after it should've been removed");
            // After removal the list should have one remaining (initial "greeting")
            Assert.AreEqual(1, _listView.RowCount);
        }

        [Test]
        public void AddNewVariable_DuplicateKey_IsMadeUnique()
        {
            // Arrange: add initial var with key "dup"
            var a = _source.AddNewVariableOfContentType<string>("dup", "one");
            // Act: add another with same suggested key
            var b = _source.AddNewVariableOfContentType<string>("dup", "two");

            // Assert: both exist with different keys
            var all = _source.Variables;
            int dupCount = all.Count(x => x.Key != null && x.Key.StartsWith("dup"));
            Assert.GreaterOrEqual(dupCount, 2, "Expected both variables whose keys start with 'dup' to be present");
            Assert.AreNotEqual(a.Key, b.Key, "Second variable should have been given a unique key");
        }

        [Test]
        public void GetVarsByContentType_ReturnsOnlyMatchingContentType()
        {
            // Arrange
            _source.AddNewVariableOfContentType<string>("s1", "s");
            _source.AddNewVariableOfContentType<int>("i1", 1);

            // Act
            var strings = _source.GetVarsByContentType<string>();
            var ints = _source.GetVarsByContentType<int>();

            // Assert
            Assert.IsTrue(strings.Any(x => x.Key == "s1"));
            Assert.IsFalse(strings.Any(x => x.Key == "i1"));
            Assert.IsTrue(ints.Any(x => x.Key == "i1"));
        }

        [Test]
        public void AddNewVariables_AssignsDistinctItemIDs()
        {
            var a = _source.AddNewVariableOfContentType<string>("a", "1");
            var b = _source.AddNewVariableOfContentType<string>("b", "2");

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.AreNotEqual(a.ItemId, b.ItemId);
            Assert.Greater(a.ItemId, 0);
            Assert.Greater(b.ItemId, 0);
        }

    }
}