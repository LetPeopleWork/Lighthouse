namespace Lighthouse.Backend.Models.Authorization
{
    /// <summary>
    /// A rule that gives a group, or everyone signed in, a role on every Team and Portfolio, as a unit's admin reads it:
    /// the role and who holds it, with no stored line behind it to change.
    /// </summary>
    public sealed record SystemWideRule(UserRole Role, string Holder)
    {
        /// <summary>What the everyone-signed-in switch gives while it is on: Viewer, held by everyone signed in.</summary>
        public static SystemWideRule EveryoneSignedIn { get; } = new(UserRole.Viewer, "everyone signed in");

        /// <summary>Reads a stored group mapping as a rule reaching a unit of the given kind. A System Admin group and a rule on one unit are none.</summary>
        public static bool TryReading(RbacGroupMapping mapping, PermissionScopeType unitType, out SystemWideRule rule)
        {
            var reaches = mapping.Role != UserRole.SystemAdmin
                && AppliesTo.TryFrom(new PermissionRule(mapping.ScopeType, mapping.ScopeId, mapping.Role), out var appliesTo)
                && appliesTo.Reaches(unitType);
            rule = reaches ? new SystemWideRule(mapping.Role, mapping.GroupValue) : null!;
            return reaches;
        }
    }
}
