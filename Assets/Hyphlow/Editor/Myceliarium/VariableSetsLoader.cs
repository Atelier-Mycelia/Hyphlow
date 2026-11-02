namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Reads the Variable Sets in the project into fresh records for the logic to adopt.
    /// </summary>
    public sealed class VariableSetsLoader : ImmediateEntryLoader
    {
        protected override object Read()
        {
            return VariableSetAssetOps.FindAll();
        }
    }
}
