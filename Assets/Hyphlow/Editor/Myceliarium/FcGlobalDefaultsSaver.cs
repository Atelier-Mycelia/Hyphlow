using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System.Collections;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public sealed class FcGlobalDefaultsSaver : ControlPanelEntrySaver, IAtMyceliaControlPanelEntrySaver
    {
        public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
        {
            return toSaveFor is FcGlobalDefaultsEntry;
        }

        protected override IEnumerator SaveProcess(EntrySaveRequest request)
        {
            var entry = (FcGlobalDefaultsEntry)request.Entry;
            FlowchartGlobalDefaults real = FlowchartGlobalDefaults.S;
            entry.WorkingState.ApplyStateTo(real);

            if (EditorUtility.IsPersistent(real))
            {
                // ApplyStateTo only sets fields, which Unity wouldn't write to disk on its own
                EditorUtility.SetDirty(real);
                AssetDatabase.SaveAssetIfDirty(real);
            }

            yield break;
        }
    }
}
