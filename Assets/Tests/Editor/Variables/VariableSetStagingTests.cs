using AtMycelia;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.MyceliariumInt;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VScriptingTests.VariableOperations
{
    /// <summary>
    /// The staging behind the Control Panel's Variable Sets entry: what the records track,
    /// which names they allow, and what committing them does to the project. Works in a
    /// throwaway folder instead of the real VSA one.
    /// </summary>
    public class VariableSetStagingTests
    {
        private const string Folder = "Assets/TestVsaStaging";
        private const string OtherFolder = "Assets/TestVsaStagingOther";
        private const string GreetingKey = "greeting";
        private const string GreetingValue = "hello";

        private string _originalVsaFolder;
        private Func<string, bool> _originalDeleteAction;
        private List<VariableSetRecord> _records;

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
            _records = new List<VariableSetRecord>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _records.Count; i++)
            {
                _records[i].Dispose();
            }

            AssetDatabase.DeleteAsset(Folder);
            AssetDatabase.DeleteAsset(OtherFolder);
            VariableSetAssetOps.VsaFolder = _originalVsaFolder;
            VariableSetCommit.DeleteAsset = _originalDeleteAction;
        }

        private VariableSetRecord MakeExisting(string name, string folder = Folder)
        {
            AssetUtils.EnsureFolderExists(folder);
            string path = VariableSetAssetOps.PathIn(folder, name);

            var set = ScriptableObject.CreateInstance<VariableSet>();
            AssetDatabase.CreateAsset(set, path);
            set.Refresh();
            set.AddNewVariableOfContentType<string>(GreetingKey, GreetingValue);
            AssetDatabase.SaveAssetIfDirty(set);

            var record = VariableSetRecord.FromAsset(set, AssetDatabase.AssetPathToGUID(path), path);
            _records.Add(record);
            return record;
        }

        private VariableSetRecord Stage(VariableSetRecord record)
        {
            _records.Add(record);
            return record;
        }

        private static Muscariable<string> Greeting(VariableSet set)
        {
            return (Muscariable<string>)set.GetVariableByName(GreetingKey);
        }

        #region Tracking changes
        [Test]
        public void ExistingRecord_StartsOutClean()
        {
            VariableSetRecord record = MakeExisting("Clean");
            record.EnsureWorking();

            Assert.IsFalse(record.RefreshStateChanged());
            Assert.IsFalse(record.IsDirty);
        }

        [Test]
        public void EditingTheWorkingCopy_MakesItDirty_WithoutTouchingTheAsset()
        {
            VariableSetRecord record = MakeExisting("Edited");
            Greeting(record.EnsureWorking()).Value = "goodbye";

            bool changed = record.RefreshStateChanged();

            Assert.IsTrue(changed);
            Assert.IsTrue(record.IsDirty);
            Assert.AreEqual(GreetingValue, Greeting(record.Real).Value);
        }

        [Test]
        public void EditingBackToTheOriginal_MakesItCleanAgain()
        {
            VariableSetRecord record = MakeExisting("Reverted");
            Greeting(record.EnsureWorking()).Value = "goodbye";
            record.RefreshStateChanged();

            Greeting(record.Working).Value = GreetingValue;
            bool changed = record.RefreshStateChanged();

            Assert.IsTrue(changed, "The answer should've flipped back");
            Assert.IsFalse(record.IsDirty);
        }

        [Test]
        public void StagedRenameDeleteMoveAndNew_AllCountAsDirty()
        {
            VariableSetRecord renamed = MakeExisting("Renamed");
            renamed.Name = "SomethingElse";
            VariableSetRecord deleted = MakeExisting("Deleted");
            deleted.PendingDelete = true;
            VariableSetRecord moved = MakeExisting("Moved", OtherFolder);
            moved.PendingMove = true;
            VariableSetRecord created = Stage(VariableSetRecord.CreateNew("Created", Folder));

            Assert.IsTrue(renamed.IsDirty);
            Assert.IsTrue(deleted.IsDirty);
            Assert.IsTrue(moved.IsDirty);
            Assert.IsTrue(created.IsDirty);
        }

        [Test]
        public void StagedMove_OfASetAlreadyInTheVsaFolder_IsNotDirty()
        {
            VariableSetRecord record = MakeExisting("Home");
            record.PendingMove = true;

            Assert.IsFalse(record.IsDirty);
        }
        #endregion

        #region Names
        [Test]
        public void ValidateName_RejectsNamesOfStagedNewSets()
        {
            Stage(VariableSetRecord.CreateNew("Alpha", Folder));

            Assert.IsNotNull(VariableSetAssetOps.ValidateName("alpha", _records, null, Folder));
        }

        [Test]
        public void ValidateName_AllowsTheNameOfASetStagedForDeletion()
        {
            VariableSetRecord doomed = MakeExisting("Beta");
            doomed.PendingDelete = true;

            Assert.IsNull(VariableSetAssetOps.ValidateName("Beta", _records, null, Folder));
        }

        [Test]
        public void ValidateName_AllowsTheOldNameOfARenamedSet_AndRejectsItsNewOne()
        {
            VariableSetRecord record = MakeExisting("Gamma");
            record.Name = "Gamma2";

            Assert.IsNull(VariableSetAssetOps.ValidateName("Gamma", _records, null, Folder));
            Assert.IsNotNull(VariableSetAssetOps.ValidateName("Gamma2", _records, null, Folder));
        }

        [Test]
        public void ValidateName_RejectsANameTakenByAnAssetThatIsNotAVariableSet()
        {
            AssetUtils.EnsureFolderExists(Folder);
            AssetDatabase.CreateAsset(new AnimationClip(), VariableSetAssetOps.PathIn(Folder, "Taken"));

            Assert.IsNotNull(VariableSetAssetOps.ValidateName("Taken", _records, null, Folder));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("a/b")]
        [TestCase("what?")]
        public void ValidateName_RejectsEmptyAndInvalidNames(string name)
        {
            Assert.IsNotNull(VariableSetAssetOps.ValidateName(name, _records, null, Folder));
        }

        [Test]
        public void SuggestName_SkipsNamesOfStagedSets()
        {
            Stage(VariableSetRecord.CreateNew(VariableSetAssetOps.DefaultName, Folder));
            Stage(VariableSetRecord.CreateNew(VariableSetAssetOps.DefaultName + " 1", Folder));

            string suggestion = VariableSetAssetOps.SuggestName(VariableSetAssetOps.DefaultName,
                _records, Folder);

            Assert.AreEqual(VariableSetAssetOps.DefaultName + " 2", suggestion);
        }
        #endregion

        #region Committing
        [Test]
        public void Commit_NewRecord_CreatesTheAssetWithItsStagedVariables()
        {
            VariableSetRecord record = Stage(VariableSetRecord.CreateNew("Fresh", Folder));
            record.Working.AddNewVariableOfContentType<int>("count", 5);

            var result = VariableSetCommit.Run(_records);

            Assert.IsTrue(result.Succeeded, result.Summary);
            Assert.IsFalse(record.IsNew);
            Assert.IsFalse(record.IsDirty);
            Assert.AreEqual(VariableSetAssetOps.PathIn(Folder, "Fresh"), record.Path);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<VariableSet>(record.Path));
            Assert.IsFalse(string.IsNullOrEmpty(record.Real.UniqueId));
            Assert.AreEqual(5, ((Muscariable<int>)record.Real.GetVariableByName("count")).Value);
            Assert.IsFalse(record.Real.IsDetached);
        }

        [Test]
        public void Commit_EditedRecord_UpdatesTheAssetKeepingItsVariableInstances()
        {
            VariableSetRecord record = MakeExisting("Edits");
            IVariable instanceBefore = record.Real.GetVariableByName(GreetingKey);
            Greeting(record.EnsureWorking()).Value = "goodbye";
            record.RefreshStateChanged();

            var result = VariableSetCommit.Run(_records);

            Assert.IsTrue(result.Succeeded, result.Summary);
            Assert.AreSame(instanceBefore, record.Real.GetVariableByName(GreetingKey));
            Assert.AreEqual("goodbye", Greeting(record.Real).Value);
            Assert.IsFalse(record.IsDirty);
            Assert.IsFalse(EditorUtility.IsDirty(record.Real), "Should've been saved to disk");
        }

        [Test]
        public void Commit_RenamedRecord_RenamesTheAssetFile()
        {
            VariableSetRecord record = MakeExisting("OldName");
            string oldPath = record.Path;
            record.Name = "NewName";

            var result = VariableSetCommit.Run(_records);

            Assert.IsTrue(result.Succeeded, result.Summary);
            Assert.AreEqual(VariableSetAssetOps.PathIn(Folder, "NewName"), record.Path);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<VariableSet>(oldPath));
            Assert.AreEqual("NewName", record.Real.name);
            Assert.IsFalse(record.IsDirty);
        }

        [Test]
        public void Commit_DeletedRecord_RemovesTheAssetAndTheRecord()
        {
            VariableSetRecord record = MakeExisting("Doomed");
            string path = record.Path;
            record.PendingDelete = true;

            var result = VariableSetCommit.Run(_records);

            Assert.IsTrue(result.Succeeded, result.Summary);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<VariableSet>(path));
            CollectionAssert.DoesNotContain(_records, record);
        }

        [Test]
        public void Commit_MovedRecord_GoesToTheVsaFolderKeepingItsGuid()
        {
            VariableSetRecord record = MakeExisting("Drifter", OtherFolder);
            string guidBefore = record.Key;
            record.PendingMove = true;

            var result = VariableSetCommit.Run(_records);

            Assert.IsTrue(result.Succeeded, result.Summary);
            Assert.AreEqual(VariableSetAssetOps.PathIn(Folder, "Drifter"), record.Path);
            Assert.AreEqual(VariableSetLocation.VsaFolder, record.Location);
            Assert.AreEqual(guidBefore, AssetDatabase.AssetPathToGUID(record.Path));
            Assert.IsFalse(record.PendingMove);
        }

        [Test]
        public void Commit_DuplicatedRecord_GetsItsOwnUniqueId_ButTheSameVariables()
        {
            VariableSetRecord source = MakeExisting("Original");
            VariableSetRecord copy = Stage(VariableSetRecord.CreateCopyOf(source, "Original 1", Folder));

            var result = VariableSetCommit.Run(_records);

            Assert.IsTrue(result.Succeeded, result.Summary);
            Assert.IsFalse(string.IsNullOrEmpty(copy.Real.UniqueId));
            Assert.AreNotEqual(source.Real.UniqueId, copy.Real.UniqueId);
            Assert.AreEqual(GreetingValue, Greeting(copy.Real).Value);
            Assert.AreEqual(source.Real.GetVariableByName(GreetingKey).ItemId,
                copy.Real.GetVariableByName(GreetingKey).ItemId);
        }

        [Test]
        public void Commit_DuplicateStartsFromTheStagedState_NotTheOnesOnDisk()
        {
            VariableSetRecord source = MakeExisting("Source");
            Greeting(source.EnsureWorking()).Value = "edited but unsaved";

            VariableSetRecord copy = Stage(VariableSetRecord.CreateCopyOf(source, "Source 1", Folder));
            VariableSetCommit.Run(_records);

            Assert.AreEqual("edited but unsaved", Greeting(copy.Real).Value);
        }

        [Test]
        public void Commit_DeleteAndCreateWithTheSameName_Works()
        {
            VariableSetRecord old = MakeExisting("Same");
            old.PendingDelete = true;
            string oldUniqueId = old.Real.UniqueId; // Read now, since the asset won't be there afterwards
            VariableSetRecord replacement = Stage(VariableSetRecord.CreateNew("Same", Folder));
            Assert.IsNull(VariableSetAssetOps.ValidateName("Same", _records, replacement, Folder),
                "Staging it should be allowed");

            var result = VariableSetCommit.Run(_records);

            Assert.IsTrue(result.Succeeded, result.Summary);
            Assert.IsNotNull(replacement.Real);

            // The asset's GUID isn't checked: Unity may hand a re-created asset the GUID of one
            // that was just deleted from the same path. What matters is that it's a new set.
            Assert.AreNotEqual(oldUniqueId, replacement.Real.UniqueId);
            Assert.IsNull(replacement.Real.GetVariableByName(GreetingKey),
                "Shouldn't have anything of the old set's");
        }

        [Test]
        public void Commit_OneFailing_DoesNotStopTheRest_AndStaysStaged()
        {
            AssetDatabase.DeleteAsset(Folder);
            AssetUtils.EnsureFolderExists(Folder);
            AssetDatabase.CreateAsset(new AnimationClip(), VariableSetAssetOps.PathIn(Folder, "Blocked"));

            VariableSetRecord failing = MakeExisting("Failing");
            failing.Name = "Blocked"; // Not allowed by validation, but staging can be bypassed
            VariableSetRecord fine = MakeExisting("Fine");
            Greeting(fine.EnsureWorking()).Value = "goodbye";
            fine.RefreshStateChanged();

            var result = VariableSetCommit.Run(_records);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(1, result.Errors.Count);
            StringAssert.Contains("Failing", result.Errors[0]);
            Assert.IsTrue(failing.IsDirty, "The failed rename should still be staged");
            Assert.IsFalse(fine.IsDirty);
            Assert.AreEqual("goodbye", Greeting(fine.Real).Value);
        }

        [Test]
        public void Commit_NothingStaged_ChangesNothing()
        {
            VariableSetRecord record = MakeExisting("Quiet");
            string path = record.Path;

            var result = VariableSetCommit.Run(_records);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(path, record.Path);
            Assert.AreEqual(1, _records.Count);
        }
        #endregion

        [Test]
        public void CreateNew_RaisesNoSignals_BeingDetached()
        {
            int signals = 0;
            Action<VariableSet> onEnabled = _ => signals++;
            Action<VariableSet> onDisabled = _ => signals++;
            VarSetSignals.VsaEnabled += onEnabled;
            VarSetSignals.VsaDisabled += onDisabled;
            try
            {
                VariableSetRecord record = VariableSetRecord.CreateNew("Quiet", Folder);
                Assert.IsTrue(record.Working.IsDetached);
                record.Dispose();
            }
            finally
            {
                VarSetSignals.VsaEnabled -= onEnabled;
                VarSetSignals.VsaDisabled -= onDisabled;
            }

            Assert.AreEqual(0, signals);
        }
    }
}
