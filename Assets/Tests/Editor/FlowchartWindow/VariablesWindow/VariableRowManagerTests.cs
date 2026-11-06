using AtMycelia.EditorExt;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UITKLabel = UnityEngine.UIElements.Label;
using UnityObject = UnityEngine.Object;

namespace VScriptingTests.VariableOperations
{
    public class VariableRowManagerTests
    {
        [SetUp]
        public virtual void SetUp()
        {
            PrepFlowchart();
            PrepUIElements();
        }

        protected virtual void PrepUIElements()
        {
            string pathToUxml = HyphlowConstants.PathToVariableDisplayEditorUxml;
            _rootTemplate = Resources.Load<VisualTreeAsset>(pathToUxml);
            _root = _rootTemplate.CloneTree();
            _listContainer = _root.Q<ListView>("rowList");
            _countLabel = _root.Q<UITKLabel>("varCountLabel");
            _addButton = _root.Q<Button>("addVarButton");

            _handlerPool = new RowVisualHandlerPool(new RowVisualHandlerResolver(),
                RowVisualHandlerRegistry.VisualHandlerLookup);

            _firstListView = new VariableListView(new VariableListViewInitArgs()
            {
                List = _listContainer,
                CountLabel = _countLabel,
                VariableSource = _firstFc,
                HandlerPool = _handlerPool,
            });

            _rowManager = new VariableRowManager();
            _rowManager.Init(new VRowManagerInitArgs()
            {
                Root = _root,
                AddButton = _addButton,
                VariableSource = _firstFc,
                VariableListView = _firstListView,
            });
        }

        protected RowVisualHandlerPool _handlerPool;

        protected VisualTreeAsset _rootTemplate;
        protected VisualElement _root;
        protected ListView _listContainer;
        protected UITKLabel _countLabel;
        protected Button _addButton;
        protected VariableListView _firstListView;
        protected VariableRowManager _rowManager;

        protected GameObject _fcHolder;
        protected Flowchart _firstFc;
        protected IList<IVariable> _initVars;

        protected virtual void PrepFlowchart()
        {
            _fcHolder = new GameObject("FC");
            _firstFc = _fcHolder.AddComponent<Flowchart>();

            var floatVar = _fcHolder.AddComponent<FloatVariable>();
            floatVar.Key = "floatVar";

            var stringVar = _fcHolder.AddComponent<StringVariable>();
            stringVar.Key = "stringVar";

            var intVar = _fcHolder.AddComponent<IntegerVariable>();
            intVar.Key = "intVar";

            var goVar = _fcHolder.AddComponent<GameObjectVariable>();
            goVar.Key = "goVar";

            var boolVar = _fcHolder.AddComponent<BooleanVariable>();
            boolVar.Key = "boolVar";

            _initVars = new List<IVariable>()
            {
                floatVar, stringVar, intVar, goVar, boolVar
            };

            foreach (var elem in _initVars)
                _firstFc.AddVariable(elem);
        }

        [TearDown]
        public virtual void TearDown()
        {
            _rowManager?.Dispose();
            _rowManager = null;
            _handlerPool = null;

            _rootTemplate = null;
            _root = null;
            _listContainer = null;
            _countLabel = null;
            _addButton = null;
            _firstListView = null;
            _initVars?.Clear();

            UnityObject.DestroyImmediate(_fcHolder);
            _fcHolder = null;
            _firstFc = null;
        }

        [Test]
        public void Init_ShowsEveryVariableOfTheSource()
        {
            Assert.AreEqual(_firstFc.VariableCount, _firstListView.RowCount);
            Assert.AreEqual(string.Format(countLabelFormat, _firstFc.VariableCount), _countLabel.text);
        }

        [Test]
        public void MaterializedRows_UseTheHandlerRegisteredForTheirContentType()
        {
            _firstListView.ForceMaterializeAllRowsForTests();

            AssertHandlerTypeFor("floatVar", typeof(FloatRowVisualHandler));
            AssertHandlerTypeFor("stringVar", typeof(StringRowVisualHandler));
            AssertHandlerTypeFor("intVar", typeof(IntRowVisualHandler));
            AssertHandlerTypeFor("boolVar", typeof(BoolRowVisualHandler));
            AssertHandlerTypeFor("goVar", typeof(GameObjectRowVisualHandler));
        }

        private void AssertHandlerTypeFor(string key, Type expectedHandlerType)
        {
            IVariable variable = _firstFc.GetVariable(key, StringComparison.Ordinal);
            Assert.IsTrue(_firstListView.TryGetHandler(variable, out IRowVisualHandler handler),
                $"No materialized row for '{key}'.");
            Assert.IsInstanceOf(expectedHandlerType, handler);
        }

        [Test]
        public void RemovingVariables_UpdatesListAndCount_AndPoolsTheHandlers()
        {
            _firstListView.ForceMaterializeAllRowsForTests();
            Assert.AreEqual(0, _handlerPool.PooledHandlerCount, "Handler pool not empty at start.");

            _firstFc.RemoveVariable(_firstFc.Variables[0]);
            _firstFc.RemoveVariable(_firstFc.Variables[0]);

            Assert.AreEqual(_firstFc.VariableCount, _firstListView.RowCount);
            Assert.AreEqual(string.Format(countLabelFormat, _firstFc.VariableCount), _countLabel.text);
            // Rows are released after structural changes; with nothing on screen to rebind
            // them, every handler that was showing something is waiting in the pool.
            Assert.AreEqual(_initVars.Count, _handlerPool.PooledHandlerCount);
        }

        [Test]
        public void ClearingThenReAddingVariables_ReusesPooledHandlers()
        {
            _firstListView.ForceMaterializeAllRowsForTests();
            var originalVars = _firstFc.Variables.ToList();

            _firstFc.ClearVariables();
            Assert.AreEqual(0, _firstFc.VariableCount);
            Assert.AreEqual(originalVars.Count, _handlerPool.PooledHandlerCount);

            foreach (IVariable toAdd in originalVars)
            {
                _firstFc.AddVariable(toAdd);
            }
            _firstListView.ForceMaterializeAllRowsForTests();

            Assert.AreEqual(0, _handlerPool.PooledHandlerCount,
                "Every pooled handler should have been taken back out instead of creating new ones.");
            Assert.AreEqual(originalVars.Count, _firstListView.RowCount);
        }

        [Test]
        public void ValueEdited_ChangesTheVariable()
        {
            var muscariable = _firstFc.AddNewMuscariable<float, FloatMuscariable>("edited", 1f);

            _firstListView.EditHost.ValueEdited(muscariable, 12.5f);

            Assert.AreEqual(12.5f, muscariable.BoxedValue);
        }

        [Test]
        public void KeyEdited_ChangesTheVariable()
        {
            var muscariable = _firstFc.AddNewMuscariable<float, FloatMuscariable>("before", 1f);

            _firstListView.EditHost.KeyEdited(muscariable, "after");

            Assert.AreEqual("after", muscariable.Key);
        }

        [Test]
        public void ModelValueChange_UpdatesTheBoundRow_WithoutRebindingTheList()
        {
            var muscariable = _firstFc.AddNewMuscariable<float, FloatMuscariable>("live", 1f);
            _firstListView.ForceMaterializeAllRowsForTests();
            Assert.IsTrue(_firstListView.TryGetHandler(muscariable, out IRowVisualHandler handler));
            var valueField = handler.RowRoot.Q<FloatField>("ValueField");
            Assert.AreEqual(1f, valueField.value);

            muscariable.Value = 9f;

            Assert.AreEqual(9f, valueField.value);
            Assert.AreSame(muscariable, handler.Variable);
        }

        [Test]
        public void Dispose_ClearsAllAndUnsubscribes()
        {
            _firstFc.ClearVariables();
            _firstFc.AddNewVariable<float>("x");

            _rowManager.Dispose();

            // Manager disposed: list view should no longer update when Flowchart changes
            _firstFc.AddNewVariable<int>("y");

            // Flowchart variable count changed, but label should show a count of 0
            // (since disposing the manager implies releasing its rows)
            Assert.AreEqual("Count: 0", _countLabel.text, "List still counting rows despite disposal");
            Assert.AreEqual(0, _handlerPool.PooledHandlerCount);
        }

        [Test]
        public void Init_CanBeCalledMultipleTimesSafely()
        {
            // First manager already initialized in SetUp with _firstFc
            int initialVariableCount = _firstFc.VariableCount;

            // Re-init with second flowchart
            var newHolder = new GameObject("FC_Second");
            var secondFc = newHolder.AddComponent<Flowchart>();
            var secondView = new VariableListView(new VariableListViewInitArgs()
            {
                List = new ListView(),
                CountLabel = new UITKLabel(),
            });
            _rowManager.Init(new VRowManagerInitArgs()
            {
                Root = new VisualElement(),
                AddButton = new Button(),
                VariableSource = secondFc,
                VariableListView = secondView,
            });

            // Add variable to second flowchart; should not affect first flowchart's variable collection
            secondFc.AddNewVariable<bool>("second_bool");
            Assert.AreEqual(1, secondFc.VariableCount);
            Assert.AreEqual(1, secondView.RowCount, "The re-initialized manager should follow the new source.");
            Assert.AreEqual(initialVariableCount, _firstFc.VariableCount,
                "Original flowchart variable count changed unexpectedly.");

            // Re-init with third flowchart
            var thirdHolder = new GameObject("FC_Third");
            var thirdFc = thirdHolder.AddComponent<Flowchart>();
            var thirdView = new VariableListView(new VariableListViewInitArgs()
            {
                List = new ListView(),
                CountLabel = new UITKLabel(),
            });
            _rowManager.Init(new VRowManagerInitArgs
            {
                Root = new VisualElement(),
                AddButton = new Button(),
                VariableSource = thirdFc,
                VariableListView = thirdView,
            });

            thirdFc.AddNewVariable<bool>("third_bool");
            Assert.AreEqual(1, thirdFc.VariableCount);
            Assert.AreEqual(1, thirdView.RowCount);
            secondFc.AddNewVariable<bool>("another_second_bool");
            Assert.AreEqual(1, secondView.RowCount, "The manager should have let go of the second flowchart.");

            UnityObject.DestroyImmediate(newHolder);
            UnityObject.DestroyImmediate(thirdHolder);
        }

        protected static readonly string countLabelFormat = "Count: {0}";
    }

    [RowVisualHandler("Null", typeof(FakeHandlerWithBadPath), "5ryw45y", "_EditorResources/Uxml/VarRows/BadPathRow")]
    public class FakeHandlerWithBadPath : RowVisualHandler<FakeHandlerWithBadPath>
    {
        public static bool SuppressTemplateErrorsForTests = true;
    }

    public sealed class SuppressLogsScope : IDisposable
    {
        readonly bool prev;
        public SuppressLogsScope()
        {
            prev = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
        }
        public void Dispose() => LogAssert.ignoreFailingMessages = prev;
    }

    public class SilentTestResolver : IRowVisualHandlerResolver
    {
        public virtual Type ResolveHandler(IDictionary<Type, Type> visualHandlerLookup, Type contentType)
        {
            var filtered = visualHandlerLookup
                .Where(kvp => !typeof(FakeHandlerWithBadPath).IsAssignableFrom(kvp.Value))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (filtered.TryGetValue(contentType, out var direct))
                return direct;

            var candidates = filtered.Keys
                .Where(baseType => baseType.IsAssignableFrom(contentType))
                .Select(bt => new { Base = bt, Dist = Distance(bt, contentType) })
                .OrderBy(x => x.Dist)
                .ToList();

            if (candidates.Any())
                return filtered[candidates.First().Base];

            if (filtered.TryGetValue(typeof(object), out var obj))
                return obj;

            throw new InvalidOperationException($"No handler for {contentType.Name}.");
        }

        static int Distance(Type baseType, Type derived)
        {
            int d = 0;
            for (var t = derived; t != null && t != baseType; t = t.BaseType) d++;
            return d;
        }
    }
}
