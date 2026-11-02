using AtMycelia.Hyphlow.EditorExt;
using UnityEngine;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Owns the working copy of the global Flowchart defaults, so the real asset stays
    /// untouched until the user saves.
    /// </summary>
    internal sealed class FcGlobalDefaultsLogic : JsonSnapshotLogic
    {
        public FlowchartGlobalDefaults Working { get; private set; }

        protected override void CopyFrom(object loadResult)
        {
            if (Working == null)
            {
                Working = ScriptableObject.CreateInstance<FlowchartGlobalDefaults>();
            }

            var real = loadResult as FlowchartGlobalDefaults;
            if (real == null)
            {
                Debug.LogError($"{nameof(FlowchartGlobalDefaults)} asset not found. Please " +
                    "ensure it exists in the Resources folder.");
                return;
            }

            real.ApplyStateTo(Working);
        }

        protected override string CaptureSnapshot()
        {
            return Working != null ? JsonUtility.ToJson(Working) : string.Empty;
        }

        protected override void RestoreSnapshot(string snapshot)
        {
            if (Working != null && !string.IsNullOrEmpty(snapshot))
            {
                JsonUtility.FromJsonOverwrite(snapshot, Working);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (Working != null)
            {
                Object.DestroyImmediate(Working);
            }

            Working = null;
        }
    }
}
