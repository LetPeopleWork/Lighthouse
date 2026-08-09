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
    /// DISTILL specifications for Epic 5500 slice 04 (Story #5505, ADR-142) — Jira write-back asks not to
    /// notify, and never loses the write when it is not allowed to ask.
    ///
    /// The stub pins our branch, not Jira's. That Jira really answers a 403 rather than ignoring the
    /// parameter is SPIKE-03 Q4 (measured on `SPIKEPRM-1`), and the live end of it is
    /// <c>JiraSuppressionRetryIntegrationTest</c>.
    /// </summary>
    [TestFixture]
    [Category("epic-5500-quiet-writeback")]
    [Category("slice-04")]
    public class JiraQuietWriteBackTest
    {
        private const string AgeField = "customfield_10206";
        private const string DateField = "customfield_10205";
        private const string PoisonField = "customfield_99999";

        private const string SuppressionParameter = "notifyUsers=false";

        private static int connectionIdSeed = 7400;

        private List<RecordedPut> recordedPuts = null!;

        private sealed record RecordedPut(string IssueKey, bool AskedForSilence, IReadOnlyCollection<string> Fields);

        [SetUp]
        public void Setup()
        {
            recordedPuts = [];
        }

        // AC-01.1 — the whole slice, in one assertion.
        [Test]
        public async Task WriteFieldsToWorkItems_AnyWrite_AsksJiraNotToNotifyWatchers()
        {
            var subject = CreateSubject(AcceptEverything());

            await subject.WriteFieldsToWorkItems(CreateConnection(), [Update("PROJ-1", AgeField, "5")]);

            Assert.That(recordedPuts.Single().AskedForSilence, Is.True,
                $"Every write-back PUT carries ?{SuppressionParameter}. Without it the epic ships nothing.");
        }

        [Test]
        public async Task WriteFieldsToWorkItems_JiraAccepted_ReportsTheWriteAsSuppressed()
        {
            var subject = CreateSubject(AcceptEverything());

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [Update("PROJ-1", AgeField, "5")]);

            Assert.That(result.ItemResults.Single().NotificationSuppression,
                Is.EqualTo(NotificationSuppression.Suppressed));
        }

        [Test]
        public async Task WriteFieldsToWorkItems_SuppressionForbidden_RetriesTheSamePayloadWithoutTheParameter()
        {
            var subject = CreateSubject(ForbidSilence());

            await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-1", DateField, "2026-08-20"),
            ]);

            Assert.That(recordedPuts, Has.Count.EqualTo(2),
                "One suppressed attempt, one unsuppressed retry — the batch is kept, only the silence is dropped.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recordedPuts[0].AskedForSilence, Is.True);
                Assert.That(recordedPuts[1].AskedForSilence, Is.False);
                Assert.That(recordedPuts[1].Fields, Is.EquivalentTo(recordedPuts[0].Fields),
                    "The retry re-sends the identical payload — a 403 says nothing about the fields.");
            }
        }

        // The anti-regression criterion: a customer who cannot suppress must keep the write-back they
        // have today. Verified live against SPIKEPRM, where the whole update was dropped without this.
        [Test]
        public async Task WriteFieldsToWorkItems_SuppressionForbidden_StillWritesEveryFieldAndSaysItWasNoisy()
        {
            var subject = CreateSubject(ForbidSilence());

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-1", DateField, "2026-08-20"),
            ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.AllSucceeded, Is.True,
                    "Write-back can never regress: a credential that could write yesterday still writes today.");
                Assert.That(result.ItemResults.Select(r => r.NotificationSuppression),
                    Is.All.EqualTo(NotificationSuppression.NotSuppressed));
            }
        }

        // ADR-142 §3 — the retry's outcome discriminates, never the 403 itself.
        [Test]
        public async Task WriteFieldsToWorkItems_ForbiddenEvenWithoutTheParameter_ReportsAPlainFailureAndUnknownSuppression()
        {
            var subject = CreateSubject(ForbidEverything());

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [Update("PROJ-1", AgeField, "5")]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults.Single().Success, Is.False);
                Assert.That(result.ItemResults.Single().NotificationSuppression,
                    Is.EqualTo(NotificationSuppression.Unknown),
                    "A credential that cannot edit the item at all was never a suppression problem — saying so would send the admin to grant the wrong permission.");
                Assert.That(result.ItemResults.Single().ErrorMessage, Does.Contain("403"));
            }
        }

        [Test]
        public async Task WriteFieldsToWorkItems_ForbiddenEvenWithoutTheParameter_RetriesOnlyOnce()
        {
            var subject = CreateSubject(ForbidEverything());

            await subject.WriteFieldsToWorkItems(CreateConnection(), [Update("PROJ-1", AgeField, "5")]);

            Assert.That(recordedPuts, Has.Count.EqualTo(2),
                "Suppressed, then unsuppressed, then stop. A retry loop over a permission failure is a storm, not a fix.");
        }

        // D-A5's two degradations are orthogonal: this one must not trigger the other.
        [Test]
        public async Task WriteFieldsToWorkItems_RejectedForSomeOtherReason_KeepsAskingForSilenceOnEveryRetry()
        {
            var subject = CreateSubject(RejectBatchesContaining(PoisonField));

            await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-1", PoisonField, "nonsense"),
                Update("PROJ-1", DateField, "2026-08-20"),
            ]);

            Assert.That(recordedPuts.Select(p => p.AskedForSilence), Is.All.True,
                "A rejected batch drops the batch, not the silence. Only a 403 drops the silence.");
        }

        [Test]
        public async Task WriteFieldsToWorkItems_ForbiddenBatchWhoseRetryIsRejected_StillIsolatesTheGoodFields()
        {
            var subject = CreateSubject(ForbidSilenceAndRejectFieldWhenAudible(PoisonField));

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("PROJ-1", AgeField, "5"),
                Update("PROJ-1", PoisonField, "nonsense"),
                Update("PROJ-1", DateField, "2026-08-20"),
            ]);

            var byField = result.ItemResults.ToDictionary(r => r.TargetFieldReference);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(byField[AgeField].Success, Is.True, "Both degradations compose — one bad field plus no suppression rights still lands the good ones.");
                Assert.That(byField[DateField].Success, Is.True);
                Assert.That(byField[PoisonField].Success, Is.False);
                Assert.That(byField[AgeField].NotificationSuppression, Is.EqualTo(NotificationSuppression.NotSuppressed));
                Assert.That(byField[PoisonField].NotificationSuppression, Is.EqualTo(NotificationSuppression.Unknown),
                    "The field never landed, so whether it could have landed quietly is unknowable.");
            }
        }

        // The rollup slice 05 will surface is per project, so the fact has to survive per item.
        [Test]
        public async Task WriteFieldsToWorkItems_OneIssueSilencedAnotherNot_ReportsSuppressionPerItem()
        {
            var subject = CreateSubject(ForbidSilenceOn("NOISY-1"));

            var result = await subject.WriteFieldsToWorkItems(CreateConnection(), [
                Update("QUIET-1", AgeField, "5"),
                Update("NOISY-1", AgeField, "8"),
            ]);

            var byItem = result.ItemResults.ToDictionary(r => r.WorkItemId, r => r.NotificationSuppression);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(byItem["QUIET-1"], Is.EqualTo(NotificationSuppression.Suppressed));
                Assert.That(byItem["NOISY-1"], Is.EqualTo(NotificationSuppression.NotSuppressed),
                    "One connection can be quiet in one project and noisy in another — the permission is project-scoped.");
            }
        }

        [Test]
        public async Task WriteFieldsToWorkItems_NothingToWrite_NeverCallsJira()
        {
            var subject = CreateSubject(AcceptEverything());

            await subject.WriteFieldsToWorkItems(CreateConnection(), []);

            Assert.That(recordedPuts, Is.Empty);
        }

        // --- Transport stubs ---

        private static Func<HttpRequestMessage, HttpResponseMessage> AcceptEverything()
            => _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        /// <summary>
        /// SPIKE-03 Q4, measured: a credential without admin / project-admin gets a 403 on the suppressed
        /// PUT and the field update is dropped whole — while the same PUT without the parameter is a 204.
        /// </summary>
        private static Func<HttpRequestMessage, HttpResponseMessage> ForbidSilence()
            => request => AskedForSilence(request) ? SuppressionForbidden() : new HttpResponseMessage(HttpStatusCode.NoContent);

        private static Func<HttpRequestMessage, HttpResponseMessage> ForbidSilenceOn(string issueKey)
            => request => AskedForSilence(request) && IssueKeyOf(request) == issueKey
                ? SuppressionForbidden()
                : new HttpResponseMessage(HttpStatusCode.NoContent);

        /// <summary>A credential that cannot edit the issue at all: the parameter is beside the point.</summary>
        private static Func<HttpRequestMessage, HttpResponseMessage> ForbidEverything()
            => _ => new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("{\"errorMessages\":[\"You do not have permission to edit issues in this project.\"],\"errors\":{}}", Encoding.UTF8, "application/json"),
            };

        private static Func<HttpRequestMessage, HttpResponseMessage> RejectBatchesContaining(string poisonField)
            => request => ReadFields(request).Contains(poisonField) ? FieldRejected(poisonField) : new HttpResponseMessage(HttpStatusCode.NoContent);

        private static Func<HttpRequestMessage, HttpResponseMessage> ForbidSilenceAndRejectFieldWhenAudible(string poisonField)
            => request =>
            {
                if (AskedForSilence(request))
                {
                    return SuppressionForbidden();
                }

                return ReadFields(request).Contains(poisonField) ? FieldRejected(poisonField) : new HttpResponseMessage(HttpStatusCode.NoContent);
            };

        private static HttpResponseMessage SuppressionForbidden()
            => new(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("{\"errorMessages\":[\"To discard the user notification either admin or project admin permissions are required.\"],\"errors\":{}}", Encoding.UTF8, "application/json"),
            };

        private static HttpResponseMessage FieldRejected(string poisonField)
            => new(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    $"{{\"errors\":{{\"{poisonField}\":\"Field cannot be set. It is not on the appropriate screen, or unknown.\"}}}}",
                    Encoding.UTF8,
                    "application/json"),
            };

        private static bool AskedForSilence(HttpRequestMessage request)
            => (request.RequestUri?.Query ?? string.Empty).Contains(SuppressionParameter, StringComparison.OrdinalIgnoreCase);

        private static string IssueKeyOf(HttpRequestMessage request)
            => request.RequestUri!.AbsolutePath.Split('/')[^1];

        private static IReadOnlyCollection<string> ReadFields(HttpRequestMessage request)
        {
            var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "{}";
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty("fields", out var fields))
            {
                return [];
            }

            return [.. fields.EnumerateObject().Select(p => p.Name)];
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

                    recordedPuts.Add(new RecordedPut(IssueKeyOf(request), AskedForSilence(request), ReadFields(request)));

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
