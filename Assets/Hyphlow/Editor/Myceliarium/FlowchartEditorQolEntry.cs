using AtMycelia.Myceliarium;
using AtMycelia.Hyphlow.EditorExt;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Control Panel entry for managing Flowchart Editor QoL assets. The user works on
    /// copies (owned by <see cref="FlowchartEditorQolLogic"/>) that only reach the
    /// project when saving, through <see cref="FlowchartEditorQolSaver"/>.
    /// </summary>
    public sealed class FlowchartEditorQolEntry : ControlPanelEntry, IAtMyceliaControlPanelEntry
    {
        public override string SortingName => "Editor QoL";

        protected override Type SidebarTabType => typeof(FlowchartEditorQolTab);
        protected override Type SubwindowType => typeof(FlowchartEditorQolSubwindow);
        protected override Type LoaderType => typeof(FlowchartEditorQolLoader);
        protected override Type LogicType => typeof(FlowchartEditorQolLogic);

        private FlowchartEditorQolSubwindow QolSubwindow => _subwindow as FlowchartEditorQolSubwindow;
        private FlowchartEditorQolLogic QolLogic => Logic as FlowchartEditorQolLogic;

        internal IReadOnlyList<FlowchartEditorQol> WorkingState => QolLogic?.Working;

        public IReadOnlyDictionary<string, FlowchartEditorQol> RealAssets => QolLogic?.RealAssets;

        protected override void PrepareLogic()
        {
            base.PrepareLogic();

            // The subwindow was made before the logic, so it's only now there's a list to show
            QolSubwindow?.UseWorkingList(QolLogic?.Working);
        }

        protected override void SetSubs(bool wantsSubsActive)
        {
            base.SetSubs(wantsSubsActive);

            FlowchartEditorQolSubwindow subwindow = QolSubwindow;
            FlowchartEditorQolLogic logic = QolLogic;
            if (subwindow == null || logic == null)
            {
                return;
            }

            if (wantsSubsActive)
            {
                subwindow.CreateRequested += OnCreateRequested;
                subwindow.RenameRequested += OnRenameRequested;
                subwindow.DeleteRequested += OnDeleteRequested;
                subwindow.SelectRequested += OnSelectRequested;
                subwindow.StateMayHaveChanged += logic.RecordPendingChange;
                logic.StateRestoredByHistory += subwindow.Refresh;
            }
            else
            {
                subwindow.CreateRequested -= OnCreateRequested;
                subwindow.RenameRequested -= OnRenameRequested;
                subwindow.DeleteRequested -= OnDeleteRequested;
                subwindow.SelectRequested -= OnSelectRequested;
                subwindow.StateMayHaveChanged -= logic.RecordPendingChange;
                logic.StateRestoredByHistory -= subwindow.Refresh;
            }
        }

        private void OnCreateRequested(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                EditorUtility.DisplayDialog("Invalid Name",
                    "Please enter a valid asset name.", "OK");
                return;
            }

            QolLogic.Create(name);
            QolSubwindow.Refresh();
        }

        private void OnRenameRequested(int index, string newName)
        {
            if (!ValidateIndex(index, "renaming"))
            {
                return;
            }

            if (!string.IsNullOrEmpty(newName))
            {
                QolLogic.Rename(index, newName);
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Name",
                    "Please enter a valid asset name.", "OK");
            }

            QolSubwindow.Refresh();
        }

        private void OnDeleteRequested(int index)
        {
            if (!ValidateIndex(index, "deleting"))
            {
                return;
            }

            QolLogic.Delete(index);
            QolSubwindow.Refresh();
        }

        private bool ValidateIndex(int index, string action)
        {
            if (QolLogic.IsValidIndex(index))
            {
                return true;
            }

            string logMessage = $"Invalid index {index} for {action} QoL asset. " +
                $"Valid range is 0 to {QolLogic.Working.Count - 1}.";
            Debug.LogError(logMessage);
            return false;
        }

        private void OnSelectRequested(int index)
        {
            if (!QolLogic.IsValidIndex(index))
            {
                return;
            }

            FlowchartEditorQol real = QolLogic.FindRealFor(index);
            if (real == null)
            {
                EditorUtility.DisplayDialog("No Real Asset Found",
                    $"No real asset found for working-state '{QolLogic.Working[index].name}'. " +
                    $"Need to click Save to make sure it is written on disk.",
                    "OK");
                return;
            }

            Selection.activeObject = real;
            EditorGUIUtility.PingObject(real);
        }
    }
}
