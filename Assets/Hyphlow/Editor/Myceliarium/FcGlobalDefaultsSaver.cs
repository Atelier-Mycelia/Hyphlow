using AtMycelia.Hyphlow.EditorExt;
using AtMycelia.Myceliarium;
using System;
using System.Collections;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    public class FcGlobalDefaultsSaver : ControlPanelEntrySaver, IAtMyceliaControlPanelEntrySaver
    {
        public override bool IsCompatibleWith(IControlPanelEntry toSaveFor)
        {
            return toSaveFor is FcGlobalDefaultsEntry;
        }

        protected override IEnumerator SaveProcess(IControlPanelEntry toSaveFor, Action onComplete)
        {
            yield return null;
            FcGlobalDefaultsEntry entry = toSaveFor as FcGlobalDefaultsEntry;
            var workingState = entry.WorkingState;
            var realObj = FlowchartGlobalDefaults.S;
            workingState.ApplyStateTo(realObj);
            yield break;
        }
    }
}