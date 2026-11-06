namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Extension methods for VariableSourceAsset to be used in the editor.
    /// </summary>
    public static class VariableSetEditorExt
    {
        public static void RemoveVariableAt(this VariableSet source, int index)
        {
            if (source == null || index < 0 || index >= source.Variables.Count) return;
        }

    }
}