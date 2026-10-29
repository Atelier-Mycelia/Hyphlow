using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// The on-disk operations behind the Variable Sets control panel entry. Kept free of
    /// UI concerns so the entry and subwindow only decide <i>when</i> these happen.
    /// </summary>
    internal static class VariableSetAssetOps
    {
        /// <summary>
        /// Where new Variable Sets go, and where ones from elsewhere get moved to.
        /// Sets in its subfolders are considered organized already, and are left alone.
        /// </summary>
        internal const string VsaFolder = "Assets/Resources/Hyphlow/VSA";
        internal const string VsaFolderDisplay = "Resources/Hyphlow/VSA";
        internal const string DefaultName = "NewVariableSet";

        internal static List<VariableSetRecord> FindAll()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(VariableSet)}");
            var result = new List<VariableSetRecord>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<VariableSet>(path);
                if (asset == null)
                {
                    continue;
                }

                result.Add(new VariableSetRecord(asset, guids[i], path, Classify(path)));
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
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

        /// <summary>
        /// Returns a user-facing reason the name can't be used, or null if it can.
        /// Names are kept unique project-wide since the var-select popups list each
        /// set's variables under the set's name.
        /// </summary>
        internal static string ValidateName(string name, IReadOnlyList<VariableSetRecord> records,
            VariableSetRecord ignore = null)
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
                return $"\"{sameName.Name}\" already exists ({sameName.Path}). Names need to be " +
                    "unique so their variables can be told apart in variable pickers.";
            }

            return null;
        }

        private static VariableSetRecord FindByName(string name, IReadOnlyList<VariableSetRecord> records,
            VariableSetRecord ignore)
        {
            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                bool sameName = record != ignore &&
                    string.Equals(record.Name, name, StringComparison.OrdinalIgnoreCase);
                if (sameName)
                {
                    return record;
                }
            }

            return null;
        }

        internal static string ValidateNameForCreate(string name, IReadOnlyList<VariableSetRecord> records)
        {
            string error = ValidateName(name, records);
            if (error == null && FileExists(PathIn(VsaFolder, name)))
            {
                error = $"Another asset is already named \"{name}\" in {VsaFolderDisplay}.";
            }

            return error;
        }

        private static string PathIn(string folder, string name)
        {
            return $"{folder}/{name}.asset";
        }

        private static bool FileExists(string assetPath)
        {
            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null || File.Exists(assetPath);
        }

        /// <summary>
        /// Follows Unity's own duplicate naming ("Name 1", "Name 2"...), skipping any name
        /// that's taken by another set or by a file in the given folder.
        /// </summary>
        internal static string SuggestName(string baseName, IReadOnlyList<VariableSetRecord> records,
            string folder = VsaFolder)
        {
            string candidate = baseName;
            for (int suffix = 1; !IsFree(candidate); suffix++)
            {
                candidate = $"{baseName} {suffix}";
            }

            return candidate;

            bool IsFree(string name)
            {
                return FindByName(name, records, null) == null && !FileExists(PathIn(folder, name));
            }
        }

        internal static VariableSet Create(string name, IReadOnlyList<VariableSetRecord> records,
            out string error)
        {
            error = ValidateNameForCreate(name, records);
            if (error != null)
            {
                return null;
            }

            AssetUtils.EnsureFolderExists(VsaFolder);
            var created = ScriptableObject.CreateInstance<VariableSet>();
            AssetDatabase.CreateAsset(created, PathIn(VsaFolder, name));
            created.Refresh(); // It only gets a UniqueId once it's an asset
            created.MarkDirtyAndSave();
            return created;
        }

        internal static bool Rename(VariableSetRecord record, string newName,
            IReadOnlyList<VariableSetRecord> records, out string error)
        {
            error = record.IsReadOnly ? ReadOnlyError(record) : ValidateName(newName, records, record);
            if (error != null)
            {
                return false;
            }

            error = AssetDatabase.RenameAsset(record.Path, newName);
            return Succeeded(ref error);
        }

        private static string ReadOnlyError(VariableSetRecord record)
        {
            return $"\"{record.Name}\" is inside a package, so it can't be changed from here.";
        }

        // AssetDatabase reports success with an empty string rather than null
        private static bool Succeeded(ref string error)
        {
            if (string.IsNullOrEmpty(error))
            {
                error = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Copies sit next to the original when it's in the VSA folder (or a subfolder of
        /// it). Otherwise, they go in the VSA folder, same as any other new set.
        /// </summary>
        internal static VariableSet Duplicate(VariableSetRecord record,
            IReadOnlyList<VariableSetRecord> records, out string error)
        {
            error = null;
            bool keepNextToOriginal = record.Location == VariableSetLocation.VsaFolder ||
                record.Location == VariableSetLocation.VsaSubfolder;
            string folder = keepNextToOriginal ? FolderOf(record.Path) : VsaFolder;
            string name = SuggestName(record.Name, records, folder);
            string destination = PathIn(folder, name);

            AssetUtils.EnsureFolderExists(folder);
            if (!AssetDatabase.CopyAsset(record.Path, destination))
            {
                error = $"Unity couldn't copy {record.Path} to {destination}.";
                return null;
            }

            var copy = AssetDatabase.LoadAssetAtPath<VariableSet>(destination);

            // VariableSetUidPostprocessor should've already given the copy its own id on
            // import. This is in case it didn't, since sharing one breaks save data.
            bool sharesId = copy != null && copy.UniqueId == record.Asset.UniqueId;
            if (sharesId)
            {
                copy.ForceResetUid();
            }

            return copy;
        }

        /// <summary>
        /// Goes through the OS trash rather than deleting outright, so a mistaken
        /// delete can still be recovered.
        /// </summary>
        internal static bool Delete(VariableSetRecord record, out string error)
        {
            error = record.IsReadOnly ? ReadOnlyError(record) : null;
            if (error != null)
            {
                return false;
            }

            if (!AssetDatabase.MoveAssetToTrash(record.Path))
            {
                error = $"Unity couldn't move {record.Path} to the trash.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Moving (rather than recreating) keeps the asset's GUID, so references to the
        /// set survive. Call EnsureFolderExists on the VSA folder first when batching
        /// these inside Start/StopAssetEditing, since folders can't be created in there.
        /// </summary>
        internal static bool MoveToVsaFolder(VariableSetRecord record, out string error)
        {
            if (record.Location != VariableSetLocation.Elsewhere)
            {
                error = $"\"{record.Name}\" is already where it should be.";
                return false;
            }

            AssetUtils.EnsureFolderExists(VsaFolder);
            string destination = AssetDatabase.GenerateUniqueAssetPath(
                $"{VsaFolder}/{Path.GetFileName(record.Path)}");
            error = AssetDatabase.MoveAsset(record.Path, destination);
            return Succeeded(ref error);
        }
    }

    internal enum VariableSetLocation
    {
        VsaFolder,
        VsaSubfolder,
        /// <summary>
        /// Anywhere in Assets outside the VSA folder. These are the ones we offer to move.
        /// </summary>
        Elsewhere,
        ReadOnly,
    }

    internal sealed class VariableSetRecord
    {
        public VariableSetRecord(VariableSet asset, string guid, string path, VariableSetLocation location)
        {
            Asset = asset;
            Guid = guid;
            Path = path;
            Location = location;
        }

        public VariableSet Asset { get; }
        public string Guid { get; }
        public string Path { get; }
        public VariableSetLocation Location { get; }

        public string Name => Asset != null ? Asset.name : System.IO.Path.GetFileNameWithoutExtension(Path);
        public bool IsReadOnly => Location == VariableSetLocation.ReadOnly;
        public int VariableCount => Asset != null ? Asset.Variables.Count : 0;
    }
}
