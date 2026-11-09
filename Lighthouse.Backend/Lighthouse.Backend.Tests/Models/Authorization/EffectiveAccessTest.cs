using System.Reflection;
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

        private static PermissionRule ViewerOnAll => new(AllUnitsScope(), null, UserRole.Viewer);

        /// <summary>L1: no row gives no access.</summary>
        [Test]
        public void No_row_gives_nothing()
        {
            var access = Resolve([], []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(access.CanRead(PermissionScopeType.Team, TeamA), Is.False, "WHAT: a person with no row reads a Team. WHY: access is opt-in. HOW: answer none without a row.");
                Assert.That(access.IsSystemAdmin, Is.False, "WHAT: a person with no row is a System Admin. WHY: access is opt-in. HOW: answer none without a row.");
            }
        }

        /// <summary>L1, L3: a Viewer rule alone reads every Team and Portfolio, writes none, and gives no System role.</summary>
        [Test]
        public void A_Viewer_rule_alone_reads_every_Team_and_Portfolio_and_writes_none()
        {
            var access = Resolve([], [ViewerOnAll]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(access.CanRead(PermissionScopeType.Team, TeamA), Is.True, "WHAT: Viewer on all does not read Team A. WHY: the rule reaches every Team. HOW: answer RoleOn from the rules that reach the unit.");
                Assert.That(access.CanRead(PermissionScopeType.Portfolio, PortfolioP), Is.True, "WHAT: Viewer on all does not read the Portfolio. WHY: the rule reaches every Portfolio. HOW: as above.");
                Assert.That(access.CanWrite(PermissionScopeType.Team, TeamA), Is.False, "WHAT: Viewer on all writes a Team. WHY: Viewer is below Team Admin. HOW: a Viewer reach never writes.");
                Assert.That(access.IsSystemAdmin, Is.False, "WHAT: a rule-only person is a System Admin. WHY: rules never change the System role. HOW: resolve the System role from the System row only.");
            }
        }

        /// <summary>L1: Team Admin on Team A plus a Viewer rule is Team Admin on A and Viewer elsewhere.</summary>
        [Test]
        public void A_unit_role_above_the_rule_wins_on_its_unit_and_the_rule_reaches_the_rest()
        {
            var access = Resolve([new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.TeamAdmin)], [ViewerOnAll]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(access.CanWrite(PermissionScopeType.Team, TeamA), Is.True, "WHAT: a Team Admin of Team A lost the write once a Viewer rule reached them. WHY: a rule only adds. HOW: take the higher of the unit role and the rule.");
                Assert.That(access.CanRead(PermissionScopeType.Team, TeamB), Is.True, "WHAT: Team B is not readable. WHY: the Viewer rule reaches it. HOW: as above.");
                Assert.That(access.CanWrite(PermissionScopeType.Team, TeamB), Is.False, "WHAT: Team B is writable. WHY: only Team A has an admin role. HOW: as above.");
            }
        }

        /// <summary>L1: Viewer on Team A plus a Viewer rule stays Viewer, including where a direct role replaced a group role on that Team.</summary>
        [Test]
        public void A_Viewer_on_a_Team_plus_a_Viewer_rule_stays_Viewer()
        {
            var access = Resolve([new PermissionRule(PermissionScopeType.Team, TeamA, UserRole.Viewer)], [ViewerOnAll]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(access.CanRead(PermissionScopeType.Team, TeamA), Is.True, "WHAT: Viewer on Team A plus the rule cannot read Team A. WHY: both give Viewer. HOW: take the higher of both.");
                Assert.That(access.CanWrite(PermissionScopeType.Team, TeamA), Is.False, "WHAT: two Viewer grants became a write. WHY: the higher of two Viewers is Viewer. HOW: compare by role priority, never add.");
            }
        }

        /// <summary>L2: for every role on a Team or Portfolio, adding a Viewer rule never lowers a read or write answer.</summary>
        [TestCaseSource(nameof(EveryUnitRole))]
        public void Adding_a_Viewer_rule_never_lowers_an_answer(PermissionScopeType unit, UserRole role)
        {
            var without = Resolve([new PermissionRule(unit, TeamA, role)], []);
            var with = Resolve([new PermissionRule(unit, TeamA, role)], [ViewerOnAll]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(with.CanRead(unit, TeamA), Is.GreaterThanOrEqualTo(without.CanRead(unit, TeamA)).And.True, $"WHAT: {role} on a {unit} reads less once a Viewer rule is added. WHY: a rule only adds, and Viewer on all reads every unit. HOW: take the higher role.");
                Assert.That(with.CanWrite(unit, TeamA), Is.EqualTo(without.CanWrite(unit, TeamA)), $"WHAT: the write answer of {role} on a {unit} changed when a Viewer rule was added. WHY: Viewer neither adds nor removes a write. HOW: take the higher role.");
            }
        }

        /// <summary>L3: a System Admin stays System Admin with and without rules.</summary>
        [Test]
        public void The_System_role_comes_from_the_System_row_alone()
        {
            var systemRow = new PermissionRule(PermissionScopeType.System, null, UserRole.SystemAdmin);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Resolve([systemRow], []).IsSystemAdmin, Is.True, "WHAT: a System Admin without rules is not System Admin. WHY: the System row decides. HOW: keep the System role apart from unit reach.");
                Assert.That(Resolve([systemRow], [ViewerOnAll]).IsSystemAdmin, Is.True, "WHAT: a rule took the System role away. WHY: rules never change it. HOW: as above.");
            }
        }

        private static IEnumerable<TestCaseData> EveryUnitRole()
            => from unit in new[] { PermissionScopeType.Team, PermissionScopeType.Portfolio }
               from role in Enum.GetValues<UserRole>().Where(role => role != UserRole.SystemAdmin)
               select new TestCaseData(unit, role);

        private static PermissionScopeType AllUnitsScope()
        {
            Assert.That(Enum.TryParse<PermissionScopeType>("AllUnits", out var allUnits), Is.True, "WHAT: there is no AllUnits scope. WHY: a rule for every Team and Portfolio is stored on it. HOW: append AllUnits = 3.");
            return allUnits;
        }

        // The rule rows are a second input, apart from the person's own rows, so that the direct-replaces-group merge
        // runs on unit rows only. It is found by its parameters so that this class compiles before it exists.
        private static EffectiveAccess Resolve(PermissionRule[] ownerRows, PermissionRule[] ruleRows)
        {
            var resolve = typeof(EffectiveAccess).GetMethod(
                nameof(EffectiveAccess.Resolve),
                BindingFlags.Public | BindingFlags.Static,
                [typeof(IEnumerable<PermissionRule>), typeof(IEnumerable<PermissionRule>)]);
            Assert.That(resolve, Is.Not.Null, "WHAT: EffectiveAccess cannot be resolved from a person's rows and their groups' rules. WHY: rules reach units by their scope and must not be expanded into unit rows. HOW: add Resolve(ownerRows, ruleRows).");
            return (EffectiveAccess)resolve!.Invoke(null, [ownerRows, ruleRows])!;
        }
    }
}
