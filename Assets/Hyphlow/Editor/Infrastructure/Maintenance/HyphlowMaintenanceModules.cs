using UnityEditor;
using HyphaTweenOrder = AtMycelia.HyphaTween.MaintenanceOrder;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// These are Hyphlow's hooks into the Common lib's <see cref="MaintenanceRunner"/>, which discovers
    /// them automatically and runs them in ascending Priority order. That order is the only
    /// thing deciding what runs when, so these tasks don't use their own lifecycle hooks or
    /// frame delays. Later steps may rely on assets that earlier ones ensure exist.

    public class EditorAssetMaintenanceModule : MaintenanceModule
    {
        public override int Priority => MaintenanceOrder.EditorAssets;

        public override void RunSceneMaintenance()
        {
            if (!MaintenanceOrder.IsPlayModeActive)
            {
                DefaultEditorAssetMaintenance.Run();
            }
        }
    }

    /// <summary>
    /// For modules that want to time their maintenances relative to ours.
    /// </summary>
    public static class MaintenanceOrder
    {
        internal static int StartPriority => HyphaTweenOrder.EndPriority + 1;
        // ^Since we want to make sure to start after HyphaTween is done

        public static int EditorAssets => StartPriority + 1; 
        public static int DefaultAssets => EditorAssets + 1;
        public static int LegacyVariableMigration => DefaultAssets + 1;
        public static int OldFungusStateRemoval => LegacyVariableMigration + 1;

        public static int EndPriority => OldFungusStateRemoval + 1;

        // The runner also calls RunSceneMaintenance on scene changes during Play Mode. The
        // asset and migration steps are edit-time work, and RunStartup already covers
        // what Play Mode needs from them.
        public static bool IsPlayModeActive => EditorApplication.isPlayingOrWillChangePlaymode;
    }


    public class DefaultAssetMaintenanceModule : MaintenanceModule
    {
        public override int Priority => MaintenanceOrder.DefaultAssets;

        // The singletons have to exist before the first scene loads.
        public override void RunStartup()
        {
            DefaultAssetMaintenance.Run();
        }

        public override void RunSceneMaintenance()
        {
            if (!MaintenanceOrder.IsPlayModeActive)
            {
                DefaultAssetMaintenance.Run();
            }
        }
    }

    public class LegacyVariableMigrationModule : MaintenanceModule
    {
        public override int Priority => MaintenanceOrder.LegacyVariableMigration;

        public override void RunSceneMaintenance()
        {
            LegacyVariableAutoMigrator.Run();
        }
    }

    public class OldFungusStateRemovalModule : MaintenanceModule
    {
        public override int Priority => MaintenanceOrder.OldFungusStateRemoval;

        public override void RunSceneMaintenance()
        {
            if (!MaintenanceOrder.IsPlayModeActive)
            {
                OldFungusStateRemover.Run();
            }
        }
    }
}
