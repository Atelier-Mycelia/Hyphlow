using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtMycelia.Hyphlow.EditorUtils
{
    public static class OldFungusStateRemover
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        [InitializeOnLoadMethod]
        public static void Init()
        {
            AssemblyReloadEvents.afterAssemblyReload -= RemoveOldFungusState;
            AssemblyReloadEvents.afterAssemblyReload += RemoveOldFungusState;

            EditorSceneManager.sceneOpened -= RemoveOldFungusStateOnSceneOpen;
            EditorSceneManager.sceneOpened += RemoveOldFungusStateOnSceneOpen;
        }

        private static void RemoveOldFungusStateOnSceneOpen(Scene scene, OpenSceneMode mode)
        {
            RemoveOldFungusState();
        }

        private static void RemoveOldFungusState()
        {
            int deletedCount = 0;
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                // Search recursively
                deletedCount += DeleteMatchesRecursive(root.transform, _target);
            }

            if (deletedCount > 0)
            {
                string logMessage = $"OldFungusStateRemover: Deleted {deletedCount} FungusState GameObject(s) in " +
                $"scene {CurrentSceneName}.";
                Debug.Log(logMessage);
            }
        }

        static readonly string _target = "_FungusState";

        private static int DeleteMatchesRecursive(Transform t, string targetName)
        {
            int count = 0;

            // Check children first (so we don't break the hierarchy while iterating)
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                count += DeleteMatchesRecursive(t.GetChild(i), targetName);
            }

            // Check this object
            if (t.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                string logMessage = $"DeleteByNameWindow: Deleting GameObject \"{t.name}\" at " +
                    $"path \"{GetFullPath(t)}\". in scene {CurrentSceneName}";
                Debug.Log(logMessage);
                Undo.DestroyObjectImmediate(t.gameObject);
                count++;
            }

            return count;
        }

        private static string CurrentSceneName => SceneManager.GetActiveScene().name;

        private static object GetFullPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}