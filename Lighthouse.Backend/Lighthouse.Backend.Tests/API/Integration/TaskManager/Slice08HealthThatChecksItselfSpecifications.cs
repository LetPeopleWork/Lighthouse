using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.ConnectionHealth;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.Logging;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.ConnectionHealth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.ConnectionHealth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic #5511 slice 08 — connection health that
    /// persists and checks itself.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// <c>IConnectionHealthService.RefreshStaleVerdictsAsync(CancellationToken)</c> — asks the
    /// connections whose verdict is absent or older than the staleness threshold, and records what they
    /// answered. It claims a connection before asking it, so two instances that both find one stale ask
    /// it once between them (D26). It is the only new method on the port, and it is the only place that
    /// knows what "stale" means, because the threshold is what makes the word <c>Healthy</c> true (D25).
    ///
    /// <c>ConnectionHealthProber</c> — a <c>BackgroundService</c> registered with
    /// <c>AddHostedService</c> and resolvable from the container, whose public
    /// <c>CheckWhatHasNotBeenHeardFromAsync(CancellationToken)</c> does exactly one pass and returns.
    /// The single-pass method exists for the same reason <c>UsageDataForwardingService</c> has one: the
    /// test host runs no background work, and a test that waits out a schedule it cannot see is not a
    /// test. It owns when; it never touches a verdict (D24).
    ///
    /// <c>RecordRefreshSucceededAsync</c> records <c>Healthy</c> rather than removing the row (D19).
    ///
    /// <c>POST /api/latest/connectionhealth/{id}/test</c> answers a verdict for an Azure DevOps
    /// connection instead of a 500 (#6010, US-08C).
    ///
    /// **Supersedes a slice 05 promise.** Slice 05's DISTILL took the decision that "a successful
    /// refresh clears the verdict; it does not record health", and its scenario
    /// <c>A_refresh_that_works_clears_the_failure_before_it_without_claiming_health</c> asserts the
    /// connection returns to <c>Unknown</c>. D19 reverses that. The scenario is rewritten in place
    /// rather than deleted, because the behaviour it guards — a failure recorded before a good refresh
    /// must not outlive it — is still a promise; only the state it lands on changed.
    /// </summary>
    public partial class Slice08HealthThatChecksItselfTest : TaskManagerAcceptanceTest
    {
        private const string HealthRoute = "/api/latest/connectionhealth";

        /// <summary>
        /// The instance's "now". Pinned because every promise in US-08B is about the age of a verdict,
        /// and an age is only assertable if the test decides what the current moment is.
        /// </summary>
        private static readonly DateTimeOffset TheInstantTheInstanceBelievesIn =
            new(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// Comfortably past any threshold D25 can derive from the seeded refresh intervals (2 x 60
        /// minutes). Deliberately not the exact threshold: a scenario that sat on the boundary would
        /// pass or fail on the arithmetic rather than on the behaviour.
        /// </summary>
        private static readonly TimeSpan LongerThanAnyThreshold = TimeSpan.FromHours(24);

        private const string SecretThisInstanceLost = "encrypted-under-a-key-this-instance-no-longer-has";

        private const string TheSecretFieldName = "ApiToken";

        private readonly record struct SeededConnection(int Id, string Name);

        private FakeLighthouseClock theInstanceClock = null!;
        private TaskCompletionSource theTrackerMayAnswer = null!;
        private JsonElement whatTheTestAnswered;
        private int trackerCallsWhenWatchingBegan;
        private bool recordingHealthRefuses;

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            // NUnit builds one fixture instance for the whole class, so anything a scenario sets here
            // outlives it. Left alone, the one scenario that makes recording throw makes it throw for
            // every scenario that runs after it alphabetically — which reds them for a reason that has
            // nothing to do with what they assert.
            recordingHealthRefuses = false;
            trackerCallsWhenWatchingBegan = 0;
            whatTheTestAnswered = default;
            theTrackerMayAnswer = null!;

            theInstanceClock = new FakeLighthouseClock(TheInstantTheInstanceBelievesIn);
            services.RemoveAll<ILighthouseClock>();
            services.AddSingleton<ILighthouseClock>(theInstanceClock);

            services.RemoveAll<ICryptoService>();
            services.AddSingleton<ICryptoService>(_ => new CryptoServiceThatLostOneKey());

            // Wraps the real service rather than replacing it: AC-08A.7 is about what a refresh does when
            // recording throws, and a wholesale fake would also stop every other scenario in this fixture
            // from exercising the code the slice is changing.
            services.AddScoped<ConnectionHealthService>();
            services.RemoveAll<IConnectionHealthService>();
            services.AddScoped<IConnectionHealthService>(provider => new HealthServiceThatCanRefuseToRecord(
                provider.GetRequiredService<ConnectionHealthService>(),
                () => recordingHealthRefuses));
        }

        /// <summary>
        /// Runs before the harness tears the host down, because NUnit unwinds from the derived class
        /// outwards. A scenario that left the tracker gated would otherwise leave a refresh parked in the
        /// queue while the database underneath it is deleted.
        /// </summary>
        [TearDown]
        public async Task LetAnyGatedRefreshFinish()
        {
            theTrackerMayAnswer?.TrySetResult();
            await TheQueueGoesIdle();
        }

        // --- Given: connections ---

        private SeededConnection GivenAConnection() => NewConnection(secretValue: null);

        private SeededConnection GivenAConnectionWhoseStoredSecretCannotBeRead()
            => NewConnection(secretValue: SecretThisInstanceLost);

        private SeededConnection GivenAnOAuthConnectionWhoseGrantIsBroken()
        {
            var connection = NewConnection(secretValue: null);

            using var scope = Factory.Services.CreateScope();
            var credentials = scope.ServiceProvider.GetRequiredService<IRepository<OAuthCredential>>();

            credentials.Add(new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = "token",
                RefreshToken = "refresh",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Status = OAuthCredentialStatus.Disconnected,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            credentials.Save().GetAwaiter().GetResult();

            return connection;
        }

        /// <summary>
        /// A verdict that outlived the process that observed it. Written straight to the row rather than
        /// produced by a probe, because the promise under test is what a *fresh* instance does with a
        /// verdict it did not write — and a probe in this test would be the thing being ruled out.
        /// </summary>
        private SeededConnection GivenAConnectionCheckedRecentlyByAnEarlierProcess()
        {
            var connection = NewConnection(secretValue: null);

            using var scope = Factory.Services.CreateScope();
            var verdicts = scope.ServiceProvider.GetRequiredService<IRepository<ConnectionHealthVerdict>>();

            verdicts.Add(new ConnectionHealthVerdict
            {
                WorkTrackingSystemConnectionId = connection.Id,
                State = ConnectionHealthState.Healthy,
                Code = string.Empty,
                Message = string.Empty,
                ObservedAt = theInstanceClock.Now.UtcDateTime,
            });
            verdicts.Save().GetAwaiter().GetResult();

            return connection;
        }

        private SeededTeam GivenATeamOn(SeededConnection connection)
        {
            var name = $"Team {Guid.NewGuid():N}";
            return new SeededTeam(SeedTeam(connection.Id, name), name);
        }

        // --- Given: the tracker ---

        private void GivenTheTrackerAnswersNormally()
        {
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            ConnectorMock
                .Setup(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()))
                .ReturnsAsync(ConnectionValidationResult.Success());
        }

        private void GivenTheTrackerRejectsTheCredential()
        {
            TheTrackerIsUnreachable(new InvalidOperationException("Jira returned 401 Unauthorized"));

            ConnectorMock
                .Setup(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()))
                .ReturnsAsync(ConnectionValidationResult.Failure(
                    "authentication_failed",
                    "Authentication failed for Jira.",
                    "Jira returned 401 Unauthorized."));
        }

        private void GivenTheTrackerFailsWithoutSayingWhy()
        {
            ConnectorMock
                .Setup(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()))
                .ReturnsAsync(ConnectionValidationResult.Failure(
                    "validation_failed",
                    "Could not validate the connection with the provided settings.",
                    "Something went wrong"));
        }

        /// <summary>
        /// A connector that raises instead of returning — which is what #6010 does today, and the reason
        /// AC-08B.5 exists independently of it.
        /// </summary>
        private void GivenTheTrackerThrowsFor(SeededConnection connection)
        {
            ConnectorMock
                .Setup(c => c.ValidateConnection(It.Is<WorkTrackingSystemConnection>(candidate => candidate.Id == connection.Id)))
                .ThrowsAsync(new ArgumentException("Key Url not found in Work Tracking Options"));
        }

        private void GivenTheTrackerAnswersNormallyFor(SeededConnection connection)
        {
            ConnectorMock
                .Setup(c => c.ValidateConnection(It.Is<WorkTrackingSystemConnection>(candidate => candidate.Id == connection.Id)))
                .ReturnsAsync(ConnectionValidationResult.Success());
        }

        private void GivenTheTrackerNeverAnswers()
        {
            theTrackerMayAnswer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            ConnectorMock
                .Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>()))
                .Returns(async (Team _, CancellationToken stopping) =>
                {
                    await theTrackerMayAnswer.Task.WaitAsync(stopping);
                    return [];
                });
        }

        /// <summary>
        /// Marks the point from which "was the tracker asked?" is counted. The scenarios that assert an
        /// absence all have a legitimate earlier call to establish the precondition, so an absolute count
        /// would answer a different question than the one being asked.
        /// </summary>
        private void GivenTheTrackerIsWatchedFromNowOn()
            => trackerCallsWhenWatchingBegan = TimesTheTrackerWasAsked();

        private void GivenTheThresholdHasPassed()
            => theInstanceClock.SetInstant(TheInstantTheInstanceBelievesIn.Add(LongerThanAnyThreshold));

        private void GivenRecordingHealthThrows() => recordingHealthRefuses = true;

        // --- When ---

        private Task WhenTheScheduledRefreshOfThatTeamRuns(SeededTeam team) => TheTeamRefreshRuns(team.Id);

        private async Task WhenTheRefreshOfThatTeamIsStoppedMidFlight(SeededTeam team)
        {
            var key = new UpdateKey(UpdateType.Team, team.Id);
            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();

            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(team.Id);

            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (!(store.TryGet(key, out var status) && status?.Status == UpdateProgress.InProgress))
            {
                Assert.That(DateTime.UtcNow, Is.LessThan(deadline),
                    "The refresh never started, so there was nothing for the operator to stop.");
                await Task.Delay(20);
            }

            using (var client = Factory.CreateClient())
            {
                using var response = await client.PostAsync(
                    new Uri($"/api/latest/update/tasks/{UpdateType.Team}/{team.Id}/cancel", UriKind.Relative), null);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    $"Cancelling is how this scenario stops the refresh; it answered {(int)response.StatusCode}.");
            }

            // The gate is deliberately NOT released here. The connector is waiting on the update's own
            // cancellation token, so the cancel is what frees it — and releasing the gate as well makes
            // it a race between the cancel and a refresh that simply finished, which this scenario cannot
            // tell apart. Slice 05's copy of this step does release it, and got away with it only because
            // under the old behaviour a completed refresh and a cancelled one both left the connection
            // Unknown. D19 gives a completed refresh a verdict, so the race now has two visible outcomes.
            while (store.HasActiveWork() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }

            Assert.That(store.HasActiveWork(), Is.False,
                "The refresh never stopped, so nothing was cancelled and the scenario proves nothing.");
        }

        private async Task WhenTheAdministratorTestsThatConnection(SeededConnection connection)
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(TestRouteFor(connection.Id), null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Test connection is a driving port for this slice; it answered {(int)response.StatusCode}.");

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            whatTheTestAnswered = document.RootElement.Clone();
        }

        /// <summary>
        /// Entered through the prober the instance registers, not through the health service behind it —
        /// so a prober wired into nothing fails here rather than passing on a direct call to the service.
        ///
        /// What this cannot prove: that the prober is *hosted*. The harness strips every
        /// <c>IHostedService</c> so background work does not run under test, which is also why the usage
        /// data services are registered as themselves alongside their hosted registration and entered
        /// this same way. The one <c>AddHostedService</c> line beside this registration is covered by
        /// review, not by this scenario, and saying so is cheaper than a test that pretends otherwise.
        /// </summary>
        private async Task WhenTheInstanceChecksWhatItHasNotHeardFrom()
        {
            var prober = Factory.Services.GetRequiredService<ConnectionHealthProber>();

            await prober.CheckWhatHasNotBeenHeardFromAsync(CancellationToken.None);
        }

        /// <summary>
        /// Two replicas reaching the same stale connection on the same tick. Run through two service
        /// scopes against the real database, because the claim is a conditional update and a unique
        /// index — database behaviour that a doubled repository cannot exhibit and cannot disprove.
        /// </summary>
        private async Task WhenTwoInstancesCheckAtTheSameMoment()
        {
            using var oneReplica = Factory.Services.CreateScope();
            using var theOther = Factory.Services.CreateScope();

            await Task.WhenAll(
                oneReplica.ServiceProvider.GetRequiredService<IConnectionHealthService>().RefreshStaleVerdictsAsync(CancellationToken.None),
                theOther.ServiceProvider.GetRequiredService<IConnectionHealthService>().RefreshStaleVerdictsAsync(CancellationToken.None));
        }

        // --- Then ---

        private void ThenTheTestItselfAnswered(string state)
        {
            Assert.That(Text(whatTheTestAnswered, "state"), Is.EqualTo(state),
                $"Test connection answers with the row it just recorded. It said: {whatTheTestAnswered}");
        }

        private async Task ThenThatConnectionReads(SeededConnection connection, string state)
        {
            var row = await TheHealthRowFor(connection);

            Assert.That(Text(row, "state"), Is.EqualTo(state),
                $"'{connection.Name}' is the connection this scenario is about. The row said: {row}");
        }

        /// <summary>
        /// A verdict that keeps its state but not its moment is still the defect, one layer down: the
        /// staleness rule reads the moment, so a Healthy whose observed-at never advances goes stale and
        /// gets probed on every tick — the recurring outbound loop D9 refused, arrived at by accident.
        /// </summary>
        private async Task ThenThatConnectionWasObservedNoEarlierThanTheCheckBeforeIt(SeededConnection connection)
        {
            var row = await TheHealthRowFor(connection);
            var observedAt = row.TryGetProperty("observedAt", out var value) && value.ValueKind is JsonValueKind.String
                ? value.GetDateTime()
                : (DateTime?)null;

            Assert.That(observedAt, Is.Not.Null,
                $"A connection something has been observed about carries the moment it was. The row said: {row}");
            Assert.That(observedAt, Is.GreaterThanOrEqualTo(TheInstantTheInstanceBelievesIn.UtcDateTime),
                $"The refresh observed this connection, so its moment is the refresh's. The row said: {row}");
        }

        private int TheStoredVerdictIdFor(SeededConnection connection)
        {
            using var scope = Factory.Services.CreateScope();
            var verdicts = scope.ServiceProvider.GetRequiredService<IRepository<ConnectionHealthVerdict>>();

            var stored = verdicts.GetAll()
                .Where(verdict => verdict.WorkTrackingSystemConnectionId == connection.Id)
                .ToList();

            Assert.That(stored, Has.Count.EqualTo(1),
                $"'{connection.Name}' should carry exactly one verdict before the refresh runs.");

            return stored[0].Id;
        }

        private void ThenTheStoredVerdictIsStillTheSameRow(SeededConnection connection, int theRowThatWasRecorded)
        {
            Assert.That(TheStoredVerdictIdFor(connection), Is.EqualTo(theRowThatWasRecorded),
                "A delete followed by an insert leaves exactly the state this slice wants and is still the bug "
                + "being fixed, so the state cannot be what proves it gone. A new row identity means the "
                + "verdict was removed and replaced rather than updated.");
        }

        private void ThenTheRefreshItselfIsRecordedAsSucceeding(SeededTeam team)
        {
            var recorded = TheRecordedRefreshFor(RefreshType.Team, team.Id);

            Assert.That(recorded, Is.Not.Null, "The refresh ran, so it wrote a row.");
            Assert.That(recorded!.Success, Is.True,
                "The refresh had already done its work and written its row before health was recorded. A "
                + "verdict nobody asked for must not be able to turn a completed refresh into a failed one.");
        }

        private void ThenTheBrowserWasToldTheRefreshCompleted()
        {
            Assert.That(TheBrowserWasTold.Describe(), Does.Contain("Completed"),
                "The operator watching the popover sees the push, not the refresh log. Both have to agree, "
                + $"which is what slice 01 exists for. The browser was told: {TheBrowserWasTold.Describe()}");
        }

        private void ThenTheTrackerWasNotAskedAgain()
        {
            Assert.That(TimesTheTrackerWasAsked(), Is.EqualTo(trackerCallsWhenWatchingBegan),
                "A connection whose answer is still fresh is not asked again. This is the criterion that "
                + "separates the staleness rule from the recurring probe loop D9 refused, and the only one a "
                + "naive 'ask everything on a timer' fails — every other scenario here passes against it.");
        }

        private void ThenTheTrackerWasAskedExactlyOnce()
        {
            Assert.That(TimesTheTrackerWasAsked(), Is.EqualTo(1),
                "Both instances found the connection stale, and the verdict row is the claim — so one of them "
                + "won the right to ask and the other found nothing to do. Two calls means every replica in a "
                + "fleet asks every tracker, which is the cost D20 was taken on not paying.");
        }

        private int TimesTheTrackerWasAsked()
            => ConnectorMock.Invocations.Count(invocation =>
                invocation.Method.Name == nameof(IWorkTrackingConnector.ValidateConnection));

        private async Task ThenThatConnectionIsExplainedAsAKeyThisInstanceLost(SeededConnection connection)
        {
            var row = await TheHealthRowFor(connection);
            var message = Text(row, "message");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(message, Does.Contain(TheSecretFieldName),
                    "Naming the field is what makes the message actionable - a connection can hold several "
                    + $"secrets and only one of them is unreadable. The row said: {row}");
                Assert.That(message, Does.Contain("Enter it again").IgnoreCase,
                    "The fix is to re-enter the credential so it is stored under the key this instance uses "
                    + $"now, and nothing else on this screen says so. The row said: {row}");
            }
        }

        // --- Azure DevOps, through the real connector (#6010) ---

        /// <summary>
        /// The harness replaces the connector factory wholesale, which is right for every other scenario
        /// here and useless for this one: a double cannot reproduce a defect that lives in the Azure
        /// DevOps connector or in the classification above it. This factory keeps the production
        /// connectors and shares the database with the rest of the fixture.
        /// </summary>
        private WebApplicationFactory<Program> TheInstanceWithItsRealConnectors()
            => RootFactory.WithWebHostBuilder(_ => { });

        private SeededConnection GivenARealAzureDevOpsConnectionPointingNowhere()
            => NewAzureDevOpsConnection(url: "https://dev.azure.com/lighthouse-acceptance-no-such-organisation");

        private SeededConnection GivenARealAzureDevOpsConnectionWithNoUrlRecorded()
            => NewAzureDevOpsConnection(url: null);

        private async Task<HttpStatusCode> WhenTheAdministratorTestsThatConnectionForReal(SeededConnection connection)
        {
            using var factory = TheInstanceWithItsRealConnectors();
            using var client = factory.CreateClient();
            using var response = await client.PostAsync(TestRouteFor(connection.Id), null);

            return response.StatusCode;
        }

        private static void ThenTheTestAnsweredRatherThanFailed(HttpStatusCode answer)
        {
            Assert.That(answer, Is.Not.EqualTo(HttpStatusCode.InternalServerError),
                "Bug #6010. Test connection is the one control built for asking whether a credential works, "
                + "and an administrator who presses it learns nothing at all. It is in this slice because the "
                + "prober D20 adds runs the same path — and there its failure is silent, leaving every Azure "
                + "DevOps connection reading Unknown, which is indistinguishable from nobody having asked.");
        }

        // --- Reading the health list ---

        private async Task<JsonElement> TheHealthRowFor(SeededConnection connection)
        {
            var rows = await TheHealthList();
            var matches = rows.Where(row => Number(row, "connectionId") == connection.Id).ToList();

            Assert.That(matches, Has.Count.EqualTo(1),
                $"Expected exactly one row for connection {connection.Id}. Got: {Describe(rows)}");

            return matches[0];
        }

        private async Task<IReadOnlyList<JsonElement>> TheHealthList()
        {
            using var client = Factory.CreateClient();
            using var response = await client.GetAsync(new Uri(HealthRoute, UriKind.Relative));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The health list is the driving port for this slice; {HealthRoute} answered {(int)response.StatusCode}.");

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);

            Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array),
                $"The popover renders a list of connections, so the endpoint answers an array. Got: {body}");

            return [.. document.RootElement.EnumerateArray().Select(element => element.Clone())];
        }

        private static Uri TestRouteFor(int connectionId)
            => new($"{HealthRoute}/{connectionId}/test", UriKind.Relative);

        // --- Seeding ---

        private SeededConnection NewConnection(string? secretValue)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            if (secretValue != null)
            {
                connection.Options.Add(new WorkTrackingSystemConnectionOption
                {
                    Key = TheSecretFieldName,
                    Value = secretValue,
                    IsSecret = true,
                });
            }

            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return new SeededConnection(connection.Id, connection.Name);
        }

        private SeededConnection NewAzureDevOpsConnection(string? url)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Azure DevOps {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            if (url != null)
            {
                connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Azure DevOps Url", Value = url });
            }

            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = "Personal Access Token",
                Value = "not-a-real-token",
                IsSecret = true,
            });

            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return new SeededConnection(connection.Id, connection.Name);
        }

        /// <summary>
        /// An instance that has lost exactly one encryption key: the sentinel value reads back as
        /// unreadable and everything else behaves as stored. Modelling it as a total loss would make every
        /// connection in the fixture unreadable and the scenarios would pass on a build that never looked.
        /// </summary>
        private sealed class CryptoServiceThatLostOneKey : ICryptoService
        {
            public string Decrypt(string cipherText) => cipherText;

            public string Encrypt(string plainText) => plainText;

            public string Encrypt(string plainText, EncryptionKey key) => plainText;

            public SecretReadResult Read(string storedValue)
                => storedValue == SecretThisInstanceLost
                    ? new SecretReadResult(SecretState.Unreadable, null, "retired-key")
                    : new SecretReadResult(SecretState.LegacyPlaintext, storedValue, null);
        }

        /// <summary>
        /// The real service, with one seam: recording the outcome of a refresh can be made to throw. It is
        /// the only way to reach the non-fatal catch in <c>RecordConnectionHealth</c> from outside, and
        /// that catch is what stops a health verdict deciding whether the refresh that produced it
        /// succeeded.
        /// </summary>
        private sealed class HealthServiceThatCanRefuseToRecord(
            IConnectionHealthService real,
            Func<bool> refuses) : IConnectionHealthService
        {
            public Task<IReadOnlyList<ConnectionHealthDto>> GetHealthAsync() => real.GetHealthAsync();

            public Task<ConnectionHealthDto?> TestConnectionAsync(int connectionId) => real.TestConnectionAsync(connectionId);

            public Task RefreshStaleVerdictsAsync(CancellationToken cancellationToken)
                => real.RefreshStaleVerdictsAsync(cancellationToken);

            public Task RecordRefreshSucceededAsync(WorkTrackingSystemConnection connection)
                => refuses()
                    ? throw new InvalidOperationException("The verdict could not be written.")
                    : real.RecordRefreshSucceededAsync(connection);

            public Task RecordRefreshFailedAsync(WorkTrackingSystemConnection connection)
                => refuses()
                    ? throw new InvalidOperationException("The verdict could not be written.")
                    : real.RecordRefreshFailedAsync(connection);
        }
    }
}
