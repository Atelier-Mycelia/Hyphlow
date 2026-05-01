using UnityEngine;
using UnityEditor;
using UnityObj = UnityEngine.Object;

namespace AtMycelia.Hyphlow.EditorUtils
{
    /// <summary>
    /// Menu items for creating Flowchart-related objects, such as
    /// Flowcharts and the Fungus Logo. These menu items are added
    /// to the "Tools/Atelier Mycelia/Hyphlow/Create" menu in the Unity Editor.
    /// </summary>
    public class FlowchartMenuItems
    {
        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Create/Flowchart", false, 0)]
        static void CreateFlowchart()
        {
            GameObject go = SpawnPrefab("Flowchart");
            go.transform.position = Vector3.zero;

            // This is the latest version of Flowchart, so no need to update.
            var flowchart = go.GetComponent<Flowchart>();
            if (flowchart != null)
            {
                flowchart.Version = HyphlowConstants.CurrentVersion;
            }
        }

        [MenuItem("Tools/Atelier Mycelia/Hyphlow/Create/Fungus Logo", false, 1000)]
        static void CreateFungusLogo()
        {
            SpawnPrefab("Fungus Logo");
        }

        public static GameObject SpawnPrefab(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>("Runtime/Prefabs/" + prefabName);
            if (prefab == null)
            {
                return null;
            }

            GameObject go = UnityObj.Instantiate(prefab);
            go.name = prefab.name;

            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                Camera sceneCam = view.camera;
                Vector3 pos = sceneCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
                pos.z = 0f;
                go.transform.position = pos;
            }

            Selection.activeGameObject = go;
            
            Undo.RegisterCreatedObjectUndo(go, $"Create {prefab.name}");

            return go;
        }
    }
}