using UnityEngine;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Owns the working copy of the editor preferences, so EditorPrefs stays untouched
    /// until the user saves.
    /// </summary>
    internal sealed class EditorPrefLogic : JsonSnapshotLogic
    {
        public EditorPrefData Working { get; private set; }

        // The loader has already refreshed the preferences from EditorPrefs; there's no
        // other result to take from it
        protected override void CopyFrom(object loadResult)
        {
            if (Working == null)
            {
                Working = ScriptableObject.CreateInstance<EditorPrefData>();
            }

            Working.ReadFromPreferences();
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
