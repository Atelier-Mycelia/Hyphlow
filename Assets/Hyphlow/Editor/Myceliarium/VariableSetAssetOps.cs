using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// What the Variable Sets control panel entry needs to know about the project's
    /// Variable Sets, and which names they're allowed to have. Changing the project is
    /// VariableSetCommit's job.
    /// </summary>
    internal static class VariableSetAssetOps
    {
        /// <summary>
        /// Where new Variable Sets go, and where ones from elsewhere get moved to.
        /// Sets in its subfolders are considered organized already, and are left alone.
        /// Settable so tests can use a throwaway folder.
        /// </summary>
        internal static string VsaFolder { get; set; } = "Assets/Resources/Hyphlow/VSA";

        internal static string VsaFolderDisplay => VsaFolder.StartsWith("Assets/", StringComparison.Ordinal) ?
            VsaFolder.Substring("Assets/".Length) :
            VsaFolder;

        internal const string DefaultName = "NewVariableSet";

        internal static List<VariableSetRecord> FindAll()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(VariableSet)}");
            var result = new List<VariableSetRecord>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<VariableSet>(path);
                if (asset != null)
                {
                    result.Add(VariableSetRecord.FromAsset(asset, guids[i], path));
                }
            }

            return result;
        }

        internal static VariableSetLocation Classify(string assetPath)
        {
            // Packages are immutable (or at least not ours to reorganize)
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return VariableSetLocation.ReadOnly;
            }

            string folder = FolderOf(assetPath);
            if (string.Equals(folder, VsaFolder, StringComparison.OrdinalIgnoreCase))
            {
                return VariableSetLocation.VsaFolder;
            }

            if (folder.StartsWith(VsaFolder + "/", StringComparison.OrdinalIgnoreCase))
            {
                return VariableSetLocation.VsaSubfolder;
            }

            return VariableSetLocation.Elsewhere;
        }

        internal static string FolderOf(string assetPath)
        {
            return Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? string.Empty;
        }

        internal static string PathIn(string folder, string name)
        {
            return $"{folder}/{name}.asset";
        }

        /// <summary>
        /// Returns a user-facing reason the name can't be used, or null if it can.
        /// <br></br><br></br>
        /// Names are kept unique across all the sets as they'll be once saved: ones staged for
        /// deletion don't count (since deleting happens first), and renamed ones count under
        /// their new names. They have to be unique since the var-select popups list each
        /// set's variables under the set's name.
        /// </summary>
        /// <param name="targetFolder">Where the set will be, if the name also needs to
        /// not clash with other assets there. Null skips that check.</param>
        internal static string ValidateName(string name, IReadOnlyList<VariableSetRecord> records,
            VariableSetRecord ignore = null, string targetFolder = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Enter a name.";
            }

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return "Names can't contain any of these: \\ / : * ? \" < > |";
            }

            VariableSetRecord sameName = FindByName(name, records, ignore);
            if (sameName != null)
            {
                string where = sameName.IsNew ?
                    "a set you've added but haven't saved yet" :
                    sameName.Path;
                return $"\"{sameName.Name}\" already exists ({where}). Names need to be " +
                    "unique so their variables can be told apart in variable pickers.";
            }

            if (targetFolder != null && IsOccupiedByOtherAsset(PathIn(targetFolder, name), records))
            {
                return $"Another asset is already named \"{name}\" in {targetFolder}.";
            }

            return null;
        }

        private static VariableSetRecord FindByName(string name, IReadOnlyList<VariableSetRecord> records,
            VariableSetRecord ignore)
        {
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                bool sameName = record != ignore && !record.PendingDelete &&
                    string.Equals(record.Name, name, StringComparison.OrdinalIgnoreCase);
                if (sameName)
                {
                    return record;
                }
            }

            return null;
        }

        /// <summary>
        /// Files the records account for don't count: Variable Sets' names are already
        /// checked against each other, and a set that's being deleted or renamed away
        /// frees its file. What's left is assets that aren't Variable Sets.
        /// </summary>
        private static bool IsOccupiedByOtherAsset(string assetPath, IReadOnlyList<VariableSetRecord> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (string.Equals(records[i].Path, assetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null || File.Exists(assetPath);
        }

        /// <summary>
        /// Follows Unity's own duplicate naming ("Name 1", "Name 2"...), skipping any name
        /// that's taken.
        /// </summary>
        internal static string SuggestName(string baseName, IReadOnlyList<VariableSetRecord> records,
            string folder)
        {
            const int giveUpAfter = 1000; // For a baseName that's invalid, and so never free
            string candidate = baseName;
            for (int suffix = 1; ValidateName(candidate, records, null, folder) != null; suffix++)
            {
                if (suffix > giveUpAfter)
                {
                    return DefaultName;
                }

                candidate = $"{baseName} {suffix}";
            }

            return candidate;
        }
    }
}
