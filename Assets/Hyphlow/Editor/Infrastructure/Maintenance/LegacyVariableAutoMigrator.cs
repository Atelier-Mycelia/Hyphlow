using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace AtMycelia.Hyphlow.EditorExt
{
    [InitializeOnLoad]
    public static class LegacyVariableAutoMigrator
    {
        private static bool _enabled = false;

        static LegacyVariableAutoMigrator()
        {
            Init();
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            QueueMigration();
        }

        private static void OnAfterAssemblyReload()
        {
            QueueMigration();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            QueueMigration();
        }

        private static void QueueMigration()
        {
            EditorApplication.delayCall -= MigrateActiveSceneFlowcharts;
            EditorApplication.delayCall += MigrateActiveSceneFlowcharts;
        }

        private static void MigrateActiveSceneFlowcharts()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !_enabled)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            bool changedAny = false;
            var roots = activeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var flowcharts = roots[i].GetComponentsInChildren<Flowchart>(true);
                for (int j = 0; j < flowcharts.Length; j++)
                {
                    changedAny |= LegacyVariableConverter.MigrateFlowchart(flowcharts[j],
                        recordUndo: true) > 0;
                }
            }

            if (changedAny)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }
    }
}
