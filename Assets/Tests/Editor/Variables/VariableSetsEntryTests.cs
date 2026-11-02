using AtMycelia;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.MyceliariumInt;
using AtMycelia.Myceliarium;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// The Variable Sets control panel entry as a whole: staging through its handlers,
    /// what counts as unsaved, committing, and keeping in step with the project. The
    /// pieces it's made of (records, naming, committing) have suites of their own, so
    /// these stay on how the entry puts them together. Works in throwaway folders instead
    /// of the real VSA one, and swaps the entry's modal dialogs for recorders.
    /// </summary>
    public class VariableSetsEntryTests
    {
        private const string Folder = "Assets/TestVsaEntry";
        private const string OtherFolder = "Assets/TestVsaEntryOther";
        private const string GreetingKey = "greeting";
        private const string GreetingValue = "hello";

        private string _originalVsaFolder;
        private Func<string, bool> _originalDeleteAction;
        private VariableSetsEntry _entry;
        private List<string> _errorTitles;
        private List<string> _errorMessages;

        [SetUp]
        public void SetUp()
        {
            VariableTypeDiscovery.DiscoverAndRegister();
            _originalVsaFolder = VariableSetAssetOps.VsaFolder;
            _originalDeleteAction = VariableSetCommit.DeleteAsset;
            VariableSetAssetOps.VsaFolder = Folder;
            VariableSetCommit.DeleteAsset = AssetDatabase.DeleteAsset;

            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.DeleteAsset(OtherFolder);
            _errorTitles = new List<string>();
            _errorMessages = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            _entry?.Dispose();
            _entry = null;

            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.DeleteAsset(OtherFolder);
            VariableSetAssetOps.VsaFolder = _originalVsaFolder;
            VariableSetCommit.DeleteAsset = _originalDeleteAction;
        }

        private static VariableSet MakeAsset(string name, string folder = Folder)
        {
            AssetUtils.EnsureFolderExists(folder);
            string path = VariableSetAssetOps.PathIn(folder, name);

            var set = ScriptableObject.CreateInstance<VariableSet>();
            AssetDatabase.CreateAsset(set, path);
            set.Refresh();
            set.AddNewVariableOfContentType<string>(GreetingKey, GreetingValue);
            AssetDatabase.SaveAssetIfDirty(set);
            return set;
        }

        private VariableSetsEntry StartEntry()
        {
            _entry = new VariableSetsEntry
            {
                ErrorDialog = (title, message) =>
                {
                    _errorTitles.Add(title);
                    _errorMessages.Add(message);
                },
                UnsavedChangesBeforePlayDialog = (title, message) =>
                    throw new InvalidOperationException("The Play Mode prompt wasn't expected here.")
            };
            _entry.Init();
            return _entry;
        }

        private VariableSetRecord RecordNamed(string name)
        {
            return _entry.Records.FirstOrDefault(record => record.Name == name);
        }

        private static Muscariable<string> Greeting(VariableSet set)
        {
            return (Muscariable<string>)set.GetVariableByName(GreetingKey);
        }

        private static string PathOf(string name, string folder = Folder)
        {
            return VariableSetAssetOps.PathIn(folder, name);
        }

        private static bool ExistsOnDisk(string path)
        {
            return AssetDatabase.LoadAssetAtPath<VariableSet>(path) != null;
        }

        #region Loading
        [Test]
        public void Init_LoadsTheSetsOnDisk_AndStartsClean()
        {
            MakeAsset("Alpha");
            MakeAsset("Elsewhere", OtherFolder);

            StartEntry();

            Assert.AreEqual(VariableSetLocation.VsaFolder, RecordNamed("Alpha").Location);
            Assert.AreEqual(VariableSetLocation.Elsewhere, RecordNamed("Elsewhere").Location);
            Assert.IsFalse(_entry.HasUnsavedChanges);
            Assert.AreEqual(0, _entry.PendingDeleteCount);
        }

        [Test]
        public void ReinitializingTheEntry_ThrowsAwayWhatWasStaged()
        {
            MakeAsset("Alpha");
            StartEntry();
            _entry.OnCreateRequested("Staged");
            _entry.OnDeleteRequested(RecordNamed("Alpha"));
            Assert.IsTrue(_entry.HasUnsavedChanges);

            _entry.Init(forceReinit: true); // What the footer's Cancel and Reinit buttons do

            Assert.IsFalse(_entry.HasUnsavedChanges);
            Assert.IsNull(RecordNamed("Staged"));
            Assert.IsFalse(RecordNamed("Alpha").PendingDelete);
        }

        [Test]
        public void ControlPanelOpening_PicksUpChangesMadeWhileItWasClosed()
        {
            StartEntry();
            MakeAsset("Late");
            Assert.IsNull(RecordNamed("Late"), "The entry shouldn't have heard about it yet");

            ControlPanelSignals.ControlPanelOpened(new StubControlPanel());

            Assert.IsNotNull(RecordNamed("Late"));
        }

        /// <summary>
        /// The signal goes to every entry that's initialized, not just ours, so what's
        /// passed along should be something they can all cope with.
        /// </summary>
        private sealed class StubControlPanel : IControlPanel
        {
            public UnityEngine.UIElements.VisualElement Root { get; } =
                new UnityEngine.UIElements.VisualElement();
            public IReadOnlyList<IControlPanelEntry> TopLevelEntries { get; } =
                new List<IControlPanelEntry>();
            public bool Contains(IControlPanelEntry entry) => false;
        }

        [Test]
        public void ControlPanelOpening_KeepsWhatWasStaged()
        {
            StartEntry();
            _entry.OnCreateRequested("Staged");
            MakeAsset("Late");

            ControlPanelSignals.ControlPanelOpened(new StubControlPanel());

            Assert.IsNotNull(RecordNamed("Staged"));
            Assert.IsNull(RecordNamed("Late"), "Reloading would've cost the staged set");
        }

        [Test]
        public void SelectingTheTab_PicksUpOutsideChanges_UnlessThingsAreStaged()
        {
            StartEntry();
            MakeAsset("Late");

            _entry.Select();
            Assert.IsNotNull(RecordNamed("Late"));

            _entry.OnCreateRequested("Staged");
            MakeAsset("Later");
            _entry.Deselect();
            _entry.Select();

            Assert.IsNotNull(RecordNamed("Staged"));
            Assert.IsNull(RecordNamed("Later"));
        }
        #endregion

        #region Creating
        [Test]
        public void Create_StagesANewSet_WithoutTouchingTheProject()
        {
            StartEntry();

            _entry.OnCreateRequested("Fresh");

            VariableSetRecord record = RecordNamed("Fresh");
            Assert.IsTrue(record.IsNew);
            Assert.AreEqual(Folder, record.TargetFolder);
            Assert.IsTrue(_entry.HasUnsavedChanges);
            Assert.IsFalse(ExistsOnDisk(PathOf("Fresh")));
        }

        [Test]
        public void Create_WithATakenName_ShowsAnErrorAndStagesNothing()
        {
            MakeAsset("Taken");
            StartEntry();
            int countBefore = _entry.Records.Count;

            _entry.OnCreateRequested("taken");

            Assert.AreEqual(1, _errorTitles.Count);
            StringAssert.Contains("Create", _errorTitles[0]);
            Assert.AreEqual(countBefore, _entry.Records.Count);
            Assert.IsFalse(_entry.HasUnsavedChanges);
        }

        [TestCase("")]
        [TestCase("a/b")]
        public void Create_WithAnInvalidName_ShowsAnErrorAndStagesNothing(string name)
        {
            StartEntry();
            int countBefore = _entry.Records.Count;

            _entry.OnCreateRequested(name);

            Assert.AreEqual(1, _errorTitles.Count);
            Assert.AreEqual(countBefore, _entry.Records.Count);
        }
        #endregion

        #region Renaming and duplicating
        [Test]
        public void Rename_StagesTheNewName_LeavingTheAssetAlone()
        {
            VariableSet asset = MakeAsset("OldName");
            StartEntry();

            _entry.OnRenameRequested(RecordNamed("OldName"), "NewName");

            Assert.IsNotNull(RecordNamed("NewName"));
            Assert.IsTrue(_entry.HasUnsavedChanges);
            Assert.AreEqual("OldName", asset.name);
        }

        [Test]
        public void Rename_ToATakenName_ShowsAnErrorAndKeepsTheName()
        {
            MakeAsset("First");
            MakeAsset("Second");
            StartEntry();

            _entry.OnRenameRequested(RecordNamed("First"), "Second");

            Assert.AreEqual(1, _errorTitles.Count);
            Assert.IsNotNull(RecordNamed("First"));
            Assert.IsFalse(_entry.HasUnsavedChanges);
        }

        [Test]
        public void Duplicate_StagesACopyNamedAfterTheOriginal_StartingFromItsStagedEdits()
        {
            MakeAsset("Original");
            StartEntry();
            VariableSetRecord original = RecordNamed("Original");
            Greeting(original.EnsureWorking()).Value = "edited but unsaved";
            original.RefreshStateChanged();

            _entry.OnDuplicateRequested(original);

            VariableSetRecord copy = RecordNamed("Original 1");
            Assert.IsTrue(copy.IsNew);
            Assert.AreEqual(Folder, copy.TargetFolder);
            Assert.AreEqual("edited but unsaved", Greeting(copy.Working).Value);
            Assert.IsFalse(ExistsOnDisk(PathOf("Original 1")));
        }

        [Test]
        public void Duplicate_OfASetOutsideTheVsaFolder_GoesInTheVsaFolder()
        {
            MakeAsset("Elsewhere", OtherFolder);
            StartEntry();

            _entry.OnDuplicateRequested(RecordNamed("Elsewhere"));

            Assert.AreEqual(Folder, RecordNamed("Elsewhere 1").TargetFolder);
        }
        #endregion

        #region Deleting
        [Test]
        public void Delete_OfASetOnDisk_IsStaged_NotDone()
        {
            MakeAsset("Doomed");
            StartEntry();

            _entry.OnDeleteRequested(RecordNamed("Doomed"));

            Assert.IsTrue(RecordNamed("Doomed").PendingDelete);
            Assert.AreEqual(1, _entry.PendingDeleteCount);
            Assert.IsTrue(_entry.HasUnsavedChanges);
            Assert.IsTrue(ExistsOnDisk(PathOf("Doomed")));
        }

        [Test]
        public void Delete_OfAStagedNewSet_RemovesItOutright()
        {
            StartEntry();
            _entry.OnCreateRequested("Fresh");

            _entry.OnDeleteRequested(RecordNamed("Fresh"));

            Assert.IsNull(RecordNamed("Fresh"));
            Assert.AreEqual(0, _entry.PendingDeleteCount);
            Assert.IsFalse(_entry.HasUnsavedChanges);
        }

        [Test]
        public void Keep_UndoesAStagedDelete()
        {
            MakeAsset("Doomed");
            StartEntry();
            _entry.OnDeleteRequested(RecordNamed("Doomed"));

            _entry.OnKeepRequested(RecordNamed("Doomed"));

            Assert.IsFalse(RecordNamed("Doomed").PendingDelete);
            Assert.IsFalse(_entry.HasUnsavedChanges);
        }

        [Test]
        public void Keep_WhenAnotherSetTookTheNameMeanwhile_ShowsAnErrorAndStaysDeleted()
        {
            MakeAsset("Same");
            StartEntry();
            VariableSetRecord old = RecordNamed("Same");
            _entry.OnDeleteRequested(old);
            _entry.OnCreateRequested("Same"); // Allowed, since the old one's going away

            _entry.OnKeepRequested(old);

            Assert.AreEqual(1, _errorTitles.Count);
            Assert.IsTrue(old.PendingDelete);
        }
        #endregion

        #region Moving
        [Test]
        public void MoveToggled_OnlyAppliesToSetsOutsideTheVsaFolder_AndTogglesBack()
        {
            MakeAsset("Inside");
            MakeAsset("Outside", OtherFolder);
            StartEntry();

            _entry.OnMoveToggled(RecordNamed("Inside"));
            Assert.IsFalse(RecordNamed("Inside").PendingMove);
            Assert.IsFalse(_entry.HasUnsavedChanges);

            _entry.OnMoveToggled(RecordNamed("Outside"));
            Assert.IsTrue(RecordNamed("Outside").PendingMove);
            Assert.IsTrue(_entry.HasUnsavedChanges);

            _entry.OnMoveToggled(RecordNamed("Outside"));
            Assert.IsFalse(RecordNamed("Outside").PendingMove);
            Assert.IsFalse(_entry.HasUnsavedChanges);
        }

        [Test]
        public void MoveAll_StagesOnlyTheSetsThatCanBeMoved()
        {
            MakeAsset("OutsideA", OtherFolder);
            MakeAsset("OutsideB", OtherFolder);
            MakeAsset("Inside");
            StartEntry();
            _entry.OnDeleteRequested(RecordNamed("OutsideB"));
            _entry.OnCreateRequested("Fresh");

            _entry.OnMoveAllRequested();

            Assert.IsTrue(RecordNamed("OutsideA").PendingMove);
            Assert.IsFalse(RecordNamed("OutsideB").PendingMove, "It's being deleted");
            Assert.IsFalse(RecordNamed("Inside").PendingMove);
            Assert.IsFalse(RecordNamed("Fresh").PendingMove);
        }
        #endregion

        #region Committing
        [Test]
        public void Commit_CarriesOutEverythingStaged_AndLeavesNothingUnsaved()
        {
            MakeAsset("OldName");
            MakeAsset("Doomed");
            MakeAsset("Drifter", OtherFolder);
            StartEntry();
            _entry.OnCreateRequested("Created");
            _entry.OnRenameRequested(RecordNamed("OldName"), "NewName");
            _entry.OnDeleteRequested(RecordNamed("Doomed"));
            _entry.OnMoveToggled(RecordNamed("Drifter"));

            bool succeeded = _entry.Commit();

            Assert.IsTrue(succeeded, string.Join("\n", _errorMessages));
            Assert.IsTrue(ExistsOnDisk(PathOf("Created")));
            Assert.IsTrue(ExistsOnDisk(PathOf("NewName")));
            Assert.IsFalse(ExistsOnDisk(PathOf("OldName")));
            Assert.IsFalse(ExistsOnDisk(PathOf("Doomed")));
            Assert.IsTrue(ExistsOnDisk(PathOf("Drifter")));
            Assert.IsFalse(_entry.HasUnsavedChanges);
            Assert.IsNull(RecordNamed("Doomed"));
        }

        [Test]
        public void Commit_WhenSomethingFails_ReportsIt_AndKeepsItStaged()
        {
            AssetUtils.EnsureFolderExists(Folder);
            AssetDatabase.CreateAsset(new AnimationClip(), PathOf("Blocked"));
            MakeAsset("Failing");
            MakeAsset("Fine");
            StartEntry();
            RecordNamed("Failing").Name = "Blocked"; // Past the entry's own name checks
            VariableSetRecord fine = RecordNamed("Fine");
            Greeting(fine.EnsureWorking()).Value = "goodbye";
            fine.RefreshStateChanged();

            bool succeeded = _entry.Commit();

            Assert.IsFalse(succeeded);
            Assert.AreEqual(1, _errorTitles.Count);
            StringAssert.Contains("Failing", _errorMessages[0]);
            Assert.IsTrue(_entry.HasUnsavedChanges, "The failed rename should still be staged");
            Assert.AreEqual("goodbye", Greeting(fine.Real).Value, "The rest should've gone through");
        }

        [Test]
        public void Commit_OfEditsToASet_UpdatesTheAssetKeepingItsVariableInstances()
        {
            VariableSet asset = MakeAsset("Edits");
            StartEntry();
            IVariable instanceBefore = asset.GetVariableByName(GreetingKey);
            VariableSetRecord record = RecordNamed("Edits");
            Greeting(record.EnsureWorking()).Value = "goodbye";
            record.RefreshStateChanged();

            _entry.Commit();

            Assert.AreSame(instanceBefore, asset.GetVariableByName(GreetingKey));
            Assert.AreEqual("goodbye", Greeting(asset).Value);
        }
        #endregion

        #region Telling the panel about unsaved changes
        [Test]
        public void UnsavedChangesChanged_IsRaisedOnlyWhenTheAnswerFlips()
        {
            StartEntry();
            int raised = 0;
            Action<IControlPanelEntry> onChanged = entry => raised += entry == _entry ? 1 : 0;
            ControlPanelSignals.UnsavedChangesChanged += onChanged;
            try
            {
                _entry.OnCreateRequested("One"); // Clean -> dirty
                _entry.OnCreateRequested("Two"); // Still dirty
                Assert.AreEqual(1, raised);

                _entry.Commit(); // Dirty -> clean
                Assert.AreEqual(2, raised);
            }
            finally
            {
                ControlPanelSignals.UnsavedChangesChanged -= onChanged;
            }
        }
        #endregion

        #region Restoring after Play Mode
        [Test]
        public void StateApplied_ToASetWithACleanWorkingCopy_MakesTheCopyCatchUp()
        {
            VariableSet asset = MakeAsset("Restored");
            StartEntry();
            VariableSetRecord record = RecordNamed("Restored");
            record.EnsureWorking(); // Made from the asset's values at the time
            Greeting(asset).Value = "what Play Mode left";
            Assert.AreEqual(GreetingValue, Greeting(record.Working).Value, "Sanity check");

            VarSetSignals.StateApplied(asset); // As raised when the snapshot module restores a set

            Assert.AreEqual("what Play Mode left", Greeting(record.EnsureWorking()).Value);
            Assert.IsFalse(record.RefreshStateChanged());
            Assert.IsFalse(_entry.HasUnsavedChanges);
        }

        [Test]
        public void StateApplied_ToASetWithStagedEdits_LeavesTheEditsAlone()
        {
            VariableSet asset = MakeAsset("Edited");
            StartEntry();
            VariableSetRecord record = RecordNamed("Edited");
            Greeting(record.EnsureWorking()).Value = "staged";
            record.RefreshStateChanged();
            Greeting(asset).Value = "changed underneath";

            VarSetSignals.StateApplied(asset);

            Assert.AreEqual("staged", Greeting(record.Working).Value);
            Assert.IsTrue(record.StateChanged);
            Assert.IsTrue(_entry.HasUnsavedChanges);
        }

        [Test]
        public void StateApplied_ToASetTheEntryDoesntKnow_IsIgnored()
        {
            StartEntry();
            var stranger = ScriptableObject.CreateInstance<VariableSet>();
            try
            {
                Assert.DoesNotThrow(() => VarSetSignals.StateApplied(stranger));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stranger);
            }
        }
        #endregion

        #region Entering Play Mode
        [Test]
        public void BeforePlayMode_WithNothingStaged_DoesntAsk()
        {
            MakeAsset("Alpha");
            StartEntry(); // Its Play Mode dialog throws if it's called

            Assert.DoesNotThrow(() => _entry.OnBeforePlayModeSnapshot());
        }

        [Test]
        public void BeforePlayMode_ChoosingSave_CommitsWhatWasStaged()
        {
            StartEntry();
            _entry.UnsavedChangesBeforePlayDialog = (title, message) => 0;
            _entry.OnCreateRequested("Fresh");

            _entry.OnBeforePlayModeSnapshot();

            Assert.IsTrue(ExistsOnDisk(PathOf("Fresh")));
            Assert.IsFalse(_entry.HasUnsavedChanges);
        }

        [TestCase(1, TestName = "BeforePlayMode_ChoosingCancel_SavesNothing")]
        [TestCase(2, TestName = "BeforePlayMode_ChoosingDiscard_SavesNothing")]
        public void BeforePlayMode_NotChoosingSave_SavesNothing(int choice)
        {
            StartEntry();
            _entry.UnsavedChangesBeforePlayDialog = (title, message) => choice;
            _entry.OnCreateRequested("Fresh");

            _entry.OnBeforePlayModeSnapshot();

            Assert.IsFalse(ExistsOnDisk(PathOf("Fresh")));
            Assert.IsTrue(_entry.HasUnsavedChanges);
        }

        [Test]
        public void BeforePlayMode_MentionsHowManySetsWillBeDeleted()
        {
            MakeAsset("Doomed");
            StartEntry();
            string shownMessage = null;
            _entry.UnsavedChangesBeforePlayDialog = (title, message) =>
            {
                shownMessage = message;
                return 2;
            };
            _entry.OnDeleteRequested(RecordNamed("Doomed"));

            _entry.OnBeforePlayModeSnapshot();

            StringAssert.Contains("1 Variable Set", shownMessage);
        }
        #endregion

        #region Saver
        [Test]
        public void Saver_IsCompatibleOnlyWithTheVariableSetsEntry()
        {
            var saver = new VariableSetsSaver();
            StartEntry();

            Assert.IsTrue(saver.IsCompatibleWith(_entry));
            Assert.IsFalse(saver.IsCompatibleWith(new FlowchartEditorQolEntry()));
        }
        #endregion

        #region Disposing
        [Test]
        public void Dispose_ReleasesTheStagedCopies_AndStopsReportingChanges()
        {
            StartEntry();
            _entry.OnCreateRequested("Staged");
            VariableSet working = RecordNamed("Staged").Working;
            Assert.IsTrue(working != null, "Sanity check");

            _entry.Dispose();

            Assert.IsTrue(working == null, "The staged copy should be destroyed");
            Assert.IsFalse(_entry.HasUnsavedChanges);
        }
        #endregion
    }
}
