using System.Collections.Generic;
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
                    changedAny |= MigrateFlowchart(flowcharts[j]);
                }
            }

            if (changedAny)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }

        private static bool MigrateFlowchart(Flowchart flowchart)
        {
            if (flowchart == null)
            {
                return false;
            }

            var legacyVars = new List<Variable>();
            CollectLegacyVars(flowchart, legacyVars);

            if (legacyVars.Count == 0)
            {
                return false;
            }

            Undo.RecordObject(flowchart, "Migrate legacy variables to Muscariables");

            bool changed = false;
            for (int i = 0; i < legacyVars.Count; i++)
            {
                Variable legacy = legacyVars[i];
                if (legacy == null)
                {
                    continue;
                }

                // If this legacy var is still registered, remove it first so ID/key can be reused.
                flowchart.RemoveVariable(legacy);

                Muscariable migrated = legacy.ToMuscariable();
                migrated.Key = legacy.Key;
                migrated.ItemId = legacy.ItemId;
                migrated.Scope = legacy.Scope;
                migrated.BoxedValue = legacy.BoxedValue;

                flowchart.AddVariable(migrated);

                Undo.DestroyObjectImmediate(legacy);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(flowchart);

                VariableManagerComponent manager = flowchart.GetComponent<VariableManagerComponent>();
                if (manager != null)
                {
                    EditorUtility.SetDirty(manager);
                }
            }

            return changed;
        }

        private static void CollectLegacyVars(Flowchart flowchart, List<Variable> results)
        {
            var onGameObject = flowchart.GetComponents<Variable>();
            for (int i = 0; i < onGameObject.Length; i++)
            {
                AddIfMissing(results, onGameObject[i]);
            }

            IList<Variable> legacyVarsFound = flowchart.GetComponentsInChildren<Variable>(true);

            for (int i = 0; i < legacyVarsFound.Count; i++)
            {
                var found = legacyVarsFound[i];
                AddIfMissing(results, found);
            }
            
        }

        private static void AddIfMissing(List<Variable> list, Variable toAdd)
        {
            if (toAdd == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], toAdd))
                {
                    return;
                }
            }

            list.Add(toAdd);
        }
    }
}