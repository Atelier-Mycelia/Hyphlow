using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;
using UnityObj = UnityEngine.Object;
using Type = System.Type;
using System.Reflection;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Searchable popup window content used to add an IVariable to a Flowchart.
    /// Mirrors CommandSelectorPopupWindowContent pattern.
    /// </summary>
    public class VariableSelectPopupWindowContent : BasePopupWindowContent
    {
        /// <summary>
        /// The variable types offered for adding, one per content type. Both the searchable popup
        /// and the context menu list these, and the popup's item indexes point into this list.
        /// Lazily (re)cached.
        /// </summary>
        protected static IReadOnlyList<Type> MenuTypes
        {
            get
            {
                if (_menuTypes == null || _menuTypes.Count == 0)
                {
                    RefreshVariableTypeCache();
                }
                return _menuTypes;
            }
        }

        /// <summary>
        /// Flowchart in context for adding variables. (Set by DoAddVariable / legacy menu path.)
        /// </summary>
        protected static Flowchart _curFlowchart;
        protected static IMuscariableSource _curSource;

        #region Lifecycle & Caching

        [UnityEditor.Callbacks.DidReloadScripts]
        protected static void OnScriptsReloaded()
        {
            RefreshVariableTypeCache();
        }

        /// <summary>
        /// Refresh the cached list of variable types from the registry.
        /// </summary>
        protected static void RefreshVariableTypeCache()
        {
            // Only Muscariables: AddVariable can't create legacy (MonoBehaviour) variables, so
            // listing those would only offer duplicates of Muscariables, or options that fail.
            // Using the registry instead of a reflection scan for performance / determinism.
            var menuTypes = new List<Type>();
            var contentTypesListed = new HashSet<Type>();
            foreach (Type varType in VariableTypeRegistry.AllMuscariableTypes)
            {
                VariableInfoAttribute info = varType.GetCustomAttribute<VariableInfoAttribute>();
                bool belongsInMenu = info != null && info.ShowInMenu && !info.IsTest;
                // One option per content type, so two Muscariables wrapping the same type can't both show
                if (!belongsInMenu || !contentTypesListed.Add(info.ContentType))
                {
                    continue;
                }

                menuTypes.Add(varType);
            }

            _menuTypes = menuTypes;
        }

        protected static IReadOnlyList<Type> _menuTypes;

        #endregion

        #region Construction

        public VariableSelectPopupWindowContent(string currentHandlerName, int width, int height)
            : base(currentHandlerName, width, height)
        {
        }

        #endregion

        #region BasePopupWindowContent Overrides

        /// <summary>
        /// Populate the internal list for filtering / display.
        /// </summary>
        protected override void PrepareAllItems()
        {
            for (int typeIndex = 0; typeIndex < MenuTypes.Count; typeIndex++)
            {
                Type mType = MenuTypes[typeIndex];
                VariableInfoAttribute info = VariableEditor.GetVariableInfo(mType);
                // We're not going to worry about any of the types having an ObsoleteAttribute.
                // typeIndex is what SelectByOrigIndex gets back, so it must index MenuTypes.
                string displayLabel = MakeDisplayLabel(info);
                FilteredListItem listItem = new FilteredListItem(typeIndex, displayLabel);
                _allItems.Add(listItem);
            }
        }

        protected static string MakeDisplayLabel(VariableInfoAttribute info)
        {
            string result;
            if (info.Category.Length > 0)
            {
                result = string.Format(_displayLabelFormat, info.Category, info.OptionDisplayName);

            }
            else
            {
                result = info.OptionDisplayName;
            }

            return result;
        }

        protected static string _displayLabelFormat = "{0}/{1}";

        /// <summary>
        /// Called when user confirms a selection (keyboard enter, double click, etc).
        /// </summary>
        protected override void SelectByOrigIndex(int index)
        {
            if (index < 0 || index >= MenuTypes.Count)
                return;

            Type mType = MenuTypes[index];
            AddVariable(mType);
        }

        #endregion

        #region Public Entry Points

        /// <summary>
        /// Show variable add popup (new searchable version or legacy menu fallback).
        /// </summary>
        /// <param name="position">Anchor rect (button rect).</param>
        /// <param name="currentHandlerName">Optional pre-filter / search seed.</param>
        /// <param name="toAddVarTo">Target flowchart to add variable to.</param>
        /// <param name="onVarAdded">Optional callback after addition (currently unused).</param>
        public static void DoAddVariable(Rect position,
                                         string currentHandlerName,
                                         IMuscariableSource toAddVarTo)
        {
            _curSource = toAddVarTo;

            ShowMenu(position, currentHandlerName, toAddVarTo);
        }

        // One or the other, like CommandSelectorPopupWindowContent; showing both opened two menus at once
        private static void ShowMenu(Rect position, string currentHandlerName, IMuscariableSource toAddVarTo)
        {
            if (!HyphlowEditorPreferences.useLegacyMenus)
            {
                var win = new VariableSelectPopupWindowContent(currentHandlerName, POPUP_WIDTH, POPUP_HEIGHT);
                PopupWindow.Show(position, win);
            }
            else
            {
                ShowLegacyMenu(toAddVarTo);
            }
        }

        protected const int POPUP_WIDTH = 200;
        protected const int POPUP_HEIGHT = 200;

        public static void DoAddVariable(Rect position, string currentHandlerName,
            IMuscariableSource toAddVarTo, System.Action onVarAdded = null)
        {
            _curFlowchart = null;
            _curSource = toAddVarTo;
            ShowMenu(position, currentHandlerName, toAddVarTo);
        }
        #endregion

        #region Legacy Menu (Context GenericMenu)

        /// <summary>
        /// Build and show the old non-searchable menu variant.
        /// </summary>
        protected static void ShowLegacyMenu(Flowchart flowchart)
        {
            GenericMenu menu = PrepMenu(MenuTypes);
            menu.ShowAsContext();
        }

        private static GenericMenu PrepMenu(IReadOnlyList<Type> varTypes)
        {
            GenericMenu menu = new GenericMenu();

            IList<Type> typesWithCategory = varTypes.Where(TypeHasCategory).ToList();
            static bool TypeHasCategory(Type type)
            {
                var info = VariableEditor.GetVariableInfo(type);
                return info == null || !string.IsNullOrEmpty(info.Category);
            }

            IList<Type> uncategorized = varTypes.Where((elem) => !TypeHasCategory(elem)).ToList();

            // We want to list the uncategorized types first
            AddToMenu(uncategorized);
            void AddToMenu(IList<Type> typesToAdd)
            {
                foreach (var typeEl in typesToAdd)
                {
                    var info = VariableEditor.GetVariableInfo(typeEl);
                    string displayLabel = MakeDisplayLabel(info);
                    GUIContent label = new GUIContent(displayLabel);
                    menu.AddItem(label, false, AddVariable, typeEl);
                }
            }
            AddToMenu(typesWithCategory);
            return menu;
        }

        public static void ShowLegacyMenu(IVariableSource variableSource)
        {
            GenericMenu menu = PrepMenu(MenuTypes);
            menu.ShowAsContext();
        }

        #endregion

        #region Variable Creation

        /// <summary>
        /// Convenience overload for GenericMenu callback signature.
        /// </summary>
        public static void AddVariable(object obj)
        {
            AddVariable(obj, string.Empty);
        }

        public static void AddVariableToSource(object obj)
        {

        }

        private static UnityObj ResolveRecordTarget()
        {
            if (_curSource is Flowchart flowchart)
            {
                VariableManagerComponent manager = flowchart.GetComponent<VariableManagerComponent>();
                if (manager != null)
                {
                    return manager;
                }

                return flowchart;
            }

            return _curSource as UnityObj;
        }

        private static void MarkDirty(UnityObj target)
        {
            if (target == null)
            {
                return;
            }

            EditorUtility.SetDirty(target);

            if (target is Component component && component.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }

            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        /// <summary>
        /// Creates a new Variable of the supplied type on the active flowchart.
        /// Optionally attempts to place it after an existing variable with the suggested name.
        /// </summary>
        /// <param name="varTypeToAdd">Type expected.</param>
        /// <param name="suggestedName">Optional preferred key (used also to attempt positional insertion).</param>
        public static void AddVariable(object varTypeToAdd, string suggestedName)
        {
            if (varTypeToAdd is not Type variableType)
            {
                return;
            }

            UnityObj varSourceObj = ResolveRecordTarget();
            if (varSourceObj == null || _curSource == null)
            {
                return;
            }

            // We'll assume that the var type passed is a Muscariable instead of a legacy type
            VariableInfoAttribute info = VariableEditor.GetVariableInfo(variableType);
            Type muscariType = typeof(Muscariable);
            if (info == null || !muscariType.IsAssignableFrom(variableType))
            {
                Debug.LogError($"Type {variableType.Name} is not a Muscariable or does not " +
                    $"have a VariableInfo attribute.");
                return;
            }

            string typeName = info.ContentType.Name;
            if (typeName.Equals("Single"))
            {
                typeName = "Float";
            }

            VariableUndo.RecordObject(varSourceObj, _curSource, $"Add {typeName} Variable");
            _curSource.AddNewVariableOfContentType(info.ContentType, suggestedName);
            MarkDirty(varSourceObj);
        }

        #endregion
    }
}