using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
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

            using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = answer.RootElement;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("Ready"),
                    "WHAT: status is not the word Ready. WHY: the frontend reads the status as a string. HOW: serialise the outlier status as a string.");
                Assert.That(root.GetProperty("upperNaturalProcessLimit").GetDouble(), Is.EqualTo(30.0),
                    "WHAT: upperNaturalProcessLimit is missing or not the unrounded limit. WHY: the list title states the limit. HOW: answer the unrounded limit as a number.");
                Assert.That(root.GetProperty("outlierWorkItemIds").EnumerateArray().Select(id => id.GetInt32()), Is.EquivalentTo(new[] { shown[31], shown[90] }),
                    "WHAT: outlierWorkItemIds is missing or names other Work Items. WHY: the scatterplot sets apart the items whose ids it lists. HOW: answer the outlier Work Item ids as numbers.");
            }
        }

        private (int TeamId, Dictionary<int, int> IdByCycleTime) GivenATeamWithItsBaselineAndClosedWorkItems(DateTime today, DateTime rangeStart, params int[] shownCycleTimes)
        {
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
