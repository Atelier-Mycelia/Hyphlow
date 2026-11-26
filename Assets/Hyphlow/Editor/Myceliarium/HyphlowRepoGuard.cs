using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// For entries that edit assets meant only for Hyphlow's developers: projects that
    /// merely use the package shouldn't change them.
    /// </summary>
    internal static class HyphlowRepoGuard
    {
        /// <summary>
        /// An Assets/Hyphlow folder is what tells a Hyphlow repo or fork apart from a
        /// project that has the package as a dependency.
        /// </summary>
        public static bool IsHyphlowRepo()
        {
            return AssetDatabase.IsValidFolder("Assets/Hyphlow");
        }

        /// <summary>
        /// True for assets in a package that Unity won't let anyone write to, such as
        /// git or registry ones.
        /// </summary>
        public static bool IsInReadOnlyPackage(string assetPath)
        {
            if (!assetPath.StartsWith("Packages/"))
            {
                return false;
            }

            PackageInfo package = PackageInfo.FindForAssetPath(assetPath);
            bool editable = package != null &&
                (package.source == PackageSource.Embedded || package.source == PackageSource.Local);
            return !editable;
        }
    }
}
