using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the Refinement tab's list. Every Given that names refinement states puts them
    /// there the way slice 01 does - the admin saves the Team settings - so the list is only ever fed by
    /// the production write, never by a hand-written setting.
    /// </summary>
    public partial class Slice02RefinementListTest
    {
        private const string Grooming = "Grooming";

        private const string Refining = "Refining";

        private const string DoingCategory = "Doing";

        private const string ToDoCategory = "ToDo";

        private const int TeamThatDoesNotExist = 987654;

        private const int ThreeHundred = 300;

        private static readonly TimeSpan TwoSeconds = TimeSpan.FromSeconds(2);

        // --- Given ---

        private async Task<TeamUnderTest> TheAdminHasChosen(TeamUnderTest team, params string[] states)
        {
            TheCallerAdministersTheTeam(team);

            using var save = await SaveTheTeamSettingsChoosing(team, states);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin's choice of refinement states was not saved, so the tab has nothing to list from. {await save.Content.ReadAsStringAsync()}");

            return team;
        }

        /// <summary>
        /// Gravity's tracker ranks GR-040 first, but it is being implemented, not refined; GR-001 is done.
        /// The four in Backlog, Analysing and Next are what refinement is about.
        /// </summary>
        private async Task<TeamUnderTest> GivenGravityRefinesInBacklogAnalysingAndNextAndItsTrackerHoldsWorkInEveryState()
        {
            var gravity = ATeamMappedLikeGravity();

            SeedWorkItems(gravity,
            [
                new TrackerWorkItem("GR-040", "Billing export", Implementation, StateCategories.Doing, "1", StartedDaysAgo: 9),
                new TrackerWorkItem("GR-058", "User activity tracking", Next, StateCategories.Doing, "2", StartedDaysAgo: 1),
                new TrackerWorkItem("GR-059", "Advanced search filters", Next, StateCategories.Doing, "3", StartedDaysAgo: 2),
                new TrackerWorkItem("GR-051", "Advanced reporting module", Analysing, StateCategories.Doing, "4", StartedDaysAgo: 3),
                new TrackerWorkItem("GR-073", "Configuration management", Backlog, StateCategories.ToDo, "5"),
                new TrackerWorkItem("GR-001", "User authentication system", Done, StateCategories.Done, "6"),
            ]);

            return await TheAdminHasChosen(gravity, Backlog, Analysing, Next);
        }

        private async Task<TeamUnderTest> GivenGravityRefinesInBacklogWithTwoWorkItemsRankedEqually()
        {
            var gravity = ATeamMappedLikeGravity();

            SeedWorkItems(gravity,
            [
                new TrackerWorkItem("GR-075", "Error tracking improvements", Backlog, StateCategories.ToDo, "7"),
                new TrackerWorkItem("GR-074", "Load testing framework", Backlog, StateCategories.ToDo, "7"),
            ]);

            return await TheAdminHasChosen(gravity, Backlog);
        }

        private async Task<TeamUnderTest> GivenGravityRefinesInBacklogWithWorkRankedNineAndTen()
        {
            var gravity = ATeamMappedLikeGravity();

            SeedWorkItems(gravity,
            [
                new TrackerWorkItem("GR-090", "Audit trail functionality", Backlog, StateCategories.ToDo, "10"),
                new TrackerWorkItem("GR-091", "Workflow automation tools", Backlog, StateCategories.ToDo, "9"),
            ]);

            return await TheAdminHasChosen(gravity, Backlog);
        }

        private async Task<TeamUnderTest> GivenOrbitRefinesInItsRefiningMappingAndHoldsWorkInBothMappedStates()
        {
            var orbit = SeedTeam(
                "Team Orbit",
                [Backlog],
                [Refining, Implementation],
                [Done],
                [new StateMapping { Name = Refining, States = [Analysing, Grooming] }]);

            SeedWorkItems(orbit,
            [
                new TrackerWorkItem("OR-003", "Telemetry pipeline", Implementation, StateCategories.Doing, "0", StartedDaysAgo: 5),
                new TrackerWorkItem("OR-001", "Orbit planner", Refining, StateCategories.Doing, "2", StartedDaysAgo: 2),
                new TrackerWorkItem("OR-002", "Launch window finder", Refining, StateCategories.Doing, "1", StartedDaysAgo: 1),
            ]);

            return await TheAdminHasChosen(orbit, Refining);
        }

        private async Task<TeamUnderTest> GivenZenithRefinesInBacklogButHoldsNothingThere()
        {
            var zenith = ATeamMappedLikeGravity("Team Zenith");

            SeedWorkItem(zenith, "ZN-010", "Release notes generator", Implementation, StateCategories.Doing, "1", startedDaysAgo: 2);

            return await TheAdminHasChosen(zenith, Backlog);
        }

        private TeamUnderTest GivenZenithWithNoRefinementStatesHoldingBacklogWork()
        {
            var zenith = ATeamMappedLikeGravity("Team Zenith");

            SeedWorkItem(zenith, "ZN-020", "Dark mode", Backlog, StateCategories.ToDo, "1");

            return zenith;
        }

        private async Task<TeamUnderTest> GivenGravityAndPulsarBothHoldWorkInBacklogAndOnlyGravityRefinesThere()
        {
            var gravity = ATeamMappedLikeGravity();
            var pulsar = ATeamMappedLikeGravity("Team Pulsar");

            SeedWorkItem(gravity, "GR-073", "Configuration management", Backlog, StateCategories.ToDo, "2");
            SeedWorkItem(pulsar, "PU-073", "Sprint report", Backlog, StateCategories.ToDo, "1");

            return await TheAdminHasChosen(gravity, Backlog);
        }

        private async Task<TeamUnderTest> GivenGravityRefinesInBacklogAndAnalysingAndHoldsWorkInBoth()
        {
            var gravity = ATeamMappedLikeGravity();

            SeedWorkItems(gravity,
            [
                new TrackerWorkItem("GR-051", "Advanced reporting module", Analysing, StateCategories.Doing, "1", StartedDaysAgo: 3),
                new TrackerWorkItem("GR-052", "Custom dashboards", Analysing, StateCategories.Doing, "2", StartedDaysAgo: 1),
                new TrackerWorkItem("GR-073", "Configuration management", Backlog, StateCategories.ToDo, "3"),
            ]);

            return await TheAdminHasChosen(gravity, Backlog, Analysing);
        }

        /// <summary>
        /// Without the Work Items in Analysing on the list first, their absence afterwards would prove nothing.
        /// </summary>
        private async Task GivenTheCoachSeesTheWorkInAnalysingListed(TeamUnderTest team, params string[] referenceIds)
        {
            TheCallerOnlyReadsTheTeam(team);
            var before = await ReadTheRefinementTab(team);

            Assert.That(RowsIn(before).Select(row => row.ReferenceId), Is.EqualTo(referenceIds),
                $"The Work Items in Analysing were not listed before the admin's save. Answer: {before}");
        }

        /// <summary>
        /// Three hundred Work Items spread over the three refinement states, seeded in the reverse of the
        /// tracker's rank so that a list that merely kept the order they were stored in reads wrong.
        /// </summary>
        private async Task<TeamUnderTest> GivenGravityHoldsThreeHundredWorkItemsInItsRefinementStates()
        {
            var gravity = ATeamMappedLikeGravity();

            SeedWorkItems(gravity, [.. Enumerable.Range(0, ThreeHundred).Select(AWorkItemInRefinement)]);

            return await TheAdminHasChosen(gravity, Backlog, Analysing, Next);
        }

        private static TrackerWorkItem AWorkItemInRefinement(int index)
        {
            var (state, category) = (index % 3) switch
            {
                0 => (Backlog, StateCategories.ToDo),
                1 => (Analysing, StateCategories.Doing),
                _ => (Next, StateCategories.Doing),
            };

            return new TrackerWorkItem(
                $"GR-{1000 + index}",
                $"Refinement candidate {index}",
                state,
                category,
                $"{ThreeHundred - index}",
                StartedDaysAgo: index % 10);
        }

        private void TheCallerAdministersTheWholeInstance() => Client.AsSystemAdmin();

        /// <summary>
        /// Checked first so that the "not found" after it is about who is asking, or which Team, and not
        /// about a tab that answers nobody.
        /// </summary>
        private async Task GivenAReaderOfTheTeamCanOpenItsRefinementTab(TeamUnderTest team)
        {
            TheCallerOnlyReadsTheTeam(team);
            await GivenTheRefinementTabOfAnExistingTeamOpens(team);
        }

        private async Task GivenTheRefinementTabOfAnExistingTeamOpens(TeamUnderTest team)
        {
            using var answer = await AskForTheRefinementTab(team.TeamId);
            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "The Refinement tab does not open even for a caller who may read the Team, so a refusal proves nothing.");
        }

        // --- When ---

        private async Task<JsonElement> WhenTheCoachOpensTheRefinementTab(TeamUnderTest team)
            => await ReadTheRefinementTab(team);

        private async Task<HttpResponseMessage> WhenSomebodyAsksForTheRefinementTabOf(int teamId)
            => await AskForTheRefinementTab(teamId);

        private async Task<TeamUnderTest> WhenTheAdminTakesAnalysingOutOfDoing(TeamUnderTest team)
        {
            TheCallerAdministersTheTeam(team);

            using var save = await SaveTheTeamSettingsWithDoingStates(team, Next, Implementation);
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin could not take {Analysing} out of Doing. {await save.Content.ReadAsStringAsync()}");

            return team with { DoingStates = [Next, Implementation] };
        }

        private async Task<(JsonElement Tab, TimeSpan Elapsed)> WhenTheCoachOpensTheRefinementTabAgainAndItIsTimed(TeamUnderTest team)
        {
            var stopwatch = Stopwatch.StartNew();
            var tab = await ReadTheRefinementTab(team);
            stopwatch.Stop();

            return (tab, stopwatch.Elapsed);
        }

        // --- Then ---

        private static void ThenTheRowsAre(JsonElement tab, params string[] referenceIds)
            => Assert.That(RowsIn(tab).Select(row => row.ReferenceId), Is.EqualTo(referenceIds),
                $"The Refinement tab does not list exactly these Work Items in this order. Answer: {tab}");

        private static void ThenTheRowReads(JsonElement tab, string referenceId, string name, string state, string category)
        {
            var row = TheRowFor(tab, referenceId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Name, Is.EqualTo(name));
                Assert.That(row.Url, Is.EqualTo(TrackerAddressOf(referenceId)),
                    "A row has to take the coach to the Work Item in the tracker.");
                Assert.That(row.State, Is.EqualTo(state));
                Assert.That(row.Category, Is.EqualTo(category));
            }
        }

        private static void ThenTheRowIsThisManyDaysOld(JsonElement tab, string referenceId, int days)
            => Assert.That(TheRowFor(tab, referenceId).WorkItemAge, Is.EqualTo(days));

        private static void ThenTheRowCarriesNoAge(JsonElement tab, string referenceId)
            => Assert.That(TheRowFor(tab, referenceId).WorkItemAge, Is.Null,
                "A To Do Work Item has not started, so it has no Work Item Age to show.");

        private static void ThenTheTabSaysTheTeamHasRefinementStates(JsonElement tab, bool expected)
        {
            bool? said = tab.TryGetProperty("refinementConfigured", out var configured) && configured.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? configured.GetBoolean()
                : null;

            Assert.That(said, Is.EqualTo(expected),
                $"The answer has to say whether the Team has refinement states; silence is not an answer. Answer: {tab}");
        }

        private static void ThenTheSettingsNoLongerName(JsonElement settings, string state)
            => Assert.That(RefinementStatesIn(settings).Select(entry => entry.State), Does.Not.Contain(state),
                $"A state that is no longer To Do or Doing is still among the refinement states. Settings: {settings}");

        private static void ThenNoWorkItemIsListed(JsonElement tab)
            => Assert.That(RowsIn(tab), Is.Empty);

        private static void ThenTheTabIsNotFound(HttpResponseMessage answer)
            => Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        private static void ThenBothAnswersListTheSameRows(JsonElement first, JsonElement second)
        {
            var firstRows = RowsIn(first);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstRows, Is.Not.Empty, "Two empty answers agree with each other whatever the read does.");
                Assert.That(RowsIn(second), Is.EqualTo(firstRows));
            }
        }

        private static void ThenTheSettingsAreUnchanged(JsonElement before, JsonElement after)
            => Assert.That(after.GetRawText(), Is.EqualTo(before.GetRawText()),
                "Opening the Refinement tab changed what the Team settings read back.");

        private void ThenTheTeamStillHolds(TeamUnderTest team, int heldBefore)
            => Assert.That(WorkItemsStoredFor(team), Is.EqualTo(heldBefore));

        private static void ThenThreeHundredRowsAreListedInRankOrder(JsonElement tab)
        {
            var expected = Enumerable.Range(0, ThreeHundred)
                .Select(index => $"GR-{1000 + index}")
                .Reverse()
                .ToList();

            Assert.That(RowsIn(tab).Select(row => row.ReferenceId), Is.EqualTo(expected));
        }

        private static void ThenTheAnswerTookLessThanTwoSeconds(TimeSpan elapsed)
            => Assert.That(elapsed, Is.LessThan(TwoSeconds));

        private static RefinementRowReading TheRowFor(JsonElement tab, string referenceId)
        {
            return RowsIn(tab).FirstOrDefault(candidate => candidate.ReferenceId == referenceId)
                ?? throw new AssertionException($"{referenceId} is not listed on the Refinement tab. Answer: {tab}");
        }
    }
}
