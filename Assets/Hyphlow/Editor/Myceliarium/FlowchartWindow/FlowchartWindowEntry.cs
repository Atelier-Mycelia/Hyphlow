using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for <see cref="FlowchartWindowConfig"/>. The user edits a working
    /// copy (owned by <see cref="FlowchartWindowLogic"/>) that only reaches the real asset
    /// when saving.
    /// </summary>
    public sealed class FlowchartWindowEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Flowchart Window";

        protected override Type SidebarTabType => typeof(FlowchartWindowTab);
        protected override Type SubwindowType => typeof(FlowchartWindowSubwindow);
        protected override Type LoaderType => typeof(FlowchartWindowLoader);
        protected override Type LogicType => typeof(FlowchartWindowLogic);

        private FlowchartWindowSubwindow WindowSubwindow => _subwindow as FlowchartWindowSubwindow;
        private FlowchartWindowLogic WindowLogic => Logic as FlowchartWindowLogic;

        /// <summary>
        /// The copy the user is editing, which isn't necessarily what's on disk.
        /// </summary>
        public FlowchartWindowConfig WorkingState => WindowLogic?.Working;

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            FlowchartWindowSubwindow subwindow = WindowSubwindow;
            FlowchartWindowLogic logic = WindowLogic;
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
            WindowSubwindow?.ShowState(WorkingState);
        }

        public override void Dispose()
        {
            // The logic destroys the working copy, which the subwindow's fields are bound to
            WindowSubwindow?.Unbind();
            base.Dispose();
        }
    }
}
