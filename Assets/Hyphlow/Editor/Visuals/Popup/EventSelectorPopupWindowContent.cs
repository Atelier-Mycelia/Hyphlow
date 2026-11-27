using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using Type = System.Type;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Searchable Popup Window for selecting Event type, used by block editor. Lists both legacy
    /// (MonoBehaviour) EventHandlers and Hyphandlers.
    /// </summary>
    public class EventSelectorPopupWindowContent : BasePopupWindowContent
    {
        /// <summary>
        /// One selectable handler type, whichever kind it is.
        /// </summary>
        protected class HandlerEntry
        {
            public Type Type;
            public string Category;
            public string Name;
            public string HelpText;
            public int Order;
            public bool IsObsolete;

            public string MenuPath => Category.Length > 0 ?
                $"{Category}/{Name}" :
                Name;
        }

        static readonly List<HandlerEntry> _entries = new List<HandlerEntry>();
        protected static IReadOnlyList<HandlerEntry> Entries
        {
            get
            {
                if (_entries.Count == 0)
                    RefreshEntries();

                return _entries;
            }
        }

        static void RefreshEntries()
        {
            _entries.Clear();
            AddEntriesDerivedFrom(typeof(EventHandler));
            AddEntriesDerivedFrom(typeof(Hyphandler));

            _entries.Sort((a, b) =>
            {
                int byOrder = a.Order.CompareTo(b.Order);
                return byOrder != 0 ?
                    byOrder :
                    string.CompareOrdinal(a.MenuPath, b.MenuPath);
            });
        }

        /// <summary>
        /// Both kinds share EventHandlerInfoAttribute, so they're filtered the same way the variable
        /// type menu filters VariableInfoAttribute: a legacy handler with a Hyphandler replacement
        /// hides itself with ShowInMenu = false, and test types never show.
        /// </summary>
        static void AddEntriesDerivedFrom(Type baseType)
        {
            var derivedTypes = EditorExtensions.FindDerivedTypes(baseType);
            for (int i = 0; i < derivedTypes.Length; i++)
            {
                Type type = derivedTypes[i];
                EventHandlerInfoAttribute info = CanInstantiate(type) ?
                    EventHandlerEditor.GetEventHandlerInfo(type) :
                    null;
                bool belongsInMenu = info != null && info.ShowInMenu && !info.IsTest;
                if (!belongsInMenu)
                {
                    continue;
                }

                _entries.Add(new HandlerEntry
                {
                    Type = type,
                    Category = info.Category ?? "",
                    Name = info.EventHandlerName,
                    HelpText = info.HelpText,
                    Order = info.Order,
                    IsObsolete = type.GetCustomAttribute<System.ObsoleteAttribute>() != null,
                });
            }
        }

        static bool CanInstantiate(Type type) => !type.IsAbstract && !type.IsInterface;

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            RefreshEntries();
        }

        protected class SetEventHandlerOperation
        {
            public IBlock block;
            public Type eventHandlerType;
        }

        protected IBlock _block;
        public EventSelectorPopupWindowContent(string currentHandlerName, IBlock block, int width, int height)
            :base(currentHandlerName, width, height, true)
        {
            this._block = block;
        }

        protected override void PrepareAllItems()
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                HandlerEntry entry = Entries[i];
                string itemText = (entry.Category.Length > 0 ? entry.Category + _CATEGORY_CHAR : "")
                    + (entry.IsObsolete ? HyphlowConstants.UIPrefixForDeprecated_RichText : "")
                    + entry.Name;
                _allItems.Add(new FilteredListItem(i, itemText, entry.HelpText));
            }
        }

        // The base sorts alphabetically, which would undo Order; it only breaks ties here
        protected override int CompareItems(FilteredListItem lhs, FilteredListItem rhs)
        {
            int byOrder = OrderOf(lhs).CompareTo(OrderOf(rhs));
            return byOrder != 0 ?
                byOrder :
                base.CompareItems(lhs, rhs);

            static int OrderOf(FilteredListItem item)
            {
                return item.origIndex >= 0 && item.origIndex < Entries.Count ?
                    Entries[item.origIndex].Order :
                    0;
            }
        }

        override protected void SelectByOrigIndex(int index)
        {
            SetEventHandlerOperation operation = new SetEventHandlerOperation();
            operation.block = _block;
            operation.eventHandlerType = (index >= 0 && index < Entries.Count) ?
                Entries[index].Type :
                null;
            OnSelectEventHandler(operation);
        }

        static public void DoEventHandlerPopUp(Rect position, string currentHandlerName, Block block, int width, int height)
        {
            if (!HyphlowEditorPreferences.useLegacyMenus)
            {
                EventSelectorPopupWindowContent win = new EventSelectorPopupWindowContent(currentHandlerName, block, width, height);
                PopupWindow.Show(position, win);
            }
            else
            {
                DoOlderMenu(block);
            }
        }

        static protected void DoOlderMenu(Block block)
        {
            SetEventHandlerOperation noneOperation = new SetEventHandlerOperation();
            noneOperation.block = block;
            noneOperation.eventHandlerType = null;

            GenericMenu eventHandlerMenu = new GenericMenu();
            eventHandlerMenu.AddItem(new GUIContent("None"), false, OnSelectEventHandler, noneOperation);

            // Handlers with no category first, then the categorized ones
            AddMenuItems(withCategory: false);
            AddMenuItems(withCategory: true);

            void AddMenuItems(bool withCategory)
            {
                foreach (HandlerEntry entry in Entries)
                {
                    if ((entry.Category.Length > 0) != withCategory)
                    {
                        continue;
                    }

                    SetEventHandlerOperation operation = new SetEventHandlerOperation();
                    operation.block = block;
                    operation.eventHandlerType = entry.Type;
                    eventHandlerMenu.AddItem(new GUIContent(entry.MenuPath), false, OnSelectEventHandler, operation);
                }
            }

            eventHandlerMenu.ShowAsContext();
        }

        static protected void OnSelectEventHandler(object obj)
        {
            SetEventHandlerOperation operation = obj as SetEventHandlerOperation;
            IBlock block = operation.block;
            Type selectedType = operation.eventHandlerType;
            Block legacyBlock = block as Block;
            if (legacyBlock == null)
            {
                return;
            }

            Undo.RecordObject(block.Owner, "Set Event Handler");
            // A Hyphandler is data on the Block, so recording the Block is what makes swapping it undoable
            Undo.RecordObject(legacyBlock, "Set Event Handler");

            if (block.EventHandler is UnityObj legacyHandler)
            {
                Undo.DestroyObjectImmediate(legacyHandler);
            }
            else if (block.EventHandler is Hyphandler)
            {
                block.EventHandler = null;
            }

            if (selectedType != null && typeof(Hyphandler).IsAssignableFrom(selectedType))
            {
                // nonPublic, since [SerializeReference] types don't need a public constructor
                var newHyphandler = System.Activator.CreateInstance(selectedType, nonPublic: true) as Hyphandler;
                block.EventHandler = newHyphandler;
            }
            else if (selectedType != null)
            {
                EventHandler newHandler = Undo.AddComponent(legacyBlock.gameObject, selectedType) as EventHandler;
                newHandler.ParentBlock = block;
                block.EventHandler = newHandler;
            }

            EditorUtility.SetDirty(legacyBlock);
            BlockEditor.SelectedBlockDataStale = true;

            // Because this is an async call, we need to force prefab instances to record changes
            PrefabUtility.RecordPrefabInstancePropertyModifications(legacyBlock);
        }
    }
}
