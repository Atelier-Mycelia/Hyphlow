using System.Collections.Generic;
using UnityEditor;
using Type = System.Type;

namespace AtMycelia.Myceliarium
{
    public class AtMyceliaControlPanel : ControlPanel
    {
        protected override string PathToUxml => "Editor/UIToolkitTemplates/ControlPanel";

        [MenuItem("Window/Atelier Mycelia/Control Panel")]
        public static void BringUp()
        {
            if (S != null)
            {
                S.Focus();
                return;
            }

            var wnd = GetWindow<AtMyceliaControlPanel>();
        }

        public static ControlPanel S { get; private set; }

        protected override IEnumerable<IControlPanelEntry> GetEntriesToAttach()
        {
            var filteredEntries = ControlPanelEntryRegistry.GetEntriesOfType(_forOurEcosys);
            return filteredEntries;
        }

        private static readonly Type _forOurEcosys = typeof(IAtMyceliaControlPanelEntry);

    }
}