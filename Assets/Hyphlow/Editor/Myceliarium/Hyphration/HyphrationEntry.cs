using AtMycelia.Myceliarium;
using System;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for bringing projects made with earlier Hyphlow versions (or
    /// Fungus) up to date. For now that means the legacy variables in Flowcharts and
    /// Variable Sets.
    ///
    /// Unlike the other entries, there's no saver: migrating isn't an edit that waits for
    /// Save. It rewrites the chosen scenes and assets right away, once the user confirms.
    /// </summary>
    public sealed class HyphrationEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Hyphration";
        public override int SortingOrder => 200; // After the entries that manage settings

        protected override Type SidebarTabType => typeof(HyphrationTab);
        protected override Type SubwindowType => typeof(HyphrationSubwindow);
        protected override Type LoaderType => typeof(HyphrationLoader);
        protected override Type LogicType => typeof(HyphrationLogic);

        private HyphrationSubwindow HyphrationWindow => _subwindow as HyphrationSubwindow;
        private HyphrationLogic HyphrationState => Logic as HyphrationLogic;

        protected override void PrepareLogic()
        {
            base.PrepareLogic();

            // The subwindow was made before the logic, so it's only now there's state to show
            HyphrationWindow?.UseLogic(HyphrationState);
        }

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            HyphrationSubwindow subwindow = HyphrationWindow;
            if (subwindow == null)
            {
                return;
            }

            if (wantsSubsActive)
            {
                subwindow.RescanRequested += Rescan;
                subwindow.MigrateRequested += OnMigrateRequested;
            }
            else
            {
                subwindow.RescanRequested -= Rescan;
                subwindow.MigrateRequested -= OnMigrateRequested;
            }
        }

        private void Rescan()
        {
            // Loading again hands the logic a fresh scan, and refreshes the subwindow
            HandleLoading();
        }

        private void OnMigrateRequested()
        {
            HyphrationLogic logic = HyphrationState;
            var selected = logic.GetSelected();
            if (selected.Count == 0)
            {
                return;
            }

            if (!UserConfirms(logic))
            {
                return;
            }

            var results = HyphrationRunner.Run(selected);
            if (results == null)
            {
                return; // Didn't start, so there's nothing to log or show
            }

            logic.AddToLog(results);
            Rescan(); // Counts changed, and the sets that are done are now unselectable
            HyphrationWindow.ShowOverview();
            HyphrationWindow.ShowLatestLogEntry();
        }

        private static bool UserConfirms(HyphrationLogic logic)
        {
            string message = $"Hyphration is about to migrate {logic.SelectedSceneCount} " +
                $"scene(s) and {logic.SelectedSetCount} Variable Set(s), saving each one it " +
                "changes. This can't be undone from Unity, so make sure your work is " +
                "committed or backed up first.";
            return EditorUtility.DisplayDialog("Migrate with Hyphration?", message, "Migrate",
                "Cancel");
        }
    }
}
