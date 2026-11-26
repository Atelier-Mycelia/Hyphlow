using System;

namespace AtMycelia.Hyphlow
{
    /// <summary>
    /// Shared AccessScope presets used across Hyphlow.
    /// </summary>
    public static class AccessScopeDefaults
    {
        /// <summary>
        /// Whether things with this scope are visible to external contexts (e.g. other
        /// Flowcharts, registry lookups).
        /// </summary>
        public static bool IsVisibleToOutsiders(AccessScope scope)
        {
            return scope == AccessScope.Public ||
                scope == AccessScope.ReadOnly ||
                scope == AccessScope.Global;
        }

        /// <summary>
        /// AccessScope isn't a flags enum, so masking with this also lets Protected and
        /// Null through. Kept so existing code still compiles.
        /// </summary>
        [Obsolete("AccessScope isn't a flags enum, so masking with this lets Protected and " +
            "Null through. Use IsVisibleToOutsiders instead.")]
        public const AccessScope VisibleToOutsiders =
            AccessScope.Public | AccessScope.ReadOnly | AccessScope.Global;
    }
}
