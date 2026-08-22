#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Bootstrap entry points for runtime/editor default asset maintenance.
    /// Keeps Unity lifecycle attributes out of Infrastructure/Assets implementation.
    /// </summary>
    public static class DefaultAssetMaintenanceBootstrap
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        
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