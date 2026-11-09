using static Lighthouse.Backend.Tests.Acceptance.SystemWideAccess.SystemWideAccessScenario;

namespace Lighthouse.Backend.Tests.Acceptance.SystemWideAccess
{
    /// <summary>
    /// L7: reading a list through Viewer on all costs the same number of database commands for 200 Teams as for 2, the
    /// summary the browser loads included, so an organisation with many Teams does not pay a lookup per Team.
    /// </summary>
    [TestFixture]
    public class ViewerOnAllListCostTest
    {
        [Test]
        public async Task A_members_Team_list_and_summary_cost_the_same_for_200_Teams_as_for_2()
        {
            var (fewCommands, fewListed, fewTeams) = await TheMembersCostWith(2);
            var (manyCommands, manyListed, manyTeams) = await TheMembersCostWith(200);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fewListed, Is.EquivalentTo(fewTeams), "WHAT: the member's list of 2 Teams is incomplete. WHY: the cost only counts when the list is right. HOW: make Viewer on all reach every Team.");
                Assert.That(manyListed, Is.EquivalentTo(manyTeams), "WHAT: the member's list of 200 Teams is incomplete. WHY: the cost only counts when the list is right. HOW: make Viewer on all reach every Team.");
                Assert.That(manyCommands, Is.EqualTo(fewCommands), $"WHAT: the list and summary sent {manyCommands} commands for 200 Teams against {fewCommands} for 2. WHY: a lookup per Team makes large organisations slow. HOW: resolve access once per request and answer each Team from it.");
            }
        }

        private static async Task<(int Commands, IReadOnlyList<int> Listed, IReadOnlyList<int> Teams)> TheMembersCostWith(int teamCount)
        {
            using var scenario = new SystemWideAccessScenario();
            var teams = scenario.TeamsAreStored(teamCount);
            await scenario.AlexAddsViewerOnAllFor(TeamReaders);

            IReadOnlyList<int> listed = [];
            var commands = scenario.Commands.During(async () =>
            {
                listed = await scenario.TheTeamsListed(Noor_SignedIn(), [.. teams]);
                await scenario.TheSummaryOf(Noor_SignedIn());
            });

            return (commands, listed, teams);
        }
    }
}
