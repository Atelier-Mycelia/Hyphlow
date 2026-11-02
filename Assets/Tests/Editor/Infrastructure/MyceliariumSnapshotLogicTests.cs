using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Hyphlow.MyceliariumInt;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;

namespace AtMycelia.Hyphlow.Tests
{
    /// <summary>
    /// The entries whose controls are bound straight to working copies get their
    /// undo and redo from snapshots, so these check that part of the logic modules.
    /// </summary>
    public class MyceliariumSnapshotLogicTests
    {
        private FcGlobalDefaultsLogic _defaults;
        private FlowchartEditorQolLogic _qol;

        [TearDown]
        public void TearDown()
        {
            _defaults?.Dispose();
            _qol?.Dispose();
        }

        private FcGlobalDefaultsLogic StartDefaults()
        {
            _defaults = new FcGlobalDefaultsLogic();
            _defaults.Load(FlowchartGlobalDefaults.S);
            return _defaults;
        }

        private FlowchartEditorQolLogic StartQol()
        {
            _qol = new FlowchartEditorQolLogic();
            _qol.Load(new List<FlowchartEditorQol>());
            return _qol;
        }

        private static void SetFirstBlockName(FlowchartGlobalDefaults working, string value)
        {
            var so = new SerializedObject(working);
            so.FindProperty("_firstBlockName").stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string FirstBlockName(FlowchartGlobalDefaults working)
        {
            return new SerializedObject(working).FindProperty("_firstBlockName").stringValue;
        }

        #region Global defaults
        [Test]
        public void Defaults_AfterLoading_AreCleanAndLeaveTheRealAssetAlone()
        {
            FcGlobalDefaultsLogic logic = StartDefaults();

            Assert.IsTrue(logic.IsLoaded);
            Assert.IsFalse(logic.HasUnsavedChanges);
            Assert.AreNotSame(FlowchartGlobalDefaults.S, logic.Working);
        }

        [Test]
        public void Defaults_AnEdit_IsUnsaved_UntilUndone()
        {
            FcGlobalDefaultsLogic logic = StartDefaults();
            string original = FirstBlockName(logic.Working);

            SetFirstBlockName(logic.Working, original + "Edited");
            logic.RecordPendingChange();
            Assert.IsTrue(logic.HasUnsavedChanges);

            logic.Undo();
            Assert.AreEqual(original, FirstBlockName(logic.Working));
            Assert.IsFalse(logic.HasUnsavedChanges);

            logic.Redo();
            Assert.AreEqual(original + "Edited", FirstBlockName(logic.Working));
            Assert.IsTrue(logic.HasUnsavedChanges);
        }

        [Test]
        public void Defaults_RecordingWithoutAnEdit_AddsNothingToTheHistory()
        {
            FcGlobalDefaultsLogic logic = StartDefaults();

            logic.RecordPendingChange();

            Assert.IsFalse(logic.CanUndo);
        }

        [Test]
        public void Defaults_CommittingPendingEdits_PutsTheEditOnTheHistory()
        {
            FcGlobalDefaultsLogic logic = StartDefaults();

            SetFirstBlockName(logic.Working, "NotRecordedYet");
            logic.CommitPendingEdits();

            Assert.IsTrue(logic.CanUndo);
        }

        [Test]
        public void Defaults_UndoingAndRedoing_TellsTheUiToShowTheRestoredState()
        {
            FcGlobalDefaultsLogic logic = StartDefaults();
            int restored = 0;
            logic.StateRestoredByHistory += () => restored++;
            SetFirstBlockName(logic.Working, "Edited");
            logic.RecordPendingChange();

            logic.Undo();
            logic.Redo();

            Assert.AreEqual(2, restored);
        }
        #endregion

        #region Editor QoL
        [Test]
        public void Qol_CreatingAnAsset_StagesItUntilUndone()
        {
            FlowchartEditorQolLogic logic = StartQol();

            logic.Create("Staged");

            Assert.AreEqual(1, logic.Working.Count);
            Assert.AreEqual("Staged", logic.Working[0].name);
            Assert.IsTrue(logic.HasUnsavedChanges);

            logic.Undo();
            Assert.AreEqual(0, logic.Working.Count);
            Assert.IsFalse(logic.HasUnsavedChanges);

            logic.Redo();
            Assert.AreEqual("Staged", logic.Working[0].name);
        }

        [Test]
        public void Qol_RenamingAndDeleting_CanBeUndone()
        {
            FlowchartEditorQolLogic logic = StartQol();
            logic.Create("First");

            logic.Rename(0, "Renamed");
            Assert.AreEqual("Renamed", logic.Working[0].name);

            logic.Delete(0);
            Assert.AreEqual(0, logic.Working.Count);

            logic.Undo();
            Assert.AreEqual("Renamed", logic.Working[0].name);

            logic.Undo();
            Assert.AreEqual("First", logic.Working[0].name);
        }

        [Test]
        public void Qol_Restoring_KeepsTheSameListInstanceForTheSubwindow()
        {
            FlowchartEditorQolLogic logic = StartQol();
            List<FlowchartEditorQol> shownByTheUi = logic.Working;
            logic.Create("Staged");

            logic.Undo();

            Assert.AreSame(shownByTheUi, logic.Working);
        }

        [Test]
        public void Qol_FindRealFor_IsNullForAssetsThatOnlyExistInTheWorkingState()
        {
            FlowchartEditorQolLogic logic = StartQol();
            logic.Create("OnlyHere");

            Assert.IsNull(logic.FindRealFor(0));
        }
        #endregion
    }
}
