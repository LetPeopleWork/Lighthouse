using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Platform.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Contracts
{
    /// <summary>
    /// What the Cycle Time scatterplot reads on the wire when a Team's process behaviour chart has no limits for the
    /// range: the status as the word NoLimits, a null limit and an empty list of outlier ids. When there are no limits
    /// is asserted at the metrics service instead.
    /// </summary>
    public class CycleTimeOutliersNoLimitsWireContractTest : WireContractTest
    {
        [Test]
        public async Task A_Team_without_limits_answers_NoLimits_with_a_null_limit_and_no_outlier_ids()
        {
            var today = Clock.GetUtcNow().UtcDateTime.Date;
            var rangeStart = today.AddDays(-30);
            var rangeEnd = today.AddDays(-1);
            var teamId = GivenATeamWithoutAnyClosedWorkItem();

            var response = await Client.GetAsync(
                $"/api/latest/teams/{teamId}/metrics/cycleTime/outliers?startDate={rangeStart:yyyy-MM-dd}&endDate={rangeEnd:yyyy-MM-dd}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "WHAT: the Team's outlier route did not answer 200 without limits. WHY: missing limits are an answer, not an error. " +
                "HOW: answer the NoLimits verdict with 200.");
            using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = answer.RootElement;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("NoLimits"),
                    "WHAT: status is not the word NoLimits. WHY: the frontend reads the status as a string to say the limits are missing. HOW: serialise the outlier status as a string.");
                Assert.That(root.GetProperty("upperNaturalProcessLimit").ValueKind, Is.EqualTo(JsonValueKind.Null),
                    "WHAT: upperNaturalProcessLimit is not null. WHY: without limits there is no limit to state, and a number would read as one. HOW: answer null.");
                Assert.That(root.GetProperty("outlierWorkItemIds").ValueKind, Is.EqualTo(JsonValueKind.Array),
                    "WHAT: outlierWorkItemIds is not a list. WHY: the frontend reads it as a list in every status. HOW: answer an empty list, not null.");
                Assert.That(root.GetProperty("outlierWorkItemIds").GetArrayLength(), Is.Zero,
                    "WHAT: outlierWorkItemIds names items without limits. WHY: without limits no Work Item may be set apart. HOW: answer an empty list.");
            }
        }

        private int GivenATeamWithoutAnyClosedWorkItem()
        {
            using (var seedScope = NewScope())
            {
                seedScope.ServiceProvider.GetServices<ISeeder>().OfType<AppSettingSeeder>().Single().Seed().GetAwaiter().GetResult();
            }

            using var scope = NewScope();
            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
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
                DoneStates = ["Done"],
            };
            teams.Add(team);
            teams.Save().GetAwaiter().GetResult();
            return team.Id;
        }
    }
}
