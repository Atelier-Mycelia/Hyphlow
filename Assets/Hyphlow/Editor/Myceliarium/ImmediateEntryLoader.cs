using AtMycelia.Myceliarium;
using System;

namespace AtMycelia.Hyphlow.MyceliariumInt
{
    /// <summary>
    /// For entries whose data comes from the AssetDatabase, which answers right away.
    /// The base loader runs its process as a coroutine, so the result only arrives on the
    /// next editor update; with nothing to wait for, that would just leave the entry
    /// briefly unloaded, and make the panel's reloads (on opening, on project changes)
    /// harder to reason about.
    /// </summary>
    public abstract class ImmediateEntryLoader : ControlPanelEntryLoader
    {
        protected abstract object Read();

        public override void Load(IControlPanelEntry toLoadFor, Action onComplete = null)
        {
            _loadResult = Read();
            onComplete?.Invoke();
        }
    }
}
