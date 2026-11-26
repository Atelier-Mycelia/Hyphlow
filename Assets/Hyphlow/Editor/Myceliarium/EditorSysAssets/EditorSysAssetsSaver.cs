using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System.Collections;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public sealed class EditorSysAssetsSaver : ControlPanelEntrySaver, IAtMyceliaControlPanelEntrySaver
    {
        public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
        {
            return toSaveFor is EditorSysAssetsEntry;
        }

        protected override IEnumerator SaveProcess(EntrySaveRequest request)
        {
            var entry = (EditorSysAssetsEntry)request.Entry;
            HyphlowEditorSysAssets real = HyphlowEditorSysAssets.S;
            if (real == null || entry.WorkingState == null)
            {
                request.ReportFailure();
                yield break;
            }

            string path = AssetDatabase.GetAssetPath(real);
            if (HyphlowRepoGuard.IsInReadOnlyPackage(path))
            {
                // Unity would refuse the write anyway, only with a less helpful message
                EditorUtility.DisplayDialog("Can't Save Editor Icons",
                    $"The editor system assets live in a read-only package ({path}), so " +
                    "they can't be changed from here. Your edits are still in the panel.",
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
