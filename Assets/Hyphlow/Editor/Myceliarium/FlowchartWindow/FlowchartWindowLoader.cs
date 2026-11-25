using AtMycelia.Hyphlow.EditorExt;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Hands the logic the real Flowchart Window config, for it to copy from. It's the
    /// one the editor system assets reference, since that's the one the window uses.
    /// </summary>
    public sealed class FlowchartWindowLoader : ImmediateEntryLoader
    {
        protected override object Read()
        {
            return HyphlowEditorSysAssets.FcwConfig;
        }
    }
}
