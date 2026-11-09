using Lighthouse.Backend.Models.Authorization;
using Microsoft.AspNetCore.Http;
using static Lighthouse.Backend.Tests.Acceptance.SystemWideAccess.SystemWideAccessScenario;

namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// A person who reads through Viewer on all limits an API key to one Team. The key works on that Team at its exact
    /// scope and reaches nothing else, never more than its owner has, and stops as soon as the rule is removed.
    /// </summary>
    [TestFixture]
    public class LimitedKeyUnderRuleAtThePortTest
    {
        private SystemWideAccessScenario scenario = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => scenario = new SystemWideAccessScenario();

        [TearDown]
        public void Dispose() => scenario.Dispose();

        [Test]
        public async Task A_member_may_limit_a_key_to_Viewer_on_a_Team_she_reaches_through_the_rule()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);

            var mayIssue = await scenario.MayIssueAKeyAs(Noor_SignedIn(), RbacGuardRequirement.TeamRead, TeamZenith);

            Assert.That(mayIssue, Is.True, "WHAT: a member reading through Viewer on all may not issue a key limited to Viewer on Team Zenith. WHY: she reads Team Zenith, so a key that reads it is no broader than she is. HOW: let the issuing check use the owner's reach.");
        }

        [Test]
        public async Task A_key_limited_to_Viewer_on_one_Team_reads_that_Team_and_nothing_else()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);
            var zenithReport = scenario.AKeyIsStoredFor(Noor, (UserRole.Viewer, PermissionScopeType.Team, TeamZenith));

            var readsZenith = await scenario.CanReadTeam(TheKey(Noor, zenithReport), TeamZenith);
            var readsApollo = await scenario.CanReadTeam(TheKey(Noor, zenithReport), TeamApollo);
            var readsOceanExplorer = await scenario.CanReadPortfolio(TheKey(Noor, zenithReport), PortfolioOceanExplorer);
            var writesZenith = await scenario.CanWriteTeam(TheKey(Noor, zenithReport), TeamZenith);
            var teams = await scenario.TheTeamsListed(TheKey(Noor, zenithReport), TeamZenith, TeamApollo, TeamMeridianCreatedLater);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readsZenith, Is.True, "WHAT: the key limited to Viewer on Team Zenith cannot read Team Zenith. WHY: its owner reaches Team Zenith through the rule, so the limit is covered. HOW: let key coverage ask the owner's role on the key's unit, rule included.");
                Assert.That(readsApollo, Is.False, "WHAT: the key reads Team Apollo. WHY: a limited key keeps its exact scope even when its owner reaches every Team. HOW: give the key its own rows only, never the owner's reach.");
                Assert.That(readsOceanExplorer, Is.False, "WHAT: the key reads a Portfolio. WHY: it is limited to one Team. HOW: give the key its own rows only.");
                Assert.That(writesZenith, Is.False, "WHAT: the key changes Team Zenith. WHY: it is limited to Viewer, and its owner only reads. HOW: never exceed the lower of the limit and the owner's role.");
                Assert.That(teams, Is.EqualTo(new[] { TeamZenith }), "WHAT: the key's Team list is not Team Zenith alone. WHY: lists follow the same exact scope. HOW: filter lists through the limited access.");
            }
        }

        [Test]
        public async Task A_member_asking_for_a_key_as_Team_Admin_is_refused_with_the_existing_message()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);

            var answer = await scenario.AsksForAKey(Noor_SignedIn(), UserRole.TeamAdmin, TeamZenith);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer?.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden), "WHAT: a member who only reads was allowed a Team Admin key. WHY: a key never claims more than its owner has. HOW: refuse a key role above the owner's role on that unit.");
                Assert.That(answer?.Value, Is.EqualTo(BroaderScopeRefusal), "WHAT: the refusal's message changed. WHY: scripts and people read this message today. HOW: keep the existing message.");
            }
        }

        [Test]
        public async Task Removing_the_rule_stops_the_limited_key_on_its_next_request()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);
            var zenithReport = scenario.AKeyIsStoredFor(Noor, (UserRole.Viewer, PermissionScopeType.Team, TeamZenith));
            var readBeforeRemoval = await scenario.CanReadTeam(TheKey(Noor, zenithReport), TeamZenith);

            await scenario.AlexRemovesTheRulesOf(TeamReaders);

            var readAfterRemoval = await scenario.CanReadTeam(TheKey(Noor, zenithReport), TeamZenith);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(readBeforeRemoval, Is.True, "WHAT: the key could not read Team Zenith while the rule existed. WHY: without that, the removal proves nothing. HOW: cover the key's limit by the owner's reach.");
                Assert.That(readAfterRemoval, Is.False, "WHAT: the key still reads Team Zenith after the rule was removed. WHY: the key's coverage is its owner's reach now, not when it was issued. HOW: check coverage on every request.");
            }
        }

        [Test]
        public async Task A_key_whose_owner_reaches_nothing_reads_nothing()
        {
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);
            var samsKey = scenario.AKeyIsStoredFor(Sam, (UserRole.Viewer, PermissionScopeType.Team, TeamZenith));

            var readsZenith = await scenario.CanReadTeam(TheKey(Sam, samsKey), TeamZenith);

            Assert.That(readsZenith, Is.False, "WHAT: a key limited to Team Zenith reads it although its owner reaches no unit. WHY: a key never exceeds its owner. HOW: cover a key row only where the owner has a role on that unit.");
        }
    }
}
