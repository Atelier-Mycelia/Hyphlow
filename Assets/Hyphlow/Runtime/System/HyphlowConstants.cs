using UnityEngine;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Global constants used in various parts of Hyphlow.
    /// </summary>
    public static class HyphlowConstants
    {
        /// <summary>
        /// Duration of fade for executing icon displayed beside blocks & commands.
        /// </summary>
        public const float ExecutingIconFadeTime = 0.5f;

        /// <summary>
        /// The current version of the Flowchart. Used for updating components.
        /// </summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// The default choice block color.
        /// </summary>
        public static Color DefaultChoiceBlockTint = new Color(1.0f, 0.627f, 0.313f, 1.0f);

        /// <summary>
        /// The default event block color.
        /// </summary>
        public static Color DefaultEventBlockTint = new Color(0.784f, 0.882f, 1.0f, 1.0f);

        /// <summary>
        /// The default process block color.
        /// </summary>
        public static Color DefaultProcessBlockTint = new Color(1.0f, 0.882f, 0.0f, 1.0f);

        public const string UIPrefixForDeprecated = "[DEP] ";
        public const string UIPrefixForDeprecated_RichText = "<color=yellow>" + UIPrefixForDeprecated + "</color>";


        public const string PathToDefaultTweenAdapter = "Runtime/DefaultTweenAdapter";

        public const string PathToSaveSysDefaultsFolder = "SaveSys/Defaults";
        public const string PathToRuntimeResourceFolder = "Runtime";
        public const string PathToEditorResourceFolder = "Editor";

        public static IStringVarSubstitutor DefaultStringVarSubstitutor => StringVarSubstitutionService.Shared;

        /// <summary>
        /// This is relative to a Resources folder.
        /// </summary>
        public static string PathToVariableDisplayEditorUxml
        {
            get
            {
                #if UNITY_EDITOR
                return $"{PathToEditorResourceFolder}/UIToolkitTemplates/VariableDisplayEditor";
#else
                string warningMessage = $"Trying to access editor-only resource {nameof(PathToVariableDisplayEditorUxml)} at runtime. This is not supported.";
                Debug.LogWarning(warningMessage);
                return null;
#endif
            }
        }

        /// <summary>
        /// The default name of the Input EventSystem, stored in the resources folder.
        /// </summary>
        public const string EventSystemPrefabName =
#if ENABLE_INPUT_SYSTEM
            "Prefabs/EventSystem_NewInputSystem";
#else
            "Prefabs/EventSystem";
#endif

        
    }
}
