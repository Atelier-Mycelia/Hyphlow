using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Gives a Flowchart a fresh UniqueId when it's added to a scene (or prefab stage) that
    /// already has a Flowchart with the same one. Duplicating, copy-pasting or instantiating
    /// a Flowchart copies its id, and anything keyed by the id (like FlowchartRegistry) can
    /// then only see one of the two.
    ///
    /// Unlike the modules in HyphlowMaintenanceModules.cs, this responds to edits as they
    /// happen instead of running on the MaintenanceRunner's schedule, since that's the only
    /// time we can tell which Flowchart is the new one.
    /// </summary>
    [InitializeOnLoad]
    public static class FlowchartUidDeduplicator
    {
        static FlowchartUidDeduplicator()
        {
            ObjectChangeEvents.changesPublished -= OnChangesPublished;
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            bool resetAny = false;
            for (int i = 0; i < stream.length; i++)
            {
                ObjectChangeKind kind = stream.GetEventType(i);
                if (kind != ObjectChangeKind.CreateGameObjectHierarchy)
                {
                    continue;
                }

                stream.GetCreateGameObjectHierarchyEvent(i, out CreateGameObjectHierarchyEventArgs args);
                resetAny |= DeduplicateUidsUnder(args.instanceId);
            }

            if (resetAny)
            {
                FlowchartRegistry.Recapture();
            }
        }

        /// <returns>Whether any Flowchart had its id reset.</returns>
        private static bool DeduplicateUidsUnder(int gameObjectInstanceId)
        {
            GameObject root = EditorUtility.InstanceIDToObject(gameObjectInstanceId) as GameObject;
            if (root == null)
            {
                return false;
            }

            Flowchart[] addedFlowcharts = root.GetComponentsInChildren<Flowchart>(true);
            bool resetAny = false;
            for (int i = 0; i < addedFlowcharts.Length; i++)
            {
                Flowchart fc = addedFlowcharts[i];
                resetAny |= ResetUidIfTaken(fc);
            }

            return resetAny;
        }

        private static bool ResetUidIfTaken(Flowchart added)
        {
            GameObject addedGo = added.gameObject;
            Scene scene = addedGo.scene;
            if (!scene.IsValid() || !IsUidTakenInScene(added, scene))
            {
                return false;
            }

            string oldUid = added.UniqueId;
            Undo.RecordObject(added, "Reset Duplicate Flowchart Unique Id");
            added.ForceResetUid();
            EditorUtility.SetDirty(added);
            EditorSceneManager.MarkSceneDirty(scene);

            string logMessage = $"Flowchart {added.name} had the same UniqueId ({oldUid}) as " +
                $"another Flowchart in {scene.name}, so it got a new one: {added.UniqueId}";
            Debug.Log(logMessage, added);
            return true;
        }

        private static bool IsUidTakenInScene(Flowchart flowchart, Scene scene)
        {
            string uid = flowchart.UniqueId;
            if (string.IsNullOrEmpty(uid))
            {
                return false; // Flowchart.AssertUniqueID will give it one
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject elem = roots[i];
                Flowchart[] flowcharts = elem.GetComponentsInChildren<Flowchart>(true);
                if (AnyOtherHasUid(flowcharts, flowchart, uid))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyOtherHasUid(Flowchart[] flowcharts, Flowchart toSkip, string uid)
        {
            for (int i = 0; i < flowcharts.Length; i++)
            {
                Flowchart other = flowcharts[i];
                if (ReferenceEquals(other, toSkip))
                {
                    continue;
                }

                if (other.UniqueId == uid)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
