using AtMycelia.Myceliarium;
using System.Collections;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public sealed class EditorPrefSaver : ControlPanelEntrySaver, IAtMyceliaControlPanelEntrySaver
    {
        public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
        {
            return toSaveFor is EditorPrefEntry;
        }

        protected override IEnumerator SaveProcess(EntrySaveRequest request)
        {
            var entry = (EditorPrefEntry)request.Entry;
            if (entry.WorkingState == null)
            {
                request.ReportFailure();
                yield break;
            }

            entry.WorkingState.WriteToPreferences();
        }
    }
}
