using Lighthouse.Backend.Models.Authorization;

namespace Lighthouse.Backend.Tests.Models.Authorization
{
    /// <summary>
    /// Two rows share a scope when they name the same kind of scope and the same unit, whatever role each gives. A role
    /// granted to a person directly replaces their groups' roles on exactly that scope and no other.
    /// </summary>
    [TestFixture]
    public class PermissionRuleTest
    {
        private static readonly PermissionRule ViewerOnTeam5 = new(PermissionScopeType.Team, 5, UserRole.Viewer);

        [TestCase(PermissionScopeType.Team, 5, UserRole.TeamAdmin, true, TestName = "Another_role_on_the_same_Team_shares_the_scope")]
        [TestCase(PermissionScopeType.Team, 6, UserRole.Viewer, false, TestName = "The_same_role_on_another_Team_does_not_share_the_scope")]
        [TestCase(PermissionScopeType.Portfolio, 5, UserRole.Viewer, false, TestName = "A_Portfolio_with_the_same_number_does_not_share_the_scope")]
        public void Rows_share_a_scope_only_on_the_same_kind_of_scope_and_the_same_unit(PermissionScopeType scope, int unit, UserRole role, bool shared)
        {
            var other = new PermissionRule(scope, unit, role);

            Assert.That(ViewerOnTeam5.HasSameScopeAs(other), Is.EqualTo(shared), $"WHAT: Viewer on Team 5 and {role} on {scope} {unit} {(shared ? "do not share" : "share")} a scope. WHY: a direct role replaces group roles on the same scope only, so a wrong answer drops or keeps another unit's role. HOW: compare both the kind of scope and the unit.");
        }
    }
}
