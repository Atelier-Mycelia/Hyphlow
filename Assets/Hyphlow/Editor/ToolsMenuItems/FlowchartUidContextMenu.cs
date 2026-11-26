using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Adds "Reset Unique Id" to the context menu of a Flowchart's Inspector, for fixing
    /// Flowcharts that ended up sharing an id.
    /// </summary>
    public static class FlowchartUidContextMenu
    {
        private const string MenuPath = "CONTEXT/Flowchart/Reset Unique Id";

        [MenuItem(MenuPath)]
        private static void ResetUid(MenuCommand command)
        {
            Flowchart flowchart = command.context as Flowchart;
            if (flowchart == null || !ConfirmReset(flowchart))
            {
                return;
            }

            Undo.RecordObject(flowchart, "Reset Flowchart Unique Id");
            flowchart.ForceResetUid();
            EditorUtility.SetDirty(flowchart);
            MarkOwningSceneDirty(flowchart);
            FlowchartRegistry.Recapture();

            string logMessage = $"Gave Flowchart {flowchart.name} a new UniqueId: {flowchart.UniqueId}";
            Debug.Log(logMessage, flowchart);
        }

        private static bool ConfirmReset(Flowchart flowchart)
        {
            string message = $"Give {flowchart.name} a new Unique Id?\n\nAnything that looks " +
                "this Flowchart up by its current id won't find it anymore.";
            return EditorUtility.DisplayDialog("Reset Unique Id", message, "Reset", "Cancel");
        }

        [MenuItem(MenuPath, true)]
        private static bool CanResetUid(MenuCommand command)
        {
            bool isFlowchart = command.context is Flowchart;
            return isFlowchart && !EditorApplication.isPlaying;
        }

        

        private static void MarkOwningSceneDirty(Flowchart flowchart)
        {
            GameObject go = flowchart.gameObject;
            Scene scene = go.scene;
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
