namespace Lighthouse.Backend.Models.Authorization
{
    public sealed class EffectiveAccess
    {
        private readonly UserRole? systemRole;
        private readonly Dictionary<UnitKey, UserRole> unitRoles;

        private EffectiveAccess(UserRole? systemRole, Dictionary<UnitKey, UserRole> unitRoles)
        {
            this.systemRole = systemRole;
            this.unitRoles = unitRoles;
        }

        public static EffectiveAccess Resolve(IEnumerable<PermissionRule> ownerRows)
        {
            UserRole? systemRole = null;
            var unitRoles = new Dictionary<UnitKey, UserRole>();

            foreach (var row in ownerRows)
            {
                if (row is { ScopeType: PermissionScopeType.System, ScopeId: null })
                {
                    systemRole = Higher(systemRole, row.Role);
                    continue;
                }

                if (UnitKey.TryFrom(row, out var unit))
                {
                    unitRoles[unit] = Higher(unitRoles.TryGetValue(unit, out var existing) ? existing : null, row.Role);
                }
            }

            return new EffectiveAccess(systemRole, unitRoles);
        }

        public bool IsSystemAdmin => systemRole == UserRole.SystemAdmin;

        public bool AdministersAnyUnit => unitRoles.Any(entry => entry.Value == AdminRoleOn(entry.Key.ScopeType));

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
            return unitRoles
                .Where(entry => entry.Key.ScopeType == unitType && entry.Value == AdminRoleOn(unitType))
                .Select(entry => entry.Key.UnitId)
                .ToList();
        }

        public EffectiveAccess LimitedTo(IEnumerable<PermissionRule> keyRows)
        {
            return Resolve(keyRows.Where(Covers));
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
            return unitRoles.TryGetValue(unit, out var role) ? role : null;
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
    }
}
