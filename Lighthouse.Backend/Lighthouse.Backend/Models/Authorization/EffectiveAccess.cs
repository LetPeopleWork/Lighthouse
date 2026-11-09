namespace Lighthouse.Backend.Models.Authorization
{
    /// <summary>
    /// What one person may do: their System role, kept apart, and their role on each Team and Portfolio, which is the
    /// higher of the role they hold on that unit and the role of every SSO group rule that reaches it. A rule only adds.
    /// The unit roles are worked out on the first unit question, so asking for the System role never depends on them.
    /// </summary>
    public sealed class EffectiveAccess
    {
        private readonly UserRole? systemRole;
        private readonly Lazy<UnitRoles> unitRoles;

        private EffectiveAccess(UserRole? systemRole, Lazy<UnitRoles> unitRoles)
        {
            this.systemRole = systemRole;
            this.unitRoles = unitRoles;
        }

        /// <param name="ownerRows">The rows the person holds, on the System or on one unit each.</param>
        /// <param name="ruleRows">The rows of the person's SSO groups that apply to a set of units rather than to one.</param>
        public static EffectiveAccess Resolve(IEnumerable<PermissionRule> ownerRows, IEnumerable<PermissionRule> ruleRows)
        {
            var owned = ownerRows.ToList();
            var systemRole = owned
                .Where(row => row is { ScopeType: PermissionScopeType.System, ScopeId: null })
                .Aggregate((UserRole?)null, (highest, row) => Higher(highest, row.Role));

            return new EffectiveAccess(systemRole, new Lazy<UnitRoles>(() => UnitRoles.From(owned, ruleRows)));
        }

        public bool IsSystemAdmin => systemRole == UserRole.SystemAdmin;

        public bool AdministersAnyUnit => unitRoles.Value.AdministersAny;

        public bool CanRead(PermissionScopeType unitType, int unitId)
        {
            var role = RoleOn(new UnitKey(unitType, unitId));
            return role == UserRole.Viewer || (role is not null && role == AdminRoleOn(unitType));
        }

        public bool CanWrite(PermissionScopeType unitType, int unitId)
        {
            var role = RoleOn(new UnitKey(unitType, unitId));
            return role is not null && role == AdminRoleOn(unitType);
        }

        public IReadOnlyList<int> AdministeredUnitIds(PermissionScopeType unitType)
        {
            return unitRoles.Value.AdministeredUnitIds(unitType);
        }

        /// <summary>
        /// The access of an API key limited to these rows: each row the owner covers on that unit, at the key's own
        /// scope. The key never takes over the owner's rules, so it reaches nothing beyond its own rows.
        /// </summary>
        public EffectiveAccess LimitedTo(IEnumerable<PermissionRule> keyRows)
        {
            return Resolve(keyRows.Where(Covers), []);
        }

        private bool Covers(PermissionRule keyRow)
        {
            if (IsSystemAdmin)
            {
                return true;
            }

            if (!UnitKey.TryFrom(keyRow, out var unit))
            {
                return false;
            }

            var ownerRole = RoleOn(unit);
            return ownerRole is not null && Priority(ownerRole.Value) >= Priority(keyRow.Role);
        }

        private UserRole? RoleOn(UnitKey unit)
        {
            return unitRoles.Value.RoleOn(unit);
        }

        private static UserRole? AdminRoleOn(PermissionScopeType unitType)
        {
            return unitType switch
            {
                PermissionScopeType.Team => UserRole.TeamAdmin,
                PermissionScopeType.Portfolio => UserRole.PortfolioAdmin,
                _ => null,
            };
        }

        private static UserRole Higher(UserRole? current, UserRole candidate)
        {
            return current is null || Priority(candidate) > Priority(current.Value) ? candidate : current.Value;
        }

        private static int Priority(UserRole role)
        {
            return role switch
            {
                UserRole.SystemAdmin => 4,
                UserRole.TeamAdmin => 3,
                UserRole.PortfolioAdmin => 3,
                UserRole.Viewer => 2,
                _ => 0,
            };
        }

        private readonly record struct UnitKey(PermissionScopeType ScopeType, int UnitId)
        {
            public static bool TryFrom(PermissionRule row, out UnitKey unit)
            {
                unit = default;
                if (row.ScopeType is not (PermissionScopeType.Team or PermissionScopeType.Portfolio) || row.ScopeId is not { } unitId)
                {
                    return false;
                }

                unit = new UnitKey(row.ScopeType, unitId);
                return true;
            }
        }

        private sealed record Reach(AppliesTo AppliesTo, UserRole Role);

        private sealed class UnitRoles
        {
            private readonly Dictionary<UnitKey, UserRole> heldOnUnits;
            private readonly List<Reach> reaches;

            private UnitRoles(Dictionary<UnitKey, UserRole> heldOnUnits, List<Reach> reaches)
            {
                this.heldOnUnits = heldOnUnits;
                this.reaches = reaches;
            }

            public static UnitRoles From(IEnumerable<PermissionRule> ownerRows, IEnumerable<PermissionRule> ruleRows)
            {
                var heldOnUnits = new Dictionary<UnitKey, UserRole>();
                foreach (var row in ownerRows)
                {
                    if (UnitKey.TryFrom(row, out var unit))
                    {
                        heldOnUnits[unit] = HigherOn(unit.ScopeType, heldOnUnits.TryGetValue(unit, out var existing) ? existing : null, row.Role);
                    }
                }

                var reaches = new List<Reach>();
                foreach (var row in ruleRows)
                {
                    if (AppliesTo.TryFrom(row, out var appliesTo))
                    {
                        reaches.Add(new Reach(appliesTo, row.Role));
                    }
                }

                return new UnitRoles(heldOnUnits, reaches);
            }

            public bool AdministersAny => heldOnUnits.Any(entry => entry.Value == AdminRoleOn(entry.Key.ScopeType));

            public UserRole? RoleOn(UnitKey unit)
            {
                UserRole? held = heldOnUnits.TryGetValue(unit, out var role) ? role : null;
                return reaches
                    .Where(reach => reach.AppliesTo.Reaches(unit.ScopeType))
                    .Aggregate(held, (highest, reach) => HigherOn(unit.ScopeType, highest, reach.Role));
            }

            public List<int> AdministeredUnitIds(PermissionScopeType unitType)
            {
                return heldOnUnits
                    .Where(entry => entry.Key.ScopeType == unitType && entry.Value == AdminRoleOn(unitType))
                    .Select(entry => entry.Key.UnitId)
                    .ToList();
            }

            // On a unit, a role that does not belong to that unit type, such as Portfolio Admin on a Team, gives
            // nothing there, so it must not hide a Viewer rule that reaches the unit.
            private static UserRole HigherOn(PermissionScopeType unitType, UserRole? current, UserRole candidate)
            {
                return current is null || PriorityOn(unitType, candidate) > PriorityOn(unitType, current.Value) ? candidate : current.Value;
            }

            private static int PriorityOn(PermissionScopeType unitType, UserRole role)
            {
                return role == UserRole.Viewer || role == AdminRoleOn(unitType) ? Priority(role) : 0;
            }
        }
    }
}
