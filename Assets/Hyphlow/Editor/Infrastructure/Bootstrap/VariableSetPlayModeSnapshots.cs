using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Puts Variable Sets back the way they were before Play Mode. Being assets, whatever
    /// Play Mode does to them would otherwise stick. Replaces the PlayerPrefs backups each
    /// set used to make for itself.
    /// <br></br><br></br>
    /// The snapshot lives in a file under Library rather than in memory, since entering
    /// Play Mode normally reloads the domain, which would wipe it. That also means a
    /// session that crashed mid-Play Mode can still be recovered.
    /// </summary>
    [InitializeOnLoad]
    internal static class VariableSetPlayModeSnapshots
    {
        internal static readonly string SnapshotPath =
            "Library/Hyphlow/VariableSetPlayModeSnapshot.json";

        static VariableSetPlayModeSnapshots()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += OfferRecoveryIfNeeded;
        }

        /// <summary>
        /// Raised right before the sets are backed up for Play Mode. Anything that wants to
        /// change them first (the Control Panel saving staged edits) has to do it here, and
        /// not from its own playModeStateChanged handler, since the order those run in
        /// isn't something anyone controls. A backup taken before the save would put the
        /// sets back to how they were before it.
        /// </summary>
        internal static event Action BeforeCapture = delegate { };

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // Before anything in the scene gets the chance to change them
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                BeforeCapture();
                WriteSnapshotFile(Capture(FindAllSets()));
            }
            // Not ExitingPlayMode: the scene's OnDisables and OnDestroys run after that,
            // and they could change variables after we'd restored them.
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                RestoreFromFile();
            }
        }

        private static List<VariableSet> FindAllSets()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(VariableSet)}");
            var result = new List<VariableSet>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var set = AssetDatabase.LoadAssetAtPath<VariableSet>(path);
                if (set != null)
                {
                    result.Add(set);
                }
            }

            return result;
        }

        internal static Snapshot Capture(IReadOnlyList<VariableSet> sets)
        {
            var snapshot = new Snapshot
            {
                takenAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            };

            for (int i = 0; i < sets.Count; i++)
            {
                VariableSet set = sets[i];
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(set));
                if (string.IsNullOrEmpty(guid))
                {
                    continue; // Not an asset, so not ours to look after
                }

                snapshot.entries.Add(new Snapshot.Entry
                {
                    guid = guid,
                    json = StateOf(set),
                    wasDirty = EditorUtility.IsDirty(set),
                });
            }

            return snapshot;
        }

        /// <summary>
        /// Taken from a detached copy, so the JSON carries the detached flag along: restoring
        /// goes through a copy overwritten with this, and that copy needs to stay detached.
        /// </summary>
        private static string StateOf(VariableSet set)
        {
            VariableSet copy = set.CreateDetachedCopy();
            try
            {
                return EditorJsonUtility.ToJson(copy);
            }
            finally
            {
                UnityObj.DestroyImmediate(copy);
            }
        }

        private static void WriteSnapshotFile(Snapshot snapshot)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath));
                File.WriteAllText(SnapshotPath, JsonUtility.ToJson(snapshot));
            }
            catch (Exception e)
            {
                Debug.LogError("[Hyphlow] Couldn't back up Variable Sets before Play Mode, so " +
                    $"changes Play Mode makes to them won't be undone afterwards.\n{e}");
            }
        }

        private static void RestoreFromFile()
        {
            if (TryReadSnapshotFile(out Snapshot snapshot))
            {
                RestoreAndCleanUp(snapshot);
            }
        }

        private static bool TryReadSnapshotFile(out Snapshot snapshot)
        {
            snapshot = null;
            if (!File.Exists(SnapshotPath))
            {
                return false;
            }

            try
            {
                snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(SnapshotPath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Hyphlow] Couldn't read the Variable Set backups at {SnapshotPath}, " +
                    $"so they weren't restored. The file has been left there.\n{e}");
            }

            return snapshot != null;
        }

        /// <summary>
        /// The file is kept if any set failed to restore, so the next session can offer
        /// to try again. The sets that did restore will be skipped then, being unchanged.
        /// </summary>
        private static void RestoreAndCleanUp(Snapshot snapshot)
        {
            RestoreResult result = Restore(snapshot);
            if (result.failed == 0)
            {
                File.Delete(SnapshotPath);
            }
        }

        /// <summary>
        /// Sets that match their snapshot are left alone, so exiting Play Mode doesn't
        /// dirty (or raise signals for) every set in the project. Sets that weren't dirty
        /// before Play Mode get saved, putting their files back exactly as they were in
        /// case something saved them mid-Play Mode. Ones that were dirty stay unsaved, as
        /// the user left them.
        /// </summary>
        internal static RestoreResult Restore(Snapshot snapshot)
        {
            var result = new RestoreResult();
            for (int i = 0; i < snapshot.entries.Count; i++)
            {
                Snapshot.Entry entry = snapshot.entries[i];
                string path = AssetDatabase.GUIDToAssetPath(entry.guid);
                var set = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<VariableSet>(path);
                if (set == null)
                {
                    continue; // Deleted while playing; nothing to put back
                }

                try
                {
                    if (StateOf(set) == entry.json)
                    {
                        continue;
                    }

                    ApplySnapshotTo(set, entry.json);
                    if (!entry.wasDirty)
                    {
                        AssetDatabase.SaveAssetIfDirty(set);
                    }

                    result.restored++;
                }
                catch (Exception e)
                {
                    result.failed++;
                    Debug.LogError($"[Hyphlow] Couldn't restore Variable Set {path} to how it was " +
                        $"before Play Mode.\n{e}", set);
                }
            }

            return result;
        }

        private static void ApplySnapshotTo(VariableSet set, string json)
        {
            VariableSet fromSnapshot = set.CreateDetachedCopy();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(json, fromSnapshot);
                set.ApplyStateFrom(fromSnapshot);
            }
            finally
            {
                UnityObj.DestroyImmediate(fromSnapshot);
            }
        }

        /// <summary>
        /// A snapshot file still being around outside of Play Mode means the last Play Mode
        /// session never got to restore from it, which almost always means a crash.
        /// </summary>
        private static void OfferRecoveryIfNeeded()
        {
            // The domain reload on entering Play Mode lands here too, with the file in use
            bool snapshotMayBeInUse = EditorApplication.isPlayingOrWillChangePlaymode;
            if (snapshotMayBeInUse || !TryReadSnapshotFile(out Snapshot snapshot))
            {
                return;
            }

            if (Application.isBatchMode)
            {
                Debug.LogWarning($"[Hyphlow] Found Variable Set backups from a Play Mode session " +
                    $"that didn't end normally ({SnapshotPath}). Left them alone, since there's " +
                    "nobody to ask in batch mode.");
                return;
            }

            string message = "Hyphlow backed up your Variable Sets when Play Mode started " +
                $"({TakenAtDisplay(snapshot)}), but that session didn't end normally, so they " +
                "were never put back. Any changes Play Mode made to them may still be there.\n\n" +
                "Restore puts them back the way they were before Play Mode.\n" +
                "Keep As Is leaves them how they are now, and discards the backups.";
            bool restore = EditorUtility.DisplayDialog("Restore Variable Sets?", message,
                "Restore", "Keep As Is");

            if (restore)
            {
                RestoreAndCleanUp(snapshot);
            }
            else
            {
                File.Delete(SnapshotPath);
            }
        }

        private static string TakenAtDisplay(Snapshot snapshot)
        {
            bool parsed = DateTime.TryParse(snapshot.takenAtUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out DateTime takenAt);
            return parsed ? takenAt.ToLocalTime().ToString("g") : "at an unknown time";
        }

        [Serializable]
        internal sealed class Snapshot
        {
            public string takenAtUtc;
            public List<Entry> entries = new List<Entry>();

            [Serializable]
            internal sealed class Entry
            {
                // Asset GUID rather than UniqueId: it's what survives the set being moved
                // or renamed in Play Mode, and duplicated UniqueIds are a known possibility.
                public string guid;
                public string json;
                public bool wasDirty;
            }
        }

        internal struct RestoreResult
        {
            public int restored;
            public int failed;
        }
    }
}
