using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Carries out what's been staged on the Variable Set records: deletes, renames, moves,
    /// edits, then creates, in that order, so that names and files freed by the earlier
    /// steps are available to the later ones. Each record is brought up to date as its
    /// steps succeed, so when something fails, the rest still go through and the failed
    /// part stays staged for another try.
    /// </summary>
    internal static class VariableSetCommit
    {
        internal sealed class Result
        {
            public readonly List<string> Errors = new List<string>();
            public bool Succeeded => Errors.Count == 0;

            public string Summary
            {
                get
                {
                    var text = new StringBuilder();
                    for (int i = 0; i < Errors.Count; i++)
                    {
                        text.AppendLine($"• {Errors[i]}");
                    }

                    return text.ToString();
                }
            }
        }

        /// <summary>
        /// Records that are gone afterwards (deleted ones) get removed from the list and
        /// disposed. Whatever held their detached copies should let go of them first.
        /// </summary>
        internal static Result Run(IList<VariableSetRecord> records)
        {
            var result = new Result();
            var snapshot = new List<VariableSetRecord>(records);

            Guard(result, "Delete", () => DeleteStaged(snapshot, records, result));
            Guard(result, "Rename", () => RenameStaged(snapshot, result));
            Guard(result, "Move", () => MoveStaged(snapshot, result));
            Guard(result, "Save edits to", () => SaveEdits(snapshot, result));
            Guard(result, "Create", () => CreateStaged(snapshot, result));

            AssetDatabase.SaveAssets();
            return result;
        }

        // A step blowing up shouldn't stop the later ones, or take the panel down with it
        private static void Guard(Result result, string step, Action doStep)
        {
            try
            {
                doStep();
            }
            catch (Exception e)
            {
                result.Errors.Add($"{step} step failed unexpectedly: {e.Message}");
                Debug.LogException(e);
            }
        }

        #region Deleting
        private static void DeleteStaged(List<VariableSetRecord> snapshot,
            IList<VariableSetRecord> records, Result result)
        {
            for (int i = 0; i < snapshot.Count; i++)
            {
                VariableSetRecord record = snapshot[i];
                if (!record.PendingDelete)
                {
                    continue;
                }

                bool gone = record.IsNew || TryDelete(record, result);
                if (gone)
                {
                    records.Remove(record);
                    record.Dispose();
                }
            }
        }

        /// <summary>
        /// Via the trash rather than outright, so a mistaken delete can still be recovered.
        /// Swappable so tests don't fill the user's trash.
        /// </summary>
        internal static Func<string, bool> DeleteAsset { get; set; } = AssetDatabase.MoveAssetToTrash;

        private static bool TryDelete(VariableSetRecord record, Result result)
        {
            if (record.IsReadOnly)
            {
                result.Errors.Add(ReadOnlyError(record));
                return false;
            }

            if (!DeleteAsset(record.Path))
            {
                result.Errors.Add($"Unity couldn't move {record.Path} to the trash.");
                return false;
            }

            return true;
        }

        private static string ReadOnlyError(VariableSetRecord record)
        {
            return $"\"{record.OriginalName}\" is inside a package, so it can't be changed from here.";
        }
        #endregion

        #region Renaming
        private static void RenameStaged(List<VariableSetRecord> snapshot, Result result)
        {
            var remaining = snapshot.FindAll(r => !r.PendingDelete && r.IsRenamed && !r.IsReadOnly);

            // One set's new name can be another's old one, so a rename that fails might work
            // once the others have gone through. Rounds go on until one gets nowhere.
            var lastErrors = new Dictionary<VariableSetRecord, string>();
            bool madeProgress = true;
            while (remaining.Count > 0 && madeProgress)
            {
                madeProgress = false;
                lastErrors.Clear();
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    VariableSetRecord record = remaining[i];
                    string error = AssetDatabase.RenameAsset(record.Path, record.Name);
                    if (string.IsNullOrEmpty(error))
                    {
                        record.OnRenamed();
                        remaining.RemoveAt(i);
                        madeProgress = true;
                    }
                    else
                    {
                        lastErrors[record] = error;
                    }
                }
            }

            for (int i = 0; i < remaining.Count; i++)
            {
                result.Errors.Add($"Couldn't rename \"{remaining[i].OriginalName}\" to " +
                    $"\"{remaining[i].Name}\": {lastErrors[remaining[i]]}");
            }
        }
        #endregion

        #region Moving
        private static void MoveStaged(List<VariableSetRecord> snapshot, Result result)
        {
            for (int i = 0; i < snapshot.Count; i++)
            {
                VariableSetRecord record = snapshot[i];
                bool shouldMove = record.PendingMove && !record.PendingDelete && !record.IsNew &&
                    record.Location == VariableSetLocation.Elsewhere;
                if (!shouldMove)
                {
                    continue;
                }

                // Moving (rather than recreating) keeps the asset's GUID, so references to it survive.
                // Not batched in Start/StopAssetEditing: two same-named sets would both get
                // the same unique path, since the first move wouldn't register until the end.
                AssetUtils.EnsureFolderExists(VariableSetAssetOps.VsaFolder);
                string destination = AssetDatabase.GenerateUniqueAssetPath(
                    VariableSetAssetOps.PathIn(VariableSetAssetOps.VsaFolder,
                        Path.GetFileNameWithoutExtension(record.Path)));
                string error = AssetDatabase.MoveAsset(record.Path, destination);

                if (string.IsNullOrEmpty(error))
                {
                    record.OnMoved();
                }
                else
                {
                    result.Errors.Add($"Couldn't move \"{record.Name}\" to " +
                        $"{VariableSetAssetOps.VsaFolder}: {error}");
                }
            }
        }
        #endregion

        #region Edits
        private static void SaveEdits(List<VariableSetRecord> snapshot, Result result)
        {
            for (int i = 0; i < snapshot.Count; i++)
            {
                VariableSetRecord record = snapshot[i];
                if (record.IsNew || record.PendingDelete || !record.HasWorking)
                {
                    continue;
                }

                // A second look, in case it was edited since the last time anyone checked
                record.RefreshStateChanged();
                if (!record.StateChanged)
                {
                    continue;
                }

                if (record.IsReadOnly)
                {
                    result.Errors.Add(ReadOnlyError(record));
                    continue;
                }

                try
                {
                    record.Real.ApplyStateFrom(record.Working);
                    AssetDatabase.SaveAssetIfDirty(record.Real);
                    record.OnStateCommitted();
                }
                catch (Exception e)
                {
                    result.Errors.Add($"Couldn't save the changes to \"{record.Name}\": {e.Message}");
                    Debug.LogException(e);
                }
            }
        }
        #endregion

        #region Creating
        private static void CreateStaged(List<VariableSetRecord> snapshot, Result result)
        {
            for (int i = 0; i < snapshot.Count; i++)
            {
                VariableSetRecord record = snapshot[i];
                if (!record.IsNew || record.PendingDelete)
                {
                    continue;
                }

                try
                {
                    CreateAssetFor(record);
                }
                catch (Exception e)
                {
                    result.Errors.Add($"Couldn't create \"{record.Name}\": {e.Message}");
                    Debug.LogException(e);
                }
            }
        }

        private static void CreateAssetFor(VariableSetRecord record)
        {
            string path = VariableSetAssetOps.PathIn(record.TargetFolder, record.Name);
            if (File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                throw new InvalidOperationException($"Something is already at {path}.");
            }

            AssetUtils.EnsureFolderExists(record.TargetFolder);

            var created = ScriptableObject.CreateInstance<VariableSet>();
            AssetDatabase.CreateAsset(created, path);

            // Copies over the variables and settings, but not the UniqueId, so a
            // duplicate gets an id of its own. The id itself comes from the Refresh in there,
            // which can only assign one now that the set is an asset.
            created.ApplyStateFrom(record.Working);
            AssetDatabase.SaveAssetIfDirty(created);

            record.OnCreated(created);
        }
        #endregion
    }
}
