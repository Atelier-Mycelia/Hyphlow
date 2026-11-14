using AtMycelia.Hyphlow.EditorExt;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// Does the migrating for Hyphration. Each target gets its own log entry, and one
    /// failing doesn't stop the rest.
    /// </summary>
    internal static class HyphrationRunner
    {
        private const string ProgressTitle = "Hyphration";

        /// <summary>
        /// Null if the run didn't start: Play Mode, or the user cancelled the prompt to
        /// save their open scenes first.
        /// </summary>
        public static IReadOnlyList<HyphrationLogEntry> Run(IReadOnlyList<HyphrationTarget> targets)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Hyphration] Can't migrate during Play Mode.");
                return null;
            }

            bool anyScenes = false;
            for (int i = 0; i < targets.Count; i++)
            {
                anyScenes |= targets[i].Kind == HyphrationTargetKind.Scene;
            }

            // Saving a migrated scene would otherwise also save whatever else is unsaved in it
            if (anyScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return null;
            }

            var log = new List<HyphrationLogEntry>(targets.Count);
            SceneSetup[] originalSetup = anyScenes ?
                EditorSceneManager.GetSceneManagerSetup() :
                Array.Empty<SceneSetup>();

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    HyphrationTarget target = targets[i];
                    EditorUtility.DisplayProgressBar(ProgressTitle, target.DisplayName,
                        (float)i / targets.Count);

                    HyphrationLogEntry entry = MigrateGuarded(target);
                    log.Add(entry);
                    LogToConsole(entry);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (originalSetup.Length > 0)
                {
                    // Closes the scenes we opened just to migrate them
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
                AssetDatabase.SaveAssets();
            }

            return log;
        }

        private static HyphrationLogEntry MigrateGuarded(HyphrationTarget target)
        {
            var entry = new HyphrationLogEntry
            {
                Kind = target.Kind,
                DisplayName = target.DisplayName,
                Path = target.Path
            };

            try
            {
                if (target.Kind == HyphrationTargetKind.Scene)
                {
                    MigrateScene(target.Path, entry.Migrations);
                }
                else
                {
                    MigrateSet(target.Path, entry.Migrations);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                entry.Succeeded = false;
                entry.FailureMessage = e.Message;
            }

            return entry;
        }

        private static void MigrateScene(string path, List<LegacyVariableConverter.Migration> migrations)
        {
            Scene scene = FindLoadedScene(path);
            if (!scene.IsValid())
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Flowchart[] flowcharts = roots[i].GetComponentsInChildren<Flowchart>(true);
                for (int j = 0; j < flowcharts.Length; j++)
                {
                    LegacyVariableConverter.MigrateFlowchart(flowcharts[j], recordUndo: false,
                        migrations);
                }
            }

            if (migrations.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException($"Couldn't save scene '{path}'.");
                }
            }
        }

        private static Scene FindLoadedScene(string path)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene candidate = SceneManager.GetSceneAt(i);
                if (candidate.path == path)
                {
                    return candidate;
                }
            }

            return default;
        }

        private static void MigrateSet(string path, List<LegacyVariableConverter.Migration> migrations)
        {
            VariableSet set = AssetDatabase.LoadAssetAtPath<VariableSet>(path);
            if (set == null)
            {
                throw new InvalidOperationException($"No Variable Set found at '{path}'.");
            }

            if (LegacyVariableConverter.MigrateSet(set, migrations) > 0)
            {
                AssetDatabase.SaveAssetIfDirty(set);
            }
        }

        private static void LogToConsole(HyphrationLogEntry entry)
        {
            string kind = entry.Kind == HyphrationTargetKind.Scene ? "scene" : "Variable Set";
            if (!entry.Succeeded)
            {
                Debug.LogError($"[Hyphration] Failed to migrate {kind} '{entry.DisplayName}': " +
                    entry.FailureMessage);
                return;
            }

            var message = new StringBuilder($"[Hyphration] Migrated {entry.MigratedCount} " +
                $"variable(s) in {kind} '{entry.DisplayName}' ({entry.Path}).");
            for (int i = 0; i < entry.Migrations.Count; i++)
            {
                LegacyVariableConverter.Migration migration = entry.Migrations[i];
                string source = string.IsNullOrEmpty(migration.Source) ?
                    string.Empty :
                    $" on Flowchart '{migration.Source}'";
                message.Append($"\n  - '{migration.Key}'{source}: " +
                    $"{migration.FromType} → {migration.ToType}");
            }

            Debug.Log(message.ToString());
        }
    }
}
