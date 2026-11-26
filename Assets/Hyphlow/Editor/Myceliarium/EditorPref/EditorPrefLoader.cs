using AtMycelia.Hyphlow.EditorExt;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Brings <see cref="HyphlowEditorPreferences"/> up to date with EditorPrefs, for the
    /// logic to copy from. The preferences are static fields, so there's no result to hand
    /// over.
    /// </summary>
    public sealed class EditorPrefLoader : ImmediateEntryLoader
    {
        protected override object Read()
        {
            HyphlowEditorPreferences.LoadFromUnityEditorPrefs();
            return null;
        }
    }
}
