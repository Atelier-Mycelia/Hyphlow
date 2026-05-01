using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Type = System.Type;

namespace AtMycelia.Hyphlow.EditorUtils
{
    /// <summary>
    /// Searchable Popup Window for selecting Event type, used by block editor
    /// </summary>
    public class EventSelectorPopupWindowContent : BasePopupWindowContent
    {
        static readonly List<Type> _eventHandlerTypes = new List<Type>();
        static IReadOnlyList<Type> EventHandlerTypes
        {
            get
            {
                if (_eventHandlerTypes == null || _eventHandlerTypes.Count == 0)
                    RefreshEventHandlerCache();

                return _eventHandlerTypes;
            }
        }

        static void RefreshEventHandlerCache()
        {
            _eventHandlerTypes.Clear();
            var derivedTypes = EditorExtensions.FindDerivedTypes(typeof(EventHandler));
            for (int i = 0; i < derivedTypes.Length; i++)
            {
                var elem = derivedTypes[i];
                bool canInstantiate = !elem.IsAbstract && !elem.IsInterface;
                if (canInstantiate)
                {
                    _eventHandlerTypes.Add(elem);
                }
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            RefreshEventHandlerCache();
        }

        protected class SetEventHandlerOperation
        {
            public Block block;
            public Type eventHandlerType;
        }

        protected Block _block;
        public EventSelectorPopupWindowContent(string currentHandlerName, Block block, int width, int height)
            :base(currentHandlerName, width, height, true)
        {
            this._block = block;
        }

        protected override void PrepareAllItems()
        {
            int i = 0;
            foreach (var item in EventHandlerTypes)
            {
                var info = EventHandlerEditor.GetEventHandlerInfo(item);
                if (info != null)
                {
                    var obsAttr = item.GetCustomAttribute<System.ObsoleteAttribute>();

                    var fliStr = (info.Category.Length > 0 ? info.Category + CATEGORY_CHAR : "")
                        + (obsAttr != null ? HyphlowConstants.UIPrefixForDeprecated_RichText : "")
                        + info.EventHandlerName;
                    allItems.Add(new FilteredListItem(i, fliStr, info.HelpText));
                }

                i++;
            }
        }
        
        override protected void SelectByOrigIndex(int index)
        {
            SetEventHandlerOperation operation = new SetEventHandlerOperation();
            operation.block = _block;
            operation.eventHandlerType = (index >= 0 && index < EventHandlerTypes.Count) ? EventHandlerTypes[index] : null;
            OnSelectEventHandler(operation);
        }


        static public void DoEventHandlerPopUp(Rect position, string currentHandlerName, Block block, int width, int height)
        {
            if (!HyphlowEditorPreferences.useLegacyMenus)
            {
                //new method
                EventSelectorPopupWindowContent win = new EventSelectorPopupWindowContent(currentHandlerName, block, width, height);
                PopupWindow.Show(position, win);
            }
            //old method
            DoOlderMenu(block);
        }

        static protected void DoOlderMenu(Block block)
        {
            SetEventHandlerOperation noneOperation = new SetEventHandlerOperation();
            noneOperation.block = block;
            noneOperation.eventHandlerType = null;

            GenericMenu eventHandlerMenu = new GenericMenu();
            eventHandlerMenu.AddItem(new GUIContent("None"), false, OnSelectEventHandler, noneOperation);

            // Add event handlers with no category first
            foreach (Type type in EventHandlerTypes)
            {
                EventHandlerInfoAttribute info = EventHandlerEditor.GetEventHandlerInfo(type);
                if (info != null &&
                    info.Category.Length == 0)
                {
                    SetEventHandlerOperation operation = new SetEventHandlerOperation();
                    operation.block = block;
                    operation.eventHandlerType = type;

                    eventHandlerMenu.AddItem(new GUIContent(info.EventHandlerName), false, OnSelectEventHandler, operation);
                }
            }

            // Add event handlers with a category afterwards
            foreach (Type type in EventHandlerTypes)
            {
                EventHandlerInfoAttribute info = EventHandlerEditor.GetEventHandlerInfo(type);
                if (info != null && info.Category.Length > 0)
                {
                    SetEventHandlerOperation operation = new SetEventHandlerOperation();
                    operation.block = block;
                    operation.eventHandlerType = type;
                    string typeName = $"{info.Category}/{info.EventHandlerName}";
                    eventHandlerMenu.AddItem(new GUIContent(typeName), false, OnSelectEventHandler, operation);
                }
            }

            eventHandlerMenu.ShowAsContext();
        }

        static protected void OnSelectEventHandler(object obj)
        {
            SetEventHandlerOperation operation = obj as SetEventHandlerOperation;
            Block block = operation.block;
            Type selectedType = operation.eventHandlerType;
            if (block == null)
            {
                return;
            }

            Undo.RecordObject(block, "Set Event Handler");

            if (block._EventHandler != null)
            {
                Undo.DestroyObjectImmediate(block._EventHandler);
            }

            if (selectedType != null)
            {
                EventHandler newHandler = Undo.AddComponent(block.gameObject, selectedType) as EventHandler;
                newHandler.ParentBlock = block;
                block._EventHandler = newHandler;
                EditorUtility.SetDirty(block);
            }

            BlockEditor.SelectedBlockDataStale = true;

            // Because this is an async call, we need to force prefab instances to record changes
            PrefabUtility.RecordPrefabInstancePropertyModifications(block);
        }
    }
}