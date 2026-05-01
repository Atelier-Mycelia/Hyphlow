using UnityEditor;

namespace AtMycelia.Hyphlow.EditorUtils
{
    public class CameraMenuItems 
    {
        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Create/View", false, 100)]
        static void CreateView()
        {
            FlowchartMenuItems.SpawnPrefab("View");
        }
    }
}