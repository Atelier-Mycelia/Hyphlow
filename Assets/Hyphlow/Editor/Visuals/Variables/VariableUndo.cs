using UnityEditor;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Where edits to variable sources go on Unity's undo stack. Detached Variable Sets
    /// are the exception: the Control Panel stages edits on them and keeps its own history,
    /// so if Unity's undo also recorded them, Edit > Undo would change a set behind the
    /// panel's back.
    /// </summary>
    public static class VariableUndo
    {
        public static bool IsRecordedFor(IVariableSource source)
        {
            return !(source is VariableSet set && set.IsDetached);
        }

        public static void RecordObject(UnityObj target, IVariableSource source, string label)
        {
            if (IsRecordedFor(source))
            {
                Undo.RecordObject(target, label);
            }
        }
    }
}
