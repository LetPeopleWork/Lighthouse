using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Contracts
{
    /// <summary>
    /// What the Cycle Time scatterplot reads on the wire when it asks a Team for its outliers: the route, the status and
    /// the JSON names of the answer, with the status as a word, the limit as an unrounded number and the outliers as
    /// Work Item ids. Which Work Items are outliers is asserted at the metrics service instead.
    /// </summary>
    public class CycleTimeOutliersWireContractTest : WireContractTest
    {
        /// <summary>Average 20.5, average moving range 25/7: the upper natural process limit is exactly 30 days.</summary>
        private static readonly int[] BaselineWithALimitOf30 = [18, 13, 12, 24, 26, 25, 25, 21];

        private const string Done = "Done";

        [Test]
        public async Task A_Team_answers_its_outliers_with_a_status_word_the_limit_and_the_outlier_Work_Item_ids()
        {
            var today = Clock.GetUtcNow().UtcDateTime.Date;
            var rangeStart = today.AddDays(-30);
            var rangeEnd = today.AddDays(-1);
            var (teamId, shown) = GivenATeamWithItsBaselineAndClosedWorkItems(today, rangeStart, 12, 30, 31, 90);

            var response = await Client.GetAsync(
                $"/api/latest/teams/{teamId}/metrics/cycleTime/outliers?startDate={rangeStart:yyyy-MM-dd}&endDate={rangeEnd:yyyy-MM-dd}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "WHAT: the Team's outlier route did not answer 200. WHY: the scatterplot reads its outliers from GET cycleTime/outliers. " +
                "HOW: add the route beside cycleTime/pbc on the Team metrics controller, under the same read guard.");

            await ThenTheAnswerIs(response, 30.0, shown[31], shown[90]);
        }

        [Test]
        public async Task A_Portfolio_answers_its_outliers_with_a_status_word_the_limit_and_the_outlier_Feature_ids()
        {
            var today = Clock.GetUtcNow().UtcDateTime.Date;
            var rangeStart = today.AddDays(-30);
            var rangeEnd = today.AddDays(-1);
            var (portfolioId, shown) = GivenAPortfolioWithItsBaselineAndClosedFeatures(today, rangeStart, 12, 30, 31, 90);

            var response = await Client.GetAsync(
                $"/api/latest/portfolios/{portfolioId}/metrics/cycleTime/outliers?startDate={rangeStart:yyyy-MM-dd}&endDate={rangeEnd:yyyy-MM-dd}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "WHAT: the Portfolio's outlier route did not answer 200. WHY: the scatterplot of a Portfolio reads its outliers from GET cycleTime/outliers. " +
                "HOW: add the route beside cycleTime/pbc on the Portfolio metrics controller, under the same read guard.");
            await ThenTheAnswerIs(response, 30.0, shown[31], shown[90]);
        }

        private static async Task ThenTheAnswerIs(HttpResponseMessage response, double limit, params int[] outlierIds)
        {
            using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = answer.RootElement;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("Ready"),
                    "WHAT: status is not the word Ready. WHY: the frontend reads the status as a string. HOW: serialise the outlier status as a string.");
                Assert.That(root.GetProperty("upperNaturalProcessLimit").GetDouble(), Is.EqualTo(limit),
                    "WHAT: upperNaturalProcessLimit is missing or not the unrounded limit. WHY: the list title states the limit. HOW: answer the unrounded limit as a number.");
                Assert.That(root.GetProperty("outlierWorkItemIds").EnumerateArray().Select(id => id.GetInt32()), Is.EquivalentTo(outlierIds),
                    "WHAT: outlierWorkItemIds is missing or names other items. WHY: the scatterplot sets apart the items whose ids it lists. HOW: answer the outlier ids as numbers.");
            }
        }

        /// <summary>The metrics services read the refresh settings, which a fresh database only has once the start-up seeder ran.</summary>
        private void GivenTheAppSettingsAsSeeded()
        {
            using var scope = NewScope();
            scope.ServiceProvider.GetServices<ISeeder>().OfType<AppSettingSeeder>().Single().Seed().GetAwaiter().GetResult();
        }

        private (int PortfolioId, Dictionary<int, int> IdByCycleTime) GivenAPortfolioWithItsBaselineAndClosedFeatures(DateTime today, DateTime rangeStart, params int[] shownCycleTimes)
        {
            GivenTheAppSettingsAsSeeded();
            using var scope = NewScope();
            var baselineStart = today.AddDays(-70);
            var portfolio = new Portfolio
            {
                Name = "Ocean Explorer roadmap",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Ocean Explorer roadmap tracker",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
                DoneItemsCutoffDays = 0,
                ToDoStates = ["Backlog"],
                DoingStates = ["Implementation"],
                DoneStates = [Done],
                ProcessBehaviourChartBaselineStartDate = baselineStart,
                ProcessBehaviourChartBaselineEndDate = baselineStart.AddDays(24),
            };
            var baseline = ClosedFeatures(baselineStart.AddDays(1), "B", BaselineWithALimitOf30);
            var shown = ClosedFeatures(rangeStart.AddDays(1), "S", shownCycleTimes);
            portfolio.Features.AddRange(baseline.Concat(shown));

            var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolios.Add(portfolio);
            portfolios.Save().GetAwaiter().GetResult();

            return (portfolio.Id, shownCycleTimes.Zip(shown).ToDictionary(pair => pair.First, pair => pair.Second.Id));
        }

        private static List<Feature> ClosedFeatures(DateTime firstClosedOn, string prefix, int[] cycleTimes)
        {
            var features = new List<Feature>();
            for (var index = 0; index < cycleTimes.Length; index++)
            {
                var closedOn = firstClosedOn.AddDays(index).AddHours(10);
                var startedOn = closedOn.Date.AddDays(1 - cycleTimes[index]).AddHours(9);
                features.Add(new Feature
                {
                    ReferenceId = $"OE-F{prefix}{index}",
                    Name = $"Feature closed after {cycleTimes[index]} days",
                    Type = "Epic",
                    State = Done,
                    StateCategory = StateCategories.Done,
                    CreatedDate = startedOn.AddDays(-1),
                    StartedDate = startedOn,
                    ClosedDate = closedOn,
                    Order = $"{prefix}{index:D3}",
                });
            }

            return features;
        }

        private (int TeamId, Dictionary<int, int> IdByCycleTime) GivenATeamWithItsBaselineAndClosedWorkItems(DateTime today, DateTime rangeStart, params int[] shownCycleTimes)
        {
            GivenTheAppSettingsAsSeeded();
            using var scope = NewScope();
            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var baselineStart = today.AddDays(-70);
            var team = new Team
            {
                Name = "Ocean Explorer",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Ocean Explorer tracker",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
                DoneItemsCutoffDays = 0,
                ToDoStates = ["Backlog"],
                DoingStates = ["Implementation"],
                DoneStates = [Done],
                ProcessBehaviourChartBaselineStartDate = baselineStart,
                ProcessBehaviourChartBaselineEndDate = baselineStart.AddDays(24),
            };
            teams.Add(team);
            teams.Save().GetAwaiter().GetResult();

            var workItems = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
            AddClosedWorkItems(workItems, team.Id, baselineStart.AddDays(1), BaselineWithALimitOf30);
            var shown = AddClosedWorkItems(workItems, team.Id, rangeStart.AddDays(1), shownCycleTimes);
            return (team.Id, shown);
        }

        private static Dictionary<int, int> AddClosedWorkItems(IWorkItemRepository workItems, int teamId, DateTime firstClosedOn, int[] cycleTimes)
        {
            var idByCycleTime = new Dictionary<int, int>();
            for (var index = 0; index < cycleTimes.Length; index++)
            {
                var closedOn = firstClosedOn.AddDays(index).AddHours(10);
                var startedOn = closedOn.Date.AddDays(1 - cycleTimes[index]).AddHours(9);
                var item = new WorkItem
                {
                    TeamId = teamId,
                    ReferenceId = $"OE-{firstClosedOn:MMdd}-{index}",
                    Name = $"Closed after {cycleTimes[index]} days",
                    Type = "Story",
                    State = Done,
                    StateCategory = StateCategories.Done,
                    CreatedDate = startedOn.AddDays(-1),
                    StartedDate = startedOn,
                    ClosedDate = closedOn,
                    Order = $"{index:D3}",
                };
                workItems.Add(item);
                workItems.Save().GetAwaiter().GetResult();
                idByCycleTime[cycleTimes[index]] = item.Id;
            }

            return idByCycleTime;
        }
    }
}
