using AtMycelia.Hyphlow.EditorExt;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Hands the logic the real editor system assets, for it to copy from. Going through
    /// <see cref="HyphlowEditorSysAssets.S"/> means a missing asset gets created.
    /// </summary>
    public sealed class EditorSysAssetsLoader : ImmediateEntryLoader
    {
        protected override object Read()
        {
            return HyphlowEditorSysAssets.S;
        }
    }
}
