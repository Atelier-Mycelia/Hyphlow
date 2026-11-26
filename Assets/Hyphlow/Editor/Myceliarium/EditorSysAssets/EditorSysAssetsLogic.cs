using AtMycelia.Hyphlow.EditorExt;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Owns the working copy of the editor system assets, so the real asset stays
    /// untouched until the user saves.
    /// </summary>
    internal sealed class EditorSysAssetsLogic : JsonSnapshotLogic
    {
        public HyphlowEditorSysAssets Working { get; private set; }

        protected override void CopyFrom(object loadResult)
        {
            if (Working == null)
            {
                Working = ScriptableObject.CreateInstance<HyphlowEditorSysAssets>();
            }

            var real = loadResult as HyphlowEditorSysAssets;
            if (real == null)
            {
                Debug.LogError($"{nameof(HyphlowEditorSysAssets)} asset not found.");
                return;
            }

            EditorUtility.CopySerialized(real, Working);
        }

        // JsonUtility writes Unity object references as instance IDs, which stay valid
        // for as long as the editor session does; that's all undo and redo need.
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
