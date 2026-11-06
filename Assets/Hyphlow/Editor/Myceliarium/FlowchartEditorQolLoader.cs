using AtMycelia.Hyphlow.EditorExt;
using System.Collections.Generic;
using UnityEditor;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Finds the Flowchart Editor QoL assets in the project, for the logic to copy from.
    /// </summary>
    public sealed class FlowchartEditorQolLoader : ImmediateEntryLoader
    {
        protected override object Read()
        {
            return FindAll();
        }

        internal static List<FlowchartEditorQol> FindAll()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FlowchartEditorQol)}");
            var found = new List<FlowchartEditorQol>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var real = AssetDatabase.LoadAssetAtPath<FlowchartEditorQol>(path);
                if (real != null)
                {
                    found.Add(real);
                }
            }

            return found;
        }
    }
}
