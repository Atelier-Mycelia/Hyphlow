using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UIToolkitLabel = UnityEngine.UIElements.Label;

namespace AtMycelia.Hyphlow.EditorExt
{
    public class FlowchartSearchPanel : IDisposable
    {
        // Settings
        protected static readonly int _resultItemHeight = 20, _resultListHeight = 200,
            _searchFieldMarginBottom = 4;

        public FlowchartSearchPanel(Flowchart toSearchFor)
        {
            _flowchart = toSearchFor;
            Root = new VisualElement();

            BuildUI();
            RebindResults();
        }

        protected Flowchart _flowchart;
        protected IReadOnlyCollection<IBlock> AllBlocks
        {
            get => _flowchart != null ?
                _flowchart.Blocks :
                Array.Empty<IBlock>();
        }

        public VisualElement Root { get; }

        protected virtual void BuildUI()
        {
            PrepSearchField();
            void PrepSearchField()
            {
                _searchField = new ToolbarSearchField();
                _searchField.name = SearchFieldName;
                _searchField.style.marginBottom = _searchFieldMarginBottom;
                _searchField.value = ""; // To avoid certain null ref errors
            }

            PrepResultList();
            void PrepResultList()
            {
                _resultList = new ListView
                {
                    itemsSource = new List<IBlock>(),
                    fixedItemHeight = _resultItemHeight,
                    selectionType = SelectionType.Single,
                    style = { height = _resultListHeight }
                };
            }

            ListenForUiEvents();

            AddUIToRoot();
        }

        public static readonly string SearchFieldName = "FlowchartSearchField";

        protected ToolbarSearchField _searchField;
        protected ListView _resultList; // Shows Block Names

        protected virtual void ListenForUiEvents()
        {
            _searchField.RegisterValueChangedCallback(OnSearchFieldQueryChanged);
            _searchField.RegisterCallback<FocusOutEvent>(OnSearchFieldUnfocused);
            _resultList.makeItem += MakeItemForResultList;
            _resultList.bindItem += BindBlockToResultListItem;
            _resultList.selectionChanged += OnResultListSelectionChanged;
        }

        protected virtual void OnSearchFieldQueryChanged(ChangeEvent<string> changeEvent)
        {
            QueryChanged?.Invoke(changeEvent.newValue);
        }

        public event Action<string> QueryChanged = delegate { };

        protected virtual void OnSearchFieldUnfocused(FocusOutEvent evt)
        {
            Debug.Log("Search field lost focus");
            SearchFieldUnfocused(evt);
        }

        public event Action<FocusOutEvent> SearchFieldUnfocused = delegate { };

        protected virtual VisualElement MakeItemForResultList()
        {
            return new UIToolkitLabel();
        }

        protected virtual void BindBlockToResultListItem(VisualElement element, int index)
        {
            if (_flowchart == null) // This could happen right as Play Mode starts
            {
                return;
            }

            UIToolkitLabel uitkLabel = (UIToolkitLabel)element;
            IList<IBlock> blocksInResults = (IList<IBlock>)_resultList.itemsSource;
            IBlock currentBlock = blocksInResults[index];

            if (currentBlock != null)
            {
                uitkLabel.text = currentBlock.BlockName;
            }
        }

        protected virtual void OnResultListSelectionChanged(IEnumerable<object> blocks)
        {
            var selected = blocks.FirstOrDefault() as Block;
            if (selected != null)
            {
                BlockChosen(selected);
            }
        }
        public event Action<IBlock> BlockChosen = delegate { };

        protected virtual void AddUIToRoot()
        {
            IList<VisualElement> elementsToRegister = new List<VisualElement>()
            {
                _searchField, _resultList,
            };

            foreach (var element in elementsToRegister)
            {
                Root.Add(element);
            }
        }

        protected virtual void RebindResults()
        {
            IList<IBlock> resultsToShow = FilterUtils.FilterBlocks(AllBlocks, Query);

            _resultList.itemsSource = (System.Collections.IList)resultsToShow;
            _resultList.RefreshItems();
        }

        public virtual int ResultCount
        {
            get
            {
                if (_resultList == null)
                    return 0;
                return _resultList.childCount;
            }
        }

        public string Query
        {
            get => _searchField.value;
            set => _searchField.value = value;
        }

        public virtual void Dispose()
        {
            UnregisterUiCallbacks();
            if (Root.parent != null)
                Root.RemoveFromHierarchy();
        }

        protected virtual void UnregisterUiCallbacks()
        {
            _searchField.UnregisterValueChangedCallback(OnSearchFieldQueryChanged);
            _searchField.UnregisterCallback<FocusOutEvent>(OnSearchFieldUnfocused);
            _resultList.makeItem -= MakeItemForResultList;
            _resultList.bindItem -= BindBlockToResultListItem;
            _resultList.selectionChanged -= OnResultListSelectionChanged;
        }
    }
}