using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
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
    /// DISTILL step definitions (Specifications) for Epic #5511 slice 05 — any broken credential says so.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// <c>GET /api/latest/connectionhealth</c>, System-Administrator-guarded, answering a JSON array with
    /// one object per configured connection: <c>connectionId</c>, <c>connectionName</c>,
    /// <c>workTrackingSystem</c>, <c>state</c>, <c>message</c> and <c>observedAt</c>. <c>state</c> renders
    /// as its name — <c>Unknown</c>, <c>Healthy</c>, <c>Unreachable</c>, <c>AuthenticationFailed</c> —
    /// because the browser's own union is strings. <c>message</c> and <c>observedAt</c> are absent for a
    /// connection nothing has been observed about.
    ///
    /// <c>POST /api/latest/connectionhealth/{connectionId}/test</c>, same guard, answering the single row
    /// for that connection, or 404 when no connection has that id.
    /// </summary>
    public partial class Slice05AnyBrokenCredentialSaysSoTest : TaskManagerAcceptanceTest
    {
        private const string HealthRoute = "/api/latest/connectionhealth";
        private const string RefreshLogRoute = "/api/latest/systeminfo/refreshlog";

        /// <summary>
        /// The one stored value this instance is made unable to decrypt. A sentinel rather than a
        /// wholesale fake, so every other connection in the fixture reads its secrets normally and the
        /// scenario that needs an unreadable one is the only one that gets it.
        /// </summary>
        private const string SecretThisInstanceLost = "encrypted-under-a-key-this-instance-no-longer-has";

        /// <summary>
        /// The option key the unreadable secret is stored under. A connection can hold several secrets, so
        /// the message has to name the one that cannot be read rather than say "a credential".
        /// </summary>
        private const string TheSecretFieldName = "ApiToken";

        private TaskCompletionSource theTrackerMayAnswer = null!;

        private readonly record struct SeededConnection(int Id, string Name);

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            services.RemoveAll<ICryptoService>();
            services.AddSingleton<ICryptoService>(_ => new CryptoServiceThatLostOneKey());
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

            var store = Factory.Services.GetRequiredService<IUpdateStatusStore>();
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (store.HasActiveWork() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }
        }

        // --- Given ---

        private SeededConnection GivenAConnectionAuthenticatingWithAToken()
            => NewConnection(secretValue: null);

        private SeededConnection GivenAConnectionWhoseStoredSecretCannotBeRead()
            => NewConnection(secretValue: SecretThisInstanceLost);

        private SeededConnection GivenAnOAuthConnectionWhoseGrantIsBroken()
            => NewOAuthConnection(OAuthCredentialStatus.Disconnected);

        private SeededConnection GivenAnOAuthConnectionWhoseGrantIsIntact()
            => NewOAuthConnection(OAuthCredentialStatus.Valid);

        private SeededTeam GivenATeamOn(SeededConnection connection)
        {
            var name = $"Team {Guid.NewGuid():N}";
            return new SeededTeam(SeedTeam(connection.Id, name), name);
        }

        /// <summary>
        /// A tracker that turns the refresh away and, asked to validate, says which of the two things went
        /// wrong. Three of the five connectors already answer this way; the refresh itself throws untyped,
        /// which is exactly why the verdict has to come from asking rather than from catching.
        /// </summary>
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

        /// <summary>
        /// Linear and CSV in one setup: the refresh failed, and the connector has no code for "the
        /// credential was refused" to answer with. Everything it can say is <c>validation_failed</c>.
        /// </summary>
        private void GivenTheTrackerFailsWithoutSayingWhy()
        {
            TheTrackerIsUnreachable(new InvalidOperationException("Something went wrong"));

            ConnectorMock
                .Setup(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()))
                .ReturnsAsync(ConnectionValidationResult.Failure(
                    "validation_failed",
                    "Could not validate the connection with the provided settings.",
                    "Something went wrong"));
        }

        private void GivenTheTrackerAnswersNormally()
        {
            ConnectorMock.Setup(c => c.GetWorkItemsForTeam(It.IsAny<Team>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            ConnectorMock.Setup(c => c.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            ConnectorMock
                .Setup(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()))
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

        // --- When ---

        private Task WhenTheScheduledRefreshOfThatTeamRuns(SeededTeam team)
            => TheTeamRefreshRuns(team.Id);

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
            // cancellation token, so the cancel is what frees it — releasing the gate as well makes this a
            // race between a cancel that landed and a refresh that simply finished. That race was
            // invisible while a completed refresh left the connection Unknown, exactly as a cancelled one
            // did; slice 08's D19 gives a completed refresh a verdict, and the two outcomes stopped
            // agreeing. The teardown still releases it, so a scenario that never cancels cannot hang.
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

        private JsonElement whatTheTestAnswered;

        // --- Then ---

        /// <summary>
        /// What the button itself answered, rather than what the list says afterwards. The popover writes
        /// this straight into the row it was pressed from, so a test that only re-read the list would let
        /// the two disagree — which is the failure the whole slice exists to stop.
        /// </summary>
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

        private async Task ThenThatConnectionIsExplainedWithAReconnect(SeededConnection connection)
        {
            var row = await TheHealthRowFor(connection);

            Assert.That(Text(row, "message"), Does.Contain("reconnect").IgnoreCase,
                "An OAuth connection that lost its grant is fixed by reconnecting it, not by re-entering a "
                + "token there is no field for. That is the wording the icon this slice deletes already used, "
                + "and it has to survive the deletion. The row said: " + row);
        }

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
                Assert.That(message, Does.Not.Contain("Jira").IgnoreCase,
                    "The tracker was never asked, so blaming it by name would send an administrator to the "
                    + $"one place that has nothing wrong with it. The row said: {row}");
            }
        }

        private async Task ThenTheHealthListNamesExactly(params SeededConnection[] connections)
        {
            var rows = await TheHealthList();
            var named = rows.Select(row => Text(row, "connectionName")).ToList();

            Assert.That(named, Is.EquivalentTo(connections.Select(c => c.Name).ToList()),
                "A connection with no OAuth credential row is precisely the one the aggregator this slice "
                + $"replaces could not see. The list said: {Describe(rows)}");
        }

        private void ThenTheTrackerWasNeverAsked()
        {
            ConnectorMock.Verify(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()), Times.Never,
                "A secret this instance cannot decrypt is refused by the tracker like any other bad credential, "
                + "and an administrator reads that as an expired token and reissues one that was never the "
                + "problem. The question has to be answered before the request leaves the machine.");
        }

        private void ThenTheTrackerWasAskedExactlyOnce()
        {
            ConnectorMock.Verify(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()), Times.Once,
                "Test connection is one outbound check for one connection, on demand. More than one call is a "
                + "probe loop wearing a button.");
        }

        private async Task ThenTestingAConnectionThatDoesNotExistIsRefused()
        {
            using var client = Factory.CreateClient();
            using var response = await client.PostAsync(TestRouteFor(404_404), null);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                "Answering for a connection that is not there would put a row in the popover for something an "
                + "administrator cannot open.");
        }

        private async Task ThenBothConnectionHealthRoutesRefuseANonAdministratorTheWayTheRefreshLogDoes()
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
            using var read = await client.GetAsync(new Uri(HealthRoute, UriKind.Relative));
            using var test = await client.PostAsync(TestRouteFor(1), null);
            using var refreshLog = await client.GetAsync(new Uri(RefreshLogRoute, UriKind.Relative));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.StatusCode, Is.EqualTo(refreshLog.StatusCode),
                    "Connection health names every connection on the instance, which is the same instance-wide "
                    + "operational detail the refresh log is administrator-only for.");
                Assert.That(test.StatusCode, Is.EqualTo(refreshLog.StatusCode),
                    "A read somebody may not perform must not be performable by asking for it to be re-run.");
            }
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

        private SeededConnection NewOAuthConnection(OAuthCredentialStatus status)
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
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            credentials.Save().GetAwaiter().GetResult();

            return connection;
        }

        /// <summary>
        /// An instance that has lost exactly one encryption key: the sentinel value reads back as
        /// unreadable and everything else behaves as stored. Modelling it as a total loss would make every
        /// connection in the fixture unreadable and the scenario would pass on a build that never looked.
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
    }
}
