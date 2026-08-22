using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    /// <summary>
    /// Bootstrap entry points for editor-only asset maintenance.
    /// Keeps Unity lifecycle attributes out of Infrastructure/Assets implementation.
    /// </summary>
    public static class DefaultEditorAssetMaintenanceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        [InitializeOnLoadMethod]
        private static void InitAfterAssembliesLoaded()
        {
            DefaultEditorAssetMaintenance.InitializeAfterAssembliesLoaded();
        }
    }
}