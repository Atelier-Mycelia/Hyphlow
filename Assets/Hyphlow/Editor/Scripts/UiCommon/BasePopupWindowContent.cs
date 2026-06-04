using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Common base for PopupWindowContent that is a search filterable list a la AddComponent
    /// 
    /// Inspired by https://github.com/roboryantron/UnityEditorJunkie/blob/master/Assets/SearchableEnum/Code/Editor/SearchablePopup.cs
    /// </summary>
    public abstract class BasePopupWindowContent : PopupWindowContent
    {
        /// <summary>
        /// Called when the user has confirmed an item from the menu.
        /// </summary>
        /// <param name="index">Index of into the original list of items to show given to the popupcontent</param>
        abstract protected void SelectByOrigIndex(int index);

        /// <summary>
        /// Called during Base Ctor, must fill allItems list so the ctor can continue to fill
        /// the visible items and current selected index.
        /// </summary>
        abstract protected void PrepareAllItems();

        /// <summary>
        /// Internal representation of 1 row of our popup list
        /// </summary>
        public class FilteredListItem
        {
            public FilteredListItem(int index, string str, string tip = "")
            {
                origIndex = index;
                name = str;
                lowerName = str.ToLowerInvariant();
                content = new GUIContent(str, tip);
            }
            public int origIndex;
            public string name, lowerName;
            public GUIContent content;
        }

        protected int _hoverIndex;
        protected readonly string _SEARCH_CONTROL_NAME = "PopupSearchControlName";
        protected readonly float _ROW_HEIGHT = EditorGUIUtility.singleLineHeight;
        protected List<FilteredListItem> _allItems = new List<FilteredListItem>(), 
            _visibleItems = new List<FilteredListItem>();
        protected string _currentFilter = string.Empty;
        protected Vector2 _scroll;
        protected int _scrollToIndex;
        protected float _scrollOffset;
        protected int _currentIndex;
        protected Vector2 _size;
        protected bool _hasNoneOption = false;

        static readonly char[] SEARCH_SPLITS = new char[]{ _CATEGORY_CHAR, ' ' };
        protected static readonly char _CATEGORY_CHAR = '/';

        public BasePopupWindowContent(string currentHandlerName, int width, int height, bool showNoneOption = false)
        {
            this._size = new Vector2(width, height);
            _hasNoneOption = showNoneOption;

            PrepareAllItems();

            _allItems.Sort((lhs, rhs) => 
            {
                //order root level objects first
                var islhsRoot = lhs.lowerName.IndexOf(_CATEGORY_CHAR) != -1;
                var isrhsRoot = rhs.lowerName.IndexOf(_CATEGORY_CHAR) != -1;

                if(islhsRoot == isrhsRoot)
                    return lhs.lowerName.CompareTo(rhs.lowerName);
                return islhsRoot ? 1 : -1;
            });
            UpdateFilter();
            _currentIndex = Mathf.Max(0, _visibleItems.FindIndex(x=>x.name.Contains(currentHandlerName)));
            _hoverIndex = _currentIndex;
        }

        public override void OnGUI(Rect rect)
        {
            Rect searchRect = new Rect(0, 0, rect.width, EditorStyles.toolbar.fixedHeight);
            Rect scrollRect = Rect.MinMaxRect(0, searchRect.yMax, rect.xMax, rect.yMax);

            GUI.skin.label.richText = true;

            HandleKeyboard();
            DrawSearch(searchRect);
            DrawSelectionArea(scrollRect);
        }

        public override Vector2 GetWindowSize()
        {
            return _size;
        }

        private void DrawSearch(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
                EditorStyles.toolbar.Draw(rect, false, false, false, false);

            Rect searchRect = new Rect(rect);
            searchRect.xMin += 6;
            searchRect.xMax -= 6;
            searchRect.y += 2;

            GUI.FocusControl(_SEARCH_CONTROL_NAME);
            GUI.SetNextControlName(_SEARCH_CONTROL_NAME);
            var prevFilter = _currentFilter;
            _currentFilter = GUI.TextField(searchRect, _currentFilter);

            if (prevFilter != _currentFilter)
            {
                UpdateFilter();
            }
        }

        private void UpdateFilter()
        {
            var curlower = _currentFilter.ToLowerInvariant();
            var lowers = curlower.Split(SEARCH_SPLITS);
            lowers = lowers.Where(x => x.Length > 0).ToArray();

            if (lowers == null || lowers.Length == 0)
            {
                _visibleItems.AddRange(_allItems);
            }
            else
            {
                _visibleItems = _allItems.Where(x =>
                {
                    //we want all tokens
                    foreach (var item in lowers)
                    {
                        if (!x.lowerName.Contains(item))
                            return false;
                    }
                    return true;
                }).ToList();
            }

            _hoverIndex = 0;
            _scroll = Vector2.zero;
            if(_hasNoneOption)
                _visibleItems.Insert(0, new FilteredListItem(-1, "None"));
        }

        private void DrawSelectionArea(Rect scrollRect)
        {
            Rect contentRect = new Rect(0, 0,
                scrollRect.width - GUI.skin.verticalScrollbar.fixedWidth,
                _visibleItems.Count * _ROW_HEIGHT);

            _scroll = GUI.BeginScrollView(scrollRect, _scroll, contentRect);

            Rect rowRect = new Rect(0, 0, scrollRect.width, _ROW_HEIGHT);

            for (int i = 0; i < _visibleItems.Count; i++)
            {
                if (_scrollToIndex == i &&
                    (Event.current.type == EventType.Repaint
                     || Event.current.type == EventType.Layout))
                {
                    Rect r = new Rect(rowRect);
                    r.y += _scrollOffset;
                    GUI.ScrollTo(r);
                    _scrollToIndex = -1;
                    _scroll.x = 0;
                }

                if (rowRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseMove ||
                        Event.current.type == EventType.ScrollWheel)
                    {
                        //if new item force update so it's snappier
                        if (_hoverIndex != 1)
                        {
                            this.editorWindow.Repaint();
                        }

                        _hoverIndex = i;
                    }

                    if (Event.current.type == EventType.MouseDown)
                    {
                        //onSelectionMade(list.Entries[i].Index);
                        SelectByOrigIndex(_visibleItems[i].origIndex);
                        EditorWindow.focusedWindow.Close();
                    }
                }

                DrawRow(rowRect, i);

                rowRect.y = rowRect.yMax;
            }

            GUI.EndScrollView();
        }

        private static void DrawBox(Rect rect, Color tint)
        {
            Color c = GUI.color;
            GUI.color = tint;
            GUI.Box(rect, "");
            GUI.color = c;
        }

        private void DrawRow(Rect rowRect, int i)
        {
            if (i == _currentIndex)
                DrawBox(rowRect, Color.cyan);
            else if (i == _hoverIndex)
                DrawBox(rowRect, Color.white);

            Rect labelRect = new Rect(rowRect);
            //labelRect.xMin += ROW_INDENT;

            GUI.Label(labelRect, _visibleItems[i].content);
        }

        /// <summary>
        /// Process keyboard input to navigate the choices or make a selection.
        /// </summary>
        private void HandleKeyboard()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    _hoverIndex = Mathf.Min(_visibleItems.Count - 1, _hoverIndex + 1);
                    Event.current.Use();
                    _scrollToIndex = _hoverIndex;
                    _scrollOffset = _ROW_HEIGHT;
                }

                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    _hoverIndex = Mathf.Max(0, _hoverIndex - 1);
                    Event.current.Use();
                    _scrollToIndex = _hoverIndex;
                    _scrollOffset = -_ROW_HEIGHT;
                }

                if (Event.current.keyCode == KeyCode.Return)
                {
                    if (_hoverIndex >= 0 && _hoverIndex < _visibleItems.Count)
                    {
                        SelectByOrigIndex(_visibleItems[_hoverIndex].origIndex);
                        EditorWindow.focusedWindow.Close();
                    }
                }

                if (Event.current.keyCode == KeyCode.Escape)
                {
                    EditorWindow.focusedWindow.Close();
                }
            }
        }
    }
}