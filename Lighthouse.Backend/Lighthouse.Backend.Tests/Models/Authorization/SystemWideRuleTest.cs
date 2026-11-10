using Lighthouse.Backend.Models.Authorization;

namespace Lighthouse.Backend.Tests.Models.Authorization
{
    /// <summary>
    /// A stored group mapping reads as a rule on a unit's Access tab only when it applies to every Team and Portfolio
    /// and the unit is a Team or a Portfolio. A rule on one unit, a System Admin group and a scope that is not a unit
    /// read as no rule.
    /// </summary>
    [TestFixture]
    public class SystemWideRuleTest
    {
        [TestCase(PermissionScopeType.Team)]
        [TestCase(PermissionScopeType.Portfolio)]
        public void A_rule_for_every_unit_reads_as_its_role_and_group_on_a_Team_or_Portfolio(PermissionScopeType unitType)
        {
            var read = SystemWideRule.TryReading(AMapping(UserRole.UnitAdmin, PermissionScopeType.AllUnits, null), unitType, out var rule);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.True, $"WHAT: Admin on all does not read as a rule on a {unitType}. WHY: it reaches every Team and Portfolio. HOW: read a mapping on every unit as a rule.");
                Assert.That(rule, Is.EqualTo(new SystemWideRule(UserRole.UnitAdmin, "delivery-leads")), "WHAT: the rule read is not the mapping's role and group. WHY: a unit's admin sees who reaches the unit and with which role. HOW: carry the mapping's role and group.");
            }
        }

        [Test]
        public void A_rule_for_every_unit_reads_as_no_rule_on_a_scope_that_is_not_a_unit()
        {
            var read = SystemWideRule.TryReading(AMapping(UserRole.Viewer, PermissionScopeType.AllUnits, null), PermissionScopeType.System, out _);

            Assert.That(read, Is.False, "WHAT: Viewer on all reads as a rule on the System scope. WHY: a rule reaches Teams and Portfolios only. HOW: read the rule only for a Team or a Portfolio.");
        }

        [TestCase(UserRole.Viewer, PermissionScopeType.Team, 5, TestName = "A_rule_on_one_Team_reads_as_no_rule")]
        [TestCase(UserRole.SystemAdmin, PermissionScopeType.System, null, TestName = "A_System_Admin_group_reads_as_no_rule")]
        public void A_mapping_that_does_not_apply_to_every_unit_reads_as_no_rule(UserRole role, PermissionScopeType scope, int? unit)
        {
            var read = SystemWideRule.TryReading(AMapping(role, scope, unit), PermissionScopeType.Team, out _);

            Assert.That(read, Is.False, $"WHAT: {role} on {scope} reads as a rule for every unit. WHY: the Access tab lists one unit's groups below and never names a System Admin group. HOW: read only mappings that apply to every Team and Portfolio.");
        }

        private static RbacGroupMapping AMapping(UserRole role, PermissionScopeType scope, int? unit)
            => new() { GroupValue = "delivery-leads", Role = role, ScopeType = scope, ScopeId = unit };
    }
}
