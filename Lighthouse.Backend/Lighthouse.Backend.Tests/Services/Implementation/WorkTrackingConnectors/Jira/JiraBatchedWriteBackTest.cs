using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// DISTILL specifications for Epic 5500 slice 02 (Story #5503, ADR-143) — one call per issue instead
    /// of one per field, with an unbatched retry so a single bad mapping cannot take the whole item down.
    ///
    /// These pin the *shape* of what goes over the wire, through a stubbed transport. That Jira really
    /// does reject a mixed-validity batch whole — the fact the whole retry exists for — is not something a
    /// stub can establish; it is measured in the live fixture `JiraWriteBackTest`.
    /// </summary>
    [TestFixture]
    [Category("epic-5500-quiet-writeback")]
    [Category("slice-02")]
    public class JiraBatchedWriteBackTest
    {
        private const string RedScaffold = "RED — Epic 5500 slice 02 (batched write-back) not implemented";

        private const string AgeField = "customfield_10206";
        private const string DateField = "customfield_10205";

        private static int connectionIdSeed = 7000;

        private List<RecordedPut> recordedPuts = null!;

        private sealed record RecordedPut(string IssueKey, Dictionary<string, JsonElement> Fields, string RawBody);

        [SetUp]
        public void Setup()
        {
            recordedPuts = [];
        }

        // AC-05.1
        [Test]
        public async Task WriteFieldsToWorkItems_ThreeFieldsOnOneIssue_IssuesOnePutCarryingAllThree()
        {
            var subject = CreateSubject(AcceptEverything());

            await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-1", DateField, "2026-08-20"),
                Update("PROJ-1", "description", "hello"),
            ]);

            Assert.That(recordedPuts, Has.Count.EqualTo(1),
                "Three changed fields on one issue are one conversation with Jira, not three. Recorded: "
                + string.Join(" | ", recordedPuts.Select(p => $"{p.IssueKey}=>[{p.RawBody}]")));
            Assert.That(recordedPuts[0].Fields.Keys, Is.EquivalentTo(new[] { AgeField, DateField, "description" }));
        }

        // AC-05.5 — parity: the single-field case must not grow a second call or lose its result.
        [Test]
        public async Task WriteFieldsToWorkItems_OneFieldOnOneIssue_IsUnchangedFromBefore()
        {
            var subject = CreateSubject(AcceptEverything());

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [Update("PROJ-1", AgeField, "5")]);

            Assert.That(recordedPuts, Has.Count.EqualTo(1));
            Assert.That(result.ItemResults, Has.Count.EqualTo(1));
            Assert.That(result.AllSucceeded, Is.True);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_FieldsOnTwoIssues_IssuesOnePutPerIssue()
        {
            var subject = CreateSubject(AcceptEverything());

            await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-2", AgeField, "8"),
                Update("PROJ-1", DateField, "2026-08-20"),
            ]);

            Assert.That(recordedPuts.Select(p => p.IssueKey), Is.EquivalentTo(new[] { "PROJ-1", "PROJ-2" }),
                "Grouping is per issue — two issues are two calls however the flat list was ordered.");
        }

        // AC-05.7 — the numeric-vs-string coercion is per field, so it has to survive inside a batch.
        [Test]
        public async Task WriteFieldsToWorkItems_NumericAndTextInOneBatch_CoercesEachFieldOnItsOwn()
        {
            var subject = CreateSubject(AcceptEverything());

            await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-1", DateField, "2026-08-20"),
            ]);

            var fields = recordedPuts.Single().Fields;
            Assert.Multiple(() =>
            {
                Assert.That(fields[AgeField].ValueKind, Is.EqualTo(JsonValueKind.Number),
                    "A value that parses as a number goes over the wire as a number, batched or not.");
                Assert.That(fields[DateField].ValueKind, Is.EqualTo(JsonValueKind.String));
            });
        }

        // AC-05.8 — the regression batching would otherwise introduce.
        [Test]
        public async Task WriteFieldsToWorkItems_BatchRejected_ResendsEachFieldOnItsOwn()
        {
            var subject = CreateSubject(RejectBatchesContaining("customfield_99999"));

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-1", "customfield_99999", "nonsense"),
                Update("PROJ-1", DateField, "2026-08-20"),
            ]);

            Assert.That(recordedPuts, Has.Count.EqualTo(4),
                "One batch that was rejected, then one call per field: 1 + N, and only on the failure path.");

            var byField = result.ItemResults.ToDictionary(r => r.TargetFieldReference, r => r.Success);
            Assert.Multiple(() =>
            {
                Assert.That(byField[AgeField], Is.True, "A valid field must not be lost to a sibling's mistake.");
                Assert.That(byField[DateField], Is.True);
                Assert.That(byField["customfield_99999"], Is.False, "The offending field fails alone.");
            });
        }

        [Test]
        public async Task WriteFieldsToWorkItems_BatchAccepted_NeverResends()
        {
            var subject = CreateSubject(AcceptEverything());

            await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-1", DateField, "2026-08-20"),
            ]);

            Assert.That(recordedPuts, Has.Count.EqualTo(1),
                "The retry is a failure path. A happy path that pays for it has lost the point of the slice.");
        }

        // AC-05.4 — when even the unbatched retry cannot land a field, nothing is reported as written.
        [Test]
        public async Task WriteFieldsToWorkItems_EveryFieldRefused_ReportsEveryFieldFailed()
        {
            var subject = CreateSubject(RejectEverything());

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-1", DateField, "2026-08-20"),
            ]);

            Assert.That(result.ItemResults, Has.Count.EqualTo(2));
            Assert.That(result.ItemResults.Any(r => r.Success), Is.False,
                "Never a silent partial success — the caller is told about every field it asked for.");
        }

        // A lone field is already isolated — retrying it would just repeat the same rejection.
        [Test]
        public async Task WriteFieldsToWorkItems_TheOnlyFieldIsRefused_DoesNotRetryIt()
        {
            var subject = CreateSubject(RejectEverything());

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [Update("PROJ-1", AgeField, "5")]);

            Assert.That(recordedPuts, Has.Count.EqualTo(1),
                "There is nothing to isolate a single field from, so the retry would only cost a second call.");
            Assert.That(result.ItemResults.Single().Success, Is.False);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_JiraRefuses_ReportsTheStatusItRefusedWith()
        {
            var subject = CreateSubject(RejectEverything());

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [Update("PROJ-1", AgeField, "5")]);

            Assert.That(result.ItemResults.Single().ErrorMessage, Does.Contain("400"),
                "The status is the whole diagnostic — an admin reading the log needs to tell a rejected field from an unreachable Jira.");
        }

        [Test]
        public async Task WriteFieldsToWorkItems_TheTransportThrows_ReportsTheFailureRatherThanPropagating()
        {
            var subject = CreateSubject(_ => throw new HttpRequestException("Jira is unreachable"));

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [Update("PROJ-1", AgeField, "5")]);

            Assert.Multiple(() =>
            {
                Assert.That(result.ItemResults.Single().Success, Is.False);
                Assert.That(result.ItemResults.Single().ErrorMessage, Does.Contain("unreachable"));
            });
        }

        // --- Transport stubs ---

        private Func<HttpRequestMessage, HttpResponseMessage> AcceptEverything()
            => _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        /// <summary>
        /// Mimics what the spike measured: Jira rejects a mixed-validity `fields` object **whole**, and
        /// the same fields sent alone then apply.
        /// </summary>
        private static Func<HttpRequestMessage, HttpResponseMessage> RejectBatchesContaining(string poisonField)
            => request =>
            {
                var fields = ReadFields(request);

                if (!fields.ContainsKey(poisonField))
                {
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        $"{{\"errors\":{{\"{poisonField}\":\"Field cannot be set. It is not on the appropriate screen, or unknown.\"}}}}",
                        Encoding.UTF8,
                        "application/json"),
                };
            };

        private static Func<HttpRequestMessage, HttpResponseMessage> RejectEverything()
            => _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"errors\":{}}", Encoding.UTF8, "application/json"),
            };

        private static Dictionary<string, JsonElement> ReadFields(HttpRequestMessage request)
        {
            var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "{}";
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty("fields", out var fields))
            {
                return [];
            }

            return fields.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }

        private JiraWorkTrackingConnector CreateSubject(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    if (request.Method != HttpMethod.Put)
                    {
                        return Task.FromResult(NonWriteResponse(request));
                    }

                    var segments = request.RequestUri!.AbsolutePath.Split('/');
                    var issueKey = segments[^1];
                    var rawBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
                    recordedPuts.Add(new RecordedPut(issueKey, ReadFields(request), rawBody));

                    return Task.FromResult(respond(request));
                });

            var strategyMock = new Mock<IWorkTrackingAuthStrategy>();
            strategyMock
                .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var factoryMock = new Mock<IWorkTrackingAuthStrategyFactory>();
            factoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns(strategyMock.Object);

            return new JiraWorkTrackingConnector(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                factoryMock.Object,
                handlerMock.Object);
        }

        private static HttpResponseMessage NonWriteResponse(HttpRequestMessage request)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            var body = path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal)
                ? "{\"deploymentType\":\"Cloud\"}"
                : path.EndsWith("rest/api/latest/field", StringComparison.Ordinal)
                    ? "[]"
                    : "{}";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static WriteBackFieldUpdate Update(string workItemId, string fieldReference, string value)
            => new() { WorkItemId = workItemId, TargetFieldReference = fieldReference, Value = value };

        private static WorkTrackingSystemConnection CreateConnection()
        {
            var connectionId = Interlocked.Increment(ref connectionIdSeed);

            var connection = new WorkTrackingSystemConnection
            {
                Id = connectionId,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = $"Test Setting {connectionId}",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = $"https://jira-{connectionId}.example.invalid", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "user@example.com", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = "token", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "10", IsSecret = false },
            ]);

            return connection;
        }
    }
}
