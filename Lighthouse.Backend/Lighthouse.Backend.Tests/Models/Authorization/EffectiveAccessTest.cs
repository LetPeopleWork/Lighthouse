using Lighthouse.Backend.Models.Authorization;

namespace Lighthouse.Backend.Tests.Models.Authorization
{
    /// <summary>
    /// What one person may do, worked out from the rows they hold on units and the rules of their groups. A rule only
    /// adds: the role on a unit is the higher of the person's own role there and every rule that reaches it, and the
    /// System role comes from the System row alone.
    /// </summary>
    [TestFixture]
    public class EffectiveAccessTest
    {
        private const int TeamA = 1;

        private const int TeamB = 2;

        private const int PortfolioP = 7;

        private static readonly PermissionRule ViewerOnAll = new(PermissionScopeType.AllUnits, null, UserRole.Viewer);

        private static readonly PermissionRule SystemAdminRow = new(PermissionScopeType.System, null, UserRole.SystemAdmin);

        [Test]
        public void No_row_gives_nothing()
        {
            var access = EffectiveAccess.Resolve([], []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(access.CanRead(PermissionScopeType.Team, TeamA), Is.False, "WHAT: a person with no row reads a Team. WHY: access is opt-in. HOW: answer none without a row.");
                Assert.That(access.IsSystemAdmin, Is.False, "WHAT: a person with no row is a System Admin. WHY: access is opt-in. HOW: answer none without a row.");
            }
        }

        [Test]
        public void A_Viewer_rule_alone_reads_every_Team_and_Portfolio_and_writes_none()
        {
            var access = EffectiveAccess.Resolve([], [ViewerOnAll]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(access.CanRead(PermissionScopeType.Team, TeamA), Is.True, "WHAT: Viewer on all does not read Team A. WHY: the rule reaches every Team. HOW: answer RoleOn from the rules that reach the unit.");
                Assert.That(access.CanRead(PermissionScopeType.Portfolio, PortfolioP), Is.True, "WHAT: Viewer on all does not read the Portfolio. WHY: the rule reaches every Portfolio. HOW: as above.");
                Assert.That(access.CanWrite(PermissionScopeType.Team, TeamA), Is.False, "WHAT: Viewer on all writes a Team. WHY: Viewer is below Team Admin. HOW: a Viewer reach never writes.");
                Assert.That(access.IsSystemAdmin, Is.False, "WHAT: a rule-only person is a System Admin. WHY: rules never change the System role. HOW: resolve the System role from the System row only.");
            }
        }

        [Test]
        public void A_unit_role_above_the_rule_wins_on_its_unit_and_the_rule_reaches_the_rest()
        {
            var access = EffectiveAccess.Resolve([new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.TeamAdmin)], [ViewerOnAll]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(access.CanWrite(PermissionScopeType.Team, TeamA), Is.True, "WHAT: a Team Admin of Team A lost the write once a Viewer rule reached them. WHY: a rule only adds. HOW: take the higher of the unit role and the rule.");
                Assert.That(access.CanRead(PermissionScopeType.Team, TeamB), Is.True, "WHAT: Team B is not readable. WHY: the Viewer rule reaches it. HOW: as above.");
                Assert.That(access.CanWrite(PermissionScopeType.Team, TeamB), Is.False, "WHAT: Team B is writable. WHY: only Team A has an admin role. HOW: as above.");
            }
        }

        [Test]
        public void A_Viewer_on_a_Team_plus_a_Viewer_rule_stays_Viewer()
        {
            var access = EffectiveAccess.Resolve([new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.Viewer)], [ViewerOnAll]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(access.CanRead(PermissionScopeType.Team, TeamA), Is.True, "WHAT: Viewer on Team A plus the rule cannot read Team A. WHY: both give Viewer. HOW: take the higher of both.");
                Assert.That(access.CanWrite(PermissionScopeType.Team, TeamA), Is.False, "WHAT: two Viewer grants became a write. WHY: the higher of two Viewers is Viewer. HOW: compare by role priority, never add.");
            }
        }

        [TestCaseSource(nameof(EveryUnitRole))]
        public void Adding_a_Viewer_rule_never_lowers_an_answer(PermissionScopeType unit, UserRole role)
        {
            var without = EffectiveAccess.Resolve([new PermissionRule(unit, TeamA, role)], []);
            var with = EffectiveAccess.Resolve([new PermissionRule(unit, TeamA, role)], [ViewerOnAll]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(with.CanRead(unit, TeamA), Is.GreaterThanOrEqualTo(without.CanRead(unit, TeamA)).And.True, $"WHAT: {role} on a {unit} reads less once a Viewer rule is added. WHY: a rule only adds, and Viewer on all reads every unit. HOW: take the higher role.");
                Assert.That(with.CanWrite(unit, TeamA), Is.EqualTo(without.CanWrite(unit, TeamA)), $"WHAT: the write answer of {role} on a {unit} changed when a Viewer rule was added. WHY: Viewer neither adds nor removes a write. HOW: take the higher role.");
            }
        }

        [Test]
        public void A_role_that_does_not_belong_to_the_unit_type_gives_no_read_there()
        {
            var access = EffectiveAccess.Resolve([new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.PortfolioAdmin)], []);

            Assert.That(access.CanRead(PermissionScopeType.Team, TeamA), Is.False, "WHAT: Portfolio Admin held on a Team reads that Team. WHY: only Viewer or the admin role of that unit type reads a unit. HOW: read only with Viewer or the unit type's admin role.");
        }

        [Test]
        public void Neither_a_row_nor_a_rule_gives_access_on_a_scope_that_is_not_a_Team_or_Portfolio()
        {
            var aRowOnNoUnit = new PermissionRule(PermissionScopeType.Team, null, UserRole.Viewer);

            var access = EffectiveAccess.Resolve([aRowOnNoUnit], [ViewerOnAll]);

            Assert.That(access.CanRead(PermissionScopeType.System, 0), Is.False, "WHAT: a row naming no unit, or a rule for every Team and Portfolio, reads the System scope. WHY: a rule reaches Teams and Portfolios only, and a row without a unit is no unit role. HOW: give a rule's role on Teams and Portfolios only, and skip rows that name no unit.");
        }

        [Test]
        public void The_System_role_comes_from_the_System_row_alone()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(EffectiveAccess.Resolve([SystemAdminRow], []).IsSystemAdmin, Is.True, "WHAT: a System Admin without rules is not System Admin. WHY: the System row decides. HOW: keep the System role apart from unit reach.");
                Assert.That(EffectiveAccess.Resolve([SystemAdminRow], [ViewerOnAll]).IsSystemAdmin, Is.True, "WHAT: a rule took the System role away. WHY: rules never change it. HOW: as above.");
            }
        }

        [Test]
        public void The_highest_System_row_decides_whatever_order_the_rows_come_in()
        {
            var viewerRow = new PermissionRule(PermissionScopeType.System, null, UserRole.Viewer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(EffectiveAccess.Resolve([SystemAdminRow, viewerRow], []).IsSystemAdmin, Is.True, "WHAT: a lower System row read after System Admin took the role away. WHY: the highest System role counts. HOW: keep the higher role while reading the rows.");
                Assert.That(EffectiveAccess.Resolve([viewerRow, SystemAdminRow], []).IsSystemAdmin, Is.True, "WHAT: System Admin read after a lower System row was lost. WHY: the highest System role counts. HOW: as above.");
            }
        }

        [Test]
        public void A_System_Admins_key_keeps_every_row_it_is_limited_to()
        {
            var key = EffectiveAccess.Resolve([SystemAdminRow], []).LimitedTo([new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.Viewer)]);

            Assert.That(key.CanRead(PermissionScopeType.Team, TeamA), Is.True, "WHAT: a System Admin's key limited to Viewer on Team A cannot read it. WHY: a System Admin covers every row a key can be limited to. HOW: keep every key row for a System Admin owner.");
        }

        [Test]
        public void A_key_keeps_only_the_unit_rows_its_owner_covers()
        {
            var owner = EffectiveAccess.Resolve([new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.TeamAdmin)], []);

            var key = owner.LimitedTo(
            [
                new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.TeamAdmin),
                new PermissionRule(PermissionScopeType.Team, TeamB, UserRole.Viewer),
                SystemAdminRow,
            ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(key.CanWrite(PermissionScopeType.Team, TeamA), Is.True, "WHAT: the key lost the Team Admin row its owner holds. WHY: a key keeps what its owner covers. HOW: keep a key row the owner's role on that unit covers.");
                Assert.That(key.CanRead(PermissionScopeType.Team, TeamB), Is.False, "WHAT: the key reads a Team its owner cannot. WHY: a key never reaches beyond its owner. HOW: drop a key row on a unit the owner holds nothing on.");
                Assert.That(key.IsSystemAdmin, Is.False, "WHAT: the key of a Team Admin became System Admin. WHY: a row on no unit is covered only by a System Admin owner. HOW: drop a key row that names no unit unless the owner is System Admin.");
            }
        }

        // A role stored on a unit of the other kind, or System Admin stored on a unit, gives nothing on that unit, so a
        // key its holder limits to that unit must get nothing there either, however high the stored role ranks.
        [TestCase(PermissionScopeType.Team, UserRole.PortfolioAdmin, UserRole.Viewer, false, false)]
        [TestCase(PermissionScopeType.Team, UserRole.PortfolioAdmin, UserRole.TeamAdmin, false, false)]
        [TestCase(PermissionScopeType.Team, UserRole.SystemAdmin, UserRole.Viewer, false, false)]
        [TestCase(PermissionScopeType.Team, UserRole.SystemAdmin, UserRole.TeamAdmin, false, false)]
        [TestCase(PermissionScopeType.Team, UserRole.TeamAdmin, UserRole.Viewer, true, false)]
        [TestCase(PermissionScopeType.Team, UserRole.TeamAdmin, UserRole.TeamAdmin, true, true)]
        [TestCase(PermissionScopeType.Portfolio, UserRole.TeamAdmin, UserRole.Viewer, false, false)]
        [TestCase(PermissionScopeType.Portfolio, UserRole.TeamAdmin, UserRole.PortfolioAdmin, false, false)]
        [TestCase(PermissionScopeType.Portfolio, UserRole.SystemAdmin, UserRole.Viewer, false, false)]
        [TestCase(PermissionScopeType.Portfolio, UserRole.SystemAdmin, UserRole.PortfolioAdmin, false, false)]
        [TestCase(PermissionScopeType.Portfolio, UserRole.PortfolioAdmin, UserRole.Viewer, true, false)]
        [TestCase(PermissionScopeType.Portfolio, UserRole.PortfolioAdmin, UserRole.PortfolioAdmin, true, true)]
        public void A_key_reaches_a_unit_only_as_far_as_its_owners_role_there_gives(PermissionScopeType unit, UserRole ownerRole, UserRole keyRole, bool keyReads, bool keyWrites)
        {
            var key = KeyOf([new PermissionRule(unit, TeamA, ownerRole)], new PermissionRule(unit, TeamA, keyRole));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(key.CanRead(unit, TeamA), Is.EqualTo(keyReads), $"WHAT: a key limited to {keyRole} on a {unit}, whose owner holds {ownerRole} there, answers read={!keyReads}. WHY: a key reaches a unit only at a role its owner's role on that unit gives there, and {ownerRole} on a {unit} gives {(keyReads ? "at least" : "nothing toward")} {keyRole}. HOW: judge the owner's role on the unit by what it gives on that kind of unit, as the owner's own read is judged, not by its general rank.");
                Assert.That(key.CanWrite(unit, TeamA), Is.EqualTo(keyWrites), $"WHAT: a key limited to {keyRole} on a {unit}, whose owner holds {ownerRole} there, answers write={!keyWrites}. WHY: a key never administers a unit its owner does not administer, and never loses what its owner covers. HOW: as above.");
            }
        }

        [Test]
        public void The_same_key_follows_its_owners_role_on_the_Team_as_it_changes()
        {
            var keyRow = new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.TeamAdmin);

            var whileOwnerAdministers = KeyOf([new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.TeamAdmin)], keyRow);
            var afterOwnerIsLoweredToViewer = KeyOf([new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.Viewer)], keyRow);
            var afterOwnerLosesTheTeam = KeyOf([], keyRow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whileOwnerAdministers.CanWrite(PermissionScopeType.Team, TeamA), Is.True, "WHAT: a Team Admin key of a Team Admin owner cannot write the Team. WHY: the owner's role covers the key. HOW: keep a key row the owner's role on that unit gives.");
                Assert.That(afterOwnerIsLoweredToViewer.CanWrite(PermissionScopeType.Team, TeamA), Is.False, "WHAT: a Team Admin key still writes after its owner was lowered to Viewer. WHY: a key never acts above its owner's current role on the unit. HOW: judge the key against the owner's role as it is now.");
                Assert.That(afterOwnerLosesTheTeam.CanRead(PermissionScopeType.Team, TeamA), Is.False, "WHAT: a key still reads a Team its owner no longer holds. WHY: a key reaches nothing its owner does not. HOW: drop a key row on a unit the owner holds nothing on.");
            }
        }

        [TestCaseSource(nameof(EveryOwnerAndKeyRoleOnAUnit))]
        public void A_key_never_reads_or_writes_a_unit_its_owner_cannot(PermissionScopeType unit, UserRole? ownerRole, UserRole keyRole)
        {
            PermissionRule[] ownerRows = ownerRole is { } role ? [new PermissionRule(unit, TeamA, role)] : [];
            var owner = EffectiveAccess.Resolve(ownerRows, []);

            var key = owner.LimitedTo([new PermissionRule(unit, TeamA, keyRole)]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(!key.CanRead(unit, TeamA) || owner.CanRead(unit, TeamA), Is.True, $"WHAT: a key limited to {keyRole} on a {unit} reads it while its owner, holding {ownerRole?.ToString() ?? "nothing"} there, cannot. WHY: a key never reaches beyond its owner. HOW: judge the owner's role on the unit by what it gives on that kind of unit.");
                Assert.That(!key.CanWrite(unit, TeamA) || owner.CanWrite(unit, TeamA), Is.True, $"WHAT: a key limited to {keyRole} on a {unit} writes it while its owner, holding {ownerRole?.ToString() ?? "nothing"} there, cannot. WHY: a key never reaches beyond its owner. HOW: as above.");
            }
        }

        private static EffectiveAccess KeyOf(PermissionRule[] ownerRows, PermissionRule keyRow)
        {
            return EffectiveAccess.Resolve(ownerRows, []).LimitedTo([keyRow]);
        }

        private static IEnumerable<TestCaseData> EveryOwnerAndKeyRoleOnAUnit()
            => from unit in new[] { PermissionScopeType.Team, PermissionScopeType.Portfolio }
               from ownerRole in Enum.GetValues<UserRole>().Select(role => (UserRole?)role).Append(null)
               from keyRole in Enum.GetValues<UserRole>()
               select new TestCaseData(unit, ownerRole, keyRole);

        private static IEnumerable<TestCaseData> EveryUnitRole()
            => from unit in new[] { PermissionScopeType.Team, PermissionScopeType.Portfolio }
               from role in Enum.GetValues<UserRole>().Where(role => role != UserRole.SystemAdmin)
               select new TestCaseData(unit, role);
    }
}
