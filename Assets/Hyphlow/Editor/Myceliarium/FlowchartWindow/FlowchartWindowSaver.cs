using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System.Collections;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public sealed class FlowchartWindowSaver : ControlPanelEntrySaver, IAtMyceliaControlPanelEntrySaver
    {
        public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
        {
            return toSaveFor is FlowchartWindowEntry;
        }

        protected override IEnumerator SaveProcess(EntrySaveRequest request)
        {
            var entry = (FlowchartWindowEntry)request.Entry;
            FlowchartWindowConfig real = HyphlowEditorSysAssets.FcwConfig;
            if (real == null || entry.WorkingState == null)
            {
                request.ReportFailure();
                yield break;
            }

            string path = AssetDatabase.GetAssetPath(real);
            if (HyphlowRepoGuard.IsInReadOnlyPackage(path))
            {
                // Unity would refuse the write anyway, only with a less helpful message
                EditorUtility.DisplayDialog("Can't Save Flowchart Window Settings",
                    $"The Flowchart Window config lives in a read-only package ({path}), so " +
                    "it can't be changed from here. Your edits are still in the panel.",
                    "OK");
                request.ReportFailure();
                yield break;
            }

            EditorUtility.CopySerialized(entry.WorkingState, real);
            EditorUtility.SetDirty(real); // So Unity writes it to disk
            AssetDatabase.SaveAssetIfDirty(real);
        }
    }
}
