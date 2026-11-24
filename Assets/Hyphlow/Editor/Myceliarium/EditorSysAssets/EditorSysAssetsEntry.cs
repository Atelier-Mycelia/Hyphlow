using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for the icons and config that <see cref="HyphlowEditorSysAssets"/>
    /// holds. The user edits a working copy (owned by <see cref="EditorSysAssetsLogic"/>)
    /// that only reaches the real asset when saving.
    /// </summary>
    public sealed class EditorSysAssetsEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Editor Icons";

        protected override Type SidebarTabType => typeof(EditorSysAssetsTab);
        protected override Type SubwindowType => typeof(EditorSysAssetsSubwindow);
        protected override Type LoaderType => typeof(EditorSysAssetsLoader);
        protected override Type LogicType => typeof(EditorSysAssetsLogic);

        private EditorSysAssetsSubwindow SysAssetsSubwindow => _subwindow as EditorSysAssetsSubwindow;
        private EditorSysAssetsLogic SysAssetsLogic => Logic as EditorSysAssetsLogic;

        /// <summary>
        /// The copy the user is editing, which isn't necessarily what's on disk.
        /// </summary>
        public HyphlowEditorSysAssets WorkingState => SysAssetsLogic?.Working;

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            EditorSysAssetsSubwindow subwindow = SysAssetsSubwindow;
            EditorSysAssetsLogic logic = SysAssetsLogic;
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
            SysAssetsSubwindow?.ShowState(WorkingState);
        }

        public override void Dispose()
        {
            // The logic destroys the working copy, which the subwindow's fields are bound to
            SysAssetsSubwindow?.Unbind();
            base.Dispose();
        }
    }
}
