using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control panel entry for configuring the global Flowchart settings. The user edits
    /// a working copy (owned by <see cref="FcGlobalDefaultsLogic"/>) that only reaches the
    /// real asset when saving.
    /// </summary>
    public sealed class FcGlobalDefaultsEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Flowchart Defaults";

        protected override Type SidebarTabType => typeof(FcGlobalDefaultsTab);
        protected override Type SubwindowType => typeof(FcGlobalDefaultsSubwindow);
        protected override Type LoaderType => typeof(FcGlobalDefaultsLoader);
        protected override Type LogicType => typeof(FcGlobalDefaultsLogic);

        private FcGlobalDefaultsSubwindow DefaultsSubwindow => _subwindow as FcGlobalDefaultsSubwindow;
        private FcGlobalDefaultsLogic DefaultsLogic => Logic as FcGlobalDefaultsLogic;

        /// <summary>
        /// The copy the user is editing, which isn't necessarily what's on disk.
        /// </summary>
        public FlowchartGlobalDefaults WorkingState => DefaultsLogic?.Working;

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            FcGlobalDefaultsSubwindow subwindow = DefaultsSubwindow;
            FcGlobalDefaultsLogic logic = DefaultsLogic;
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
            DefaultsSubwindow?.ShowState(WorkingState);
        }

        public override void Dispose()
        {
            // The logic destroys the working copy, which the subwindow's fields are bound to
            DefaultsSubwindow?.Unbind();
            base.Dispose();
        }
    }
}
