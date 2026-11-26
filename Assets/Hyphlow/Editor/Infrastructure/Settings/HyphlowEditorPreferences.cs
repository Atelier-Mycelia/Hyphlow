using AtMycelia.Hyphlow.MyceliariumInt;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Holds Hyphlow's per-machine editor preferences (stored in EditorPrefs), which the
    /// rest of the editor code reads from the static fields. They're edited in the
    /// Control Panel's Editor Prefs entry.
    /// </summary>
    [InitializeOnLoad]
    public static class HyphlowEditorPreferences
    {
        #region Pref Values
        public static bool hideMushroomInHierarchy;
        public static bool useLegacyMenus;
        public static bool useGridSnap;
        public static Color commandListTint = Color.white;
        public static Color flowchartBlockTint = Color.white;
        public static bool suppressHelpBoxes = false;
        public static bool navigateCmdListWithArrows = false;
        #endregion

        #region Keys to Fetch Values from EditorPrefs
        private const string HIDE_MUSH_KEY = "hideMushroomInHierarchy";
        private const string USE_LEGACY_MENUS = "useLegacyMenus";
        private const string USE_GRID_SNAP = "useGridSnap";
        private const string COMMAND_LIST_ITEM_TINT = "commandListTint";
        private const string FLOWCHART_WINDOW_BLOCK_TINT = "flowchartWindowBlockTint";
        private const string SUPPRESS_HELP_BOXES = "suppressHelpBoxes";
        private const string NAV_CMD_WITH_ARROWS = "navigateCmdListWithArrows";
        #endregion

        static HyphlowEditorPreferences()
        {
            LoadFromUnityEditorPrefs();
        }

        #region Loading Prefs
        public static void LoadFromUnityEditorPrefs()
        {
            LoadFlags();
            LoadColors();
        }

        private static void LoadFlags()
        {
            hideMushroomInHierarchy = EditorPrefs.GetBool(HIDE_MUSH_KEY, false);
            useLegacyMenus = EditorPrefs.GetBool(USE_LEGACY_MENUS, true);
            // ^We want the default to be true due to the "non"-legacy menus being hard
            // to work with.
            useGridSnap = EditorPrefs.GetBool(USE_GRID_SNAP, false);
            suppressHelpBoxes = EditorPrefs.GetBool(SUPPRESS_HELP_BOXES, false);
            navigateCmdListWithArrows = EditorPrefs.GetBool(NAV_CMD_WITH_ARROWS, false);
        }

        private static void LoadColors()
        {
            string colorHtml = EditorPrefs.GetString(COMMAND_LIST_ITEM_TINT);
            bool colorParseSuccess = ColorUtility.TryParseHtmlString(colorHtml, out var col);
            if (colorParseSuccess)
            {
                commandListTint = col;
            }

            colorHtml = EditorPrefs.GetString(FLOWCHART_WINDOW_BLOCK_TINT);
            colorParseSuccess = ColorUtility.TryParseHtmlString(colorHtml, out col);
            if (colorParseSuccess)
            {
                flowchartBlockTint = col;
            }
        }
        #endregion

        #region Settings Provider
        
        /// <summary>
        /// The options now live in the Atelier Mycelia Control Panel (see EditorPrefEntry);
        /// this page stays only so that people who look for them here are pointed there.
        /// </summary>
        [SettingsProvider]
        public static SettingsProvider CreateHyphlowSettingsProvider()
        {
            SettingsProvider result = new SettingsProvider(_providerTabPath, SettingsScope.Project)
            {
                label = "Hyphlow",
                guiHandler = _ => PointerGUI()
            };
            return result;
        }

        private static string _providerTabPath = "Project/Hyphlow";

        private static void PointerGUI()
        {
            EditorGUILayout.HelpBox(_pointerText, MessageType.Info);
            bool controlPanelOpenRequested = GUILayout.Button(_buttonText, GUILayout.Width(_buttonWidth));
            if (controlPanelOpenRequested)
            {
                EditorApplication.ExecuteMenuItem(_menuItemPath);
            }
        }

        private static string _pointerText = "Hyphlow's editor preferences are now in the Atelier Mycelia " +
                "Control Panel. There, you can find it under Hyphow > Editor Prefs.";
        private static string _buttonText = "Open Control Panel";
        private static int _buttonWidth = 180;
        private static string _menuItemPath = "Window/Atelier Mycelia/Control Panel";

        #endregion

        #region Saving Prefs
        /// <summary>
        /// Writes the current values to EditorPrefs, so they outlive the editor session.
        /// </summary>
        public static void Save()
        {
            SaveFlags();
            SaveColors();
        }

        private static void SaveFlags()
        {
            EditorPrefs.SetBool(HIDE_MUSH_KEY, hideMushroomInHierarchy);
            EditorPrefs.SetBool(USE_LEGACY_MENUS, useLegacyMenus);
            EditorPrefs.SetBool(USE_GRID_SNAP, useGridSnap);
            EditorPrefs.SetBool(SUPPRESS_HELP_BOXES, suppressHelpBoxes);
            EditorPrefs.SetBool(NAV_CMD_WITH_ARROWS, navigateCmdListWithArrows);
        }

        private static void SaveColors()
        {
            string rawHtml = ColorUtility.ToHtmlStringRGBA(commandListTint);
            var colAsString = $"#{rawHtml}";
            EditorPrefs.SetString(COMMAND_LIST_ITEM_TINT, colAsString);

            rawHtml = ColorUtility.ToHtmlStringRGBA(flowchartBlockTint);
            colAsString = $"#{rawHtml}";
            EditorPrefs.SetString(FLOWCHART_WINDOW_BLOCK_TINT, colAsString);
        }
        #endregion
        
    }
    
}
