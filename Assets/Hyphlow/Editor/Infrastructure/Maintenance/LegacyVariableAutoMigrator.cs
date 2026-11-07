using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace AtMycelia.Hyphlow.EditorExt
{
    public static class LegacyVariableAutoMigrator
    {
        private static bool _enabled = false;

        /// <summary>
        /// Called by its module in HyphlowMaintenanceModules.cs via the Common lib's MaintenanceRunner; don't call it from lifecycle hooks.
        /// </summary>
        public static void Run()
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
