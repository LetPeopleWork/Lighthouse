using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic #5511 slice 02 — see what is running.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// <c>GET /api/latest/update/tasks</c>, System-Administrator-guarded, answering a JSON array with one
    /// object per admitted piece of work: <c>updateType</c>, <c>id</c>, <c>name</c>, <c>status</c> and
    /// <c>waitingBehind</c>. Enums render as their names, because the browser's own union is strings.
    /// <c>name</c> is resolved on the read path and falls back to something descriptive when the entity
    /// has gone. <c>waitingBehind</c> names the entity holding the lane, and is absent for work that is
    /// running.
    /// </summary>
    public partial class Slice02SeeWhatIsRunningTest : TaskManagerAcceptanceTest
    {
        private const string TaskListRoute = "/api/latest/update/tasks";

        private const string RefreshLogRoute = "/api/latest/systeminfo/refreshlog";

        private TaskCompletionSource theTrackerMayAnswer = null!;

        private readonly List<UpdateKey> admittedByHand = [];

        private readonly record struct SeededTeam(int Id, string Name);

        private readonly record struct SeededPortfolio(int Id, string Name);

        private readonly record struct ExpectedRow(string UpdateType, int Id, string Name, string Status);

        /// <summary>
        /// Runs before the harness tears the host down, because NUnit unwinds from the derived class
        /// outwards. A scenario that left the tracker gated would otherwise leave a refresh parked in the
        /// queue while the database underneath it is deleted.
        /// </summary>
        [TearDown]
        public async Task LetAnyGatedRefreshFinish()
        {
            theTrackerMayAnswer?.TrySetResult();

            // Work admitted by hand never runs, so nothing will ever take it back out of the store and the
            // wait below would sit out its whole deadline for a key that was only ever a fixture.
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            foreach (var key in admittedByHand)
            {
                store.Remove(key);
            }

            admittedByHand.Clear();

            await TheQueueGoesIdle();
        }

        // --- Given ---

        private SeededTeam GivenATeamThatIsRefreshedOnSchedule()
        {
            var teamName = $"Team {Guid.NewGuid():N}";
            return new SeededTeam(SeedTeam(SeedConnection(), teamName), teamName);
        }

        private SeededPortfolio GivenAPortfolioThatIsRefreshedOnSchedule()
        {
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";
            return new SeededPortfolio(SeedPortfolio(SeedConnection(), portfolioName), portfolioName);
        }

        /// <summary>
        /// A refresh that is genuinely in flight. Nothing about "what is running right now" can be
        /// observed against work that has already finished, and the queue runs one thing at a time, so a
        /// held fetch is also what puts a second refresh honestly in the queue behind it.
        /// </summary>
        private void GivenTheTrackerDoesNotAnswerUntilWeSaySo()
        {
            theTrackerMayAnswer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>()))
                .Returns(async () =>
                {
                    await theTrackerMayAnswer.Task;
                    return [];
                });

            ConnectorMock
                .Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>()))
                .Returns(async () =>
                {
                    await theTrackerMayAnswer.Task;
                    return [];
                });
        }

        private void GivenThatTeamIsDeletedWhileItsRefreshIsStillRunning(SeededTeam team)
        {
            using var scope = Factory.Services.CreateScope();
            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teams.Remove(team.Id);
            teams.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Admits work straight through the port, without the queue and without this controller ever
        /// having seen it. Under Redis with more than one replica that is exactly the shape of what
        /// another pod admitted, and the endpoint being replaced cannot see it at all.
        /// </summary>
        private void GivenWorkIsAdmittedThroughTheStoreWithoutThisControllerEverSeeingIt(SeededTeam team)
            => AdmitDirectly(UpdateType.Team, team.Id);

        private void GivenADeleteOfThatTeamIsAdmitted(SeededTeam team)
            => AdmitDirectly(UpdateType.TeamDelete, team.Id);

        private void AdmitDirectly(UpdateType updateType, int id)
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var key = new UpdateKey(updateType, id);

            store.TryAdmit(key, new UpdateStatus { UpdateType = updateType, Id = id, Status = UpdateProgress.Queued });
            admittedByHand.Add(key);
        }

        // --- When ---

        private Task WhenARefreshOfThatTeamIsUnderWay(SeededTeam team)
            => StartRefreshAndWaitUntil(
                sp => sp.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id),
                new UpdateKey(UpdateType.Team, team.Id),
                UpdateProgress.InProgress);

        private Task WhenARefreshOfThatPortfolioIsUnderWay(SeededPortfolio portfolio)
            => StartRefreshAndWaitUntil(
                sp => sp.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolio.Id),
                new UpdateKey(UpdateType.Features, portfolio.Id),
                UpdateProgress.InProgress);

        private Task WhenARefreshOfThatTeamIsAlsoAskedFor(SeededTeam team)
            => StartRefreshAndWaitUntil(
                sp => sp.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id),
                new UpdateKey(UpdateType.Team, team.Id),
                UpdateProgress.Queued);

        private async Task StartRefreshAndWaitUntil(Action<IServiceProvider> trigger, UpdateKey key, UpdateProgress reached)
        {
            trigger(Factory.Services);

            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                if (store.TryGet(key, out var status) && status?.Status == reached)
                {
                    return;
                }

                await Task.Delay(20);
            }

            Assert.Fail($"{key.UpdateType} {key.Id} never reached {reached}; the scenario cannot ask what is running.");
        }

        private async Task TheQueueGoesIdle()
        {
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (store.HasActiveWork() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }
        }

        // --- Then ---

        private static ExpectedRow OneRunningTeamNamed(SeededTeam team)
            => new(nameof(UpdateType.Team), team.Id, team.Name, nameof(UpdateProgress.InProgress));

        private static ExpectedRow OneQueuedTeamNamed(SeededTeam team)
            => new(nameof(UpdateType.Team), team.Id, team.Name, nameof(UpdateProgress.Queued));

        private static ExpectedRow OneRunningPortfolioNamed(SeededPortfolio portfolio)
            => new(nameof(UpdateType.Features), portfolio.Id, portfolio.Name, nameof(UpdateProgress.InProgress));

        private async Task ThenTheTaskListShowsExactly(ExpectedRow expected)
        {
            var rows = await TheTaskList();

            Assert.That(rows, Has.Count.EqualTo(1),
                $"One piece of work is in flight, so the operator should be reading one row. Got: {Describe(rows)}");

            AssertRowMatches(rows[0], expected);
        }

        private async Task ThenTheTaskListShows(params ExpectedRow[] expected)
        {
            var rows = await TheTaskList();

            Assert.That(rows, Has.Count.EqualTo(expected.Length),
                $"The list has to account for everything admitted, not only what is running. Got: {Describe(rows)}");

            foreach (var row in expected)
            {
                var matches = rows
                    .Where(candidate => Text(candidate, "updateType") == row.UpdateType && Number(candidate, "id") == row.Id)
                    .ToList();

                Assert.That(matches, Has.Count.EqualTo(1),
                    $"Expected exactly one row for {row.UpdateType} {row.Id}. Got: {Describe(rows)}");

                AssertRowMatches(matches[0], row);
            }
        }

        private static void AssertRowMatches(JsonElement row, ExpectedRow expected)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Text(row, "updateType"), Is.EqualTo(expected.UpdateType),
                    "A reader has to be able to tell a Team refresh from a Portfolio one.");
                Assert.That(Number(row, "id"), Is.EqualTo(expected.Id));
                Assert.That(Text(row, "name"), Is.EqualTo(expected.Name),
                    "An id is not an answer to 'what is running' - the name is what the operator recognises.");
                Assert.That(Text(row, "status"), Is.EqualTo(expected.Status),
                    "Running and waiting are different answers, and the browser's own union reads the name, not an ordinal.");
            }
        }

        private async Task ThenTheQueuedRowSaysItIsWaitingBehind(SeededTeam waiting, SeededTeam holdingTheLane)
        {
            var row = await TheRowFor(UpdateType.Team, waiting.Id);

            Assert.That(Text(row, "waitingBehind"), Is.EqualTo(holdingTheLane.Name),
                "A row that says only 'queued' is what let a user read three teams stuck behind one portfolio as a hang. "
                + "Naming the lane-holder is the difference between waiting and wedged.");
        }

        private async Task ThenTheRunningRowSaysItIsWaitingBehindNothing(SeededTeam running)
        {
            var row = await TheRowFor(UpdateType.Team, running.Id);

            Assert.That(
                !row.TryGetProperty("waitingBehind", out var behind) || behind.ValueKind is JsonValueKind.Null,
                Is.True,
                "Work that is running is not behind anything, and saying it is would make the list lie in the one "
                + "place it is supposed to be clearest.");
        }

        private async Task ThenTheTaskListStillDescribesThatWorkByTypeAndId(SeededTeam team)
        {
            var row = await TheRowFor(UpdateType.Team, team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Text(row, "updateType"), Is.EqualTo(nameof(UpdateType.Team)),
                    "An entity that has gone mid-refresh must not take the whole list down with it.");
                Assert.That(Text(row, "name"), Is.Not.Null.And.Not.Empty,
                    "A blank row tells an operator nothing; the work still has a type and an id to describe it by.");
                Assert.That(Text(row, "name"), Does.Contain(team.Id.ToString()),
                    "With no name left to resolve, the id is the only thing that still identifies the work.");
            }
        }

        private async Task ThenTheTaskListIsAnsweredAndEmpty()
        {
            var rows = await TheTaskList();

            Assert.That(rows, Is.Empty,
                $"Nothing is running, and an idle instance is an ordinary answer rather than a failure. Got: {Describe(rows)}");
        }

        private async Task ThenTheTaskListDescribesThatWorkWithoutSayingUndefined(SeededTeam team)
        {
            var row = await TheRowFor(UpdateType.TeamDelete, team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Text(row, "updateType"), Is.EqualTo(nameof(UpdateType.TeamDelete)),
                    "Deletes go through the same queue and reach this list; the browser knowing only three of the five "
                    + "update types is what makes them render as nothing at all.");
                Assert.That(Text(row, "name"), Is.Not.Null.And.Not.Empty);
            }
        }

        /// <summary>
        /// AC-02.6 asks for the same refusal the refresh log gives, so the scenario asks both and compares
        /// rather than writing a status code down twice and letting them drift apart.
        /// </summary>
        private async Task ThenTheTaskListRefusesANonAdministratorTheSameWayTheRefreshLogDoes()
        {
            using var factory = RootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var refuses = new Mock<IRbacAdministrationService>();
                    refuses
                        .Setup(s => s.CanSatisfyRequirementAsync(
                            It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                            It.IsAny<Lighthouse.Backend.Models.Authorization.RbacGuardRequirement>(),
                            It.IsAny<int?>(),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

                    services.RemoveAll<IRbacAdministrationService>();
                    services.AddScoped(_ => refuses.Object);
                });
            });

            using var client = factory.CreateClient();
            using var taskList = await client.GetAsync(new Uri(TaskListRoute, UriKind.Relative));
            using var refreshLog = await client.GetAsync(new Uri(RefreshLogRoute, UriKind.Relative));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(taskList.StatusCode, Is.Not.EqualTo(HttpStatusCode.OK),
                    "The task list names every entity on the instance, which is why it is administrator-only.");
                Assert.That(taskList.StatusCode, Is.EqualTo(refreshLog.StatusCode),
                    "Two administrator-only reads on the same instance refusing differently is a difference an operator "
                    + "has to learn for no reason.");
            }
        }

        // --- Reading the list ---

        private async Task<JsonElement> TheRowFor(UpdateType updateType, int id)
        {
            var rows = await TheTaskList();

            var matches = rows
                .Where(candidate => Text(candidate, "updateType") == updateType.ToString() && Number(candidate, "id") == id)
                .ToList();

            Assert.That(matches, Has.Count.EqualTo(1),
                $"Expected exactly one row for {updateType} {id}. Got: {Describe(rows)}");

            return matches[0];
        }

        private async Task<IReadOnlyList<JsonElement>> TheTaskList()
        {
            using var client = Factory.CreateClient();
            using var response = await client.GetAsync(new Uri(TaskListRoute, UriKind.Relative));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The task list is the driving port for this whole slice; {TaskListRoute} answered {(int)response.StatusCode}.");

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);

            Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array),
                $"The popover renders a list of rows, so the endpoint answers an array. Got: {body}");

            return [.. document.RootElement.EnumerateArray().Select(element => element.Clone())];
        }

        private static string? Text(JsonElement row, string property)
            => row.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.String
                ? value.GetString()
                : null;

        private static int? Number(JsonElement row, string property)
            => row.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.Number
                ? value.GetInt32()
                : null;

        private static string Describe(IReadOnlyList<JsonElement> rows)
            => rows.Count == 0 ? "(an empty list)" : string.Join(" | ", rows.Select(row => row.ToString()));
    }
}
