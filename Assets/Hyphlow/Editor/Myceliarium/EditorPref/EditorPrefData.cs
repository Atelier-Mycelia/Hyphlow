using AtMycelia.Hyphlow.EditorExt;
using UnityEngine;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// The values of <see cref="HyphlowEditorPreferences"/> as something the Editor Prefs
    /// entry's fields can bind to. It's only ever a working copy: it's never saved as an
    /// asset, and reaches EditorPrefs through <see cref="WriteToPreferences"/>.
    /// </summary>
    internal sealed class EditorPrefData : ScriptableObject
    {
        [SerializeField] internal bool _hideMushroomInHierarchy;
        [SerializeField] internal bool _useLegacyMenus;
        [SerializeField] internal bool _useGridSnap;
        [SerializeField] internal Color _flowchartBlockTint = Color.white;
        [SerializeField] internal Color _commandListTint = Color.white;
        [SerializeField] internal bool _suppressHelpBoxes;
        [SerializeField] internal bool _navigateCmdListWithArrows;

        public void ReadFromPreferences()
        {
            _hideMushroomInHierarchy = HyphlowEditorPreferences.hideMushroomInHierarchy;
            _useLegacyMenus = HyphlowEditorPreferences.useLegacyMenus;
            _useGridSnap = HyphlowEditorPreferences.useGridSnap;
            _flowchartBlockTint = HyphlowEditorPreferences.flowchartBlockTint;
            _commandListTint = HyphlowEditorPreferences.commandListTint;
            _suppressHelpBoxes = HyphlowEditorPreferences.suppressHelpBoxes;
            _navigateCmdListWithArrows = HyphlowEditorPreferences.navigateCmdListWithArrows;
        }

        /// <summary>
        /// Updates the fields the rest of the editor reads, and persists them.
        /// </summary>
        public void WriteToPreferences()
        {
            HyphlowEditorPreferences.hideMushroomInHierarchy = _hideMushroomInHierarchy;
            HyphlowEditorPreferences.useLegacyMenus = _useLegacyMenus;
            HyphlowEditorPreferences.useGridSnap = _useGridSnap;
            HyphlowEditorPreferences.flowchartBlockTint = _flowchartBlockTint;
            HyphlowEditorPreferences.commandListTint = _commandListTint;
            HyphlowEditorPreferences.suppressHelpBoxes = _suppressHelpBoxes;
            HyphlowEditorPreferences.navigateCmdListWithArrows = _navigateCmdListWithArrows;
            HyphlowEditorPreferences.Save();
        }
    }

}
