using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Bootstrap entry points for runtime/editor default asset maintenance.
    /// Keeps Unity lifecycle attributes out of Infrastructure/Assets implementation.
    /// </summary>
    public static class DefaultAssetMaintenanceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        [InitializeOnLoadMethod]
        private static void InitAfterAssembliesLoaded()
        {
            DefaultAssetMaintenance.InitializeAfterAssembliesLoaded();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitBeforeSceneLoad()
        {
            DefaultAssetMaintenance.InitializeBeforeSceneLoad();
        }
    }
}