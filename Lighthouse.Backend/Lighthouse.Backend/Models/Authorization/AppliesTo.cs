using System.Diagnostics.CodeAnalysis;

namespace Lighthouse.Backend.Models.Authorization
{
    /// <summary>
    /// The part of an SSO group rule that says which units it covers. It names no unit, so a Team or Portfolio created
    /// after the rule is covered with no further grant.
    /// </summary>
    public sealed class AppliesTo
    {
        private readonly PermissionScopeType storedScope;

        private AppliesTo(PermissionScopeType storedScope)
        {
            this.storedScope = storedScope;
        }

        public static AppliesTo AllUnits { get; } = new(PermissionScopeType.AllUnits);

        /// <summary>Reads what a stored row applies to. A row on one unit or on the System applies to no set of units.</summary>
        public static bool TryFrom(PermissionRule row, [NotNullWhen(true)] out AppliesTo? appliesTo)
        {
            appliesTo = row is { ScopeType: PermissionScopeType.AllUnits, ScopeId: null } ? AllUnits : null;
            return appliesTo is not null;
        }

        public bool Reaches(PermissionScopeType unitType)
        {
            return storedScope == PermissionScopeType.AllUnits
                && unitType is PermissionScopeType.Team or PermissionScopeType.Portfolio;
        }
    }
}
