using AtMycelia.Hyphlow.EditorExt;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Hands the logic the real global defaults asset, for it to copy from.
    /// </summary>
    public sealed class FcGlobalDefaultsLoader : ImmediateEntryLoader
    {
        protected override object Read()
        {
            return FlowchartGlobalDefaults.S;
        }
    }
}
