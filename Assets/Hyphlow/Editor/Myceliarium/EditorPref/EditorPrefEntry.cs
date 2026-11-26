using AtMycelia.Myceliarium;
using System;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for Hyphlow's per-machine editor preferences, which are kept in
    /// EditorPrefs rather than in a project asset. The user edits a working copy (owned by
    /// <see cref="EditorPrefLogic"/>) that only reaches EditorPrefs when saving.
    /// </summary>
    public sealed class EditorPrefEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Editor Prefs";

        protected override Type SidebarTabType => typeof(EditorPrefTab);
        protected override Type SubwindowType => typeof(EditorPrefSubwindow);
        protected override Type LoaderType => typeof(EditorPrefLoader);
        protected override Type LogicType => typeof(EditorPrefLogic);

        private EditorPrefSubwindow PrefSubwindow => _subwindow as EditorPrefSubwindow;
        private EditorPrefLogic PrefLogic => Logic as EditorPrefLogic;

        /// <summary>
        /// The copy the user is editing, which isn't necessarily what's in EditorPrefs.
        /// </summary>
        internal EditorPrefData WorkingState => PrefLogic?.Working;

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            EditorPrefSubwindow subwindow = PrefSubwindow;
            EditorPrefLogic logic = PrefLogic;
            if (subwindow == null || logic == null)
            {
                return;
            }

            if (wantsSubsActive)
            {
                subwindow.StateMayHaveChanged += logic.RecordPendingChange;
                logic.StateRestoredByHistory += ShowWorkingState;
            }
            else
            {
                subwindow.StateMayHaveChanged -= logic.RecordPendingChange;
                logic.StateRestoredByHistory -= ShowWorkingState;
            }
        }

        protected override void OnLoadingDone()
        {
            base.OnLoadingDone();
            ShowWorkingState();
        }

        private void ShowWorkingState()
        {
            PrefSubwindow?.ShowState(WorkingState);
        }

        public override void Dispose()
        {
            // The logic destroys the working copy, which the subwindow's fields are bound to
            PrefSubwindow?.Unbind();
            base.Dispose();
        }
    }
}
