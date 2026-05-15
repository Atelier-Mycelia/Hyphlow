namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Shared AccessScope presets used across Hyphlow.
    /// </summary>
    public static class AccessScopeDefaults
    {
        /// <summary>
        /// Scopes visible to external contexts (e.g. other Flowcharts, registry lookups).
        /// </summary>
        public const AccessScope VisibleToOutsiders =
            AccessScope.Public | AccessScope.ReadOnly | AccessScope.Global;
    }
}