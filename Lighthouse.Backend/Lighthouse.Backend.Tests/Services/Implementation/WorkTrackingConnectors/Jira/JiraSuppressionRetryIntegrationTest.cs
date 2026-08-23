using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Epic 5500 slice 04, the half no stub can establish: that Jira really answers a suppressed PUT with
    /// 403 for an under-permissioned credential, and that ADR-142's retry lands the value anyway.
    ///
    /// This needs a SECOND identity — the CI credential is site admin and can always suppress, so it can
    /// never reach the branch. The restricted identity writes into `SPIKEPRM`, the team-managed project
    /// SPIKE-03 created precisely because Jira Free forbids editing permission schemes: every other
    /// project on this site grants ADMINISTER_PROJECTS to any licensed user.
    ///
    /// Without <c>JiraLighthouseRestrictedIntegrationTestToken</c> the fixture ignores itself rather than
    /// failing — the same shape as <c>JiraWriteBackTest</c> on fork PRs.
    /// </summary>
    [TestFixture]
    [Category("JiraIntegration")]
    [Category("epic-5500-quiet-writeback")]
    [Category("slice-04")]
    public class JiraSuppressionRetryIntegrationTest
    {
        private const string OrganizationUrl = "https://letpeoplework.atlassian.net";

        /// <summary>
        /// One fixed issue, reused, deliberately — the create-and-delete shape every other Jira fixture
        /// uses is wrong here on both ends. The restricted credential has no Delete Issues in
        /// <c>SPIKEPRM</c> (measured: <c>DELETE_ISSUES=false</c>), so teardown would leak an issue per
        /// run; and this credential cannot suppress notifications *by definition*, so every create would
        /// mail the project lead. Reusing one issue that the acting identity also owns and is assigned to
        /// leaves the actor as the only notification recipient — and Jira does not mail you your own
        /// changes.
        /// </summary>
        private const string DefaultProbeIssueKey = "SPIKEPRM-4";

        private const string DueDateField = "duedate";

        private const string TokenEnvironmentVariable = "JiraLighthouseRestrictedIntegrationTestToken";
        private const string UsernameEnvironmentVariable = "JiraLighthouseRestrictedIntegrationTestUsername";
        private const string IssueEnvironmentVariable = "JiraLighthouseRestrictedIntegrationTestIssue";
        private const string DefaultRestrictedUsername = "benjamin@letpeople.work";

        private string? apiToken;
        private string? accountId;

        [OneTimeSetUp]
        public async Task EnsureTheProbeIssueIsOwnedByTheActingIdentity()
        {
            apiToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);
            if (string.IsNullOrEmpty(apiToken))
            {
                return;
            }

            using var client = CreateRawClient(apiToken);

            accountId = await ReadOwnAccountId(client);

            // Idempotent, and the reason the fixture is quiet: assignee, reporter and the only watcher
            // are all the acting identity, so no notification has anyone else to reach. A run that finds
            // the issue reassigned puts it back rather than mailing whoever it was handed to.
            var payload = JsonSerializer.Serialize(new { accountId });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.PutAsync($"rest/api/2/issue/{ProbeIssueKey}/assignee", content);

            if (!response.IsSuccessStatusCode)
            {
                TestContext.Progress.WriteLine(
                    $"Could not self-assign {ProbeIssueKey}: HTTP {(int)response.StatusCode}. The write below may notify.");
            }
        }

        /// <summary>
        /// The anti-regression criterion, measured: without the retry this credential's write is dropped
        /// whole (SPIKE-03 Q4 — `SPIKEPRM-1.duedate` stayed null).
        /// </summary>
        [Test]
        [Category("Integration")]
        public async Task WriteFieldsToWorkItems_CredentialCannotSuppress_WritesTheValueAnywayAndReportsItNoisy()
        {
            SkipWithoutRestrictedCredential();

            var dueDate = await ADueDateDifferentFromTheStoredOne();

            var result = await CreateSubject().WriteFieldsToWorkItems(CreateRestrictedConnection(), [
                new WriteBackFieldUpdate { WorkItemId = ProbeIssueKey, TargetFieldReference = DueDateField, Value = dueDate },
            ]);

            var written = await ReadDueDate();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.AllSucceeded, Is.True, "The retry is what keeps this customer's write-back working at all.");
                Assert.That(result.ItemResults.Single().NotificationSuppression, Is.EqualTo(NotificationSuppression.NotSuppressed));
                Assert.That(written, Is.EqualTo(dueDate), "Reported success without the value in Jira would be the worst of both.");
            }
        }

        private void SkipWithoutRestrictedCredential()
        {
            if (string.IsNullOrEmpty(apiToken))
            {
                Assert.Ignore($"{TokenEnvironmentVariable} is not set — the under-permissioned identity is what this fixture is for.");
            }
        }

        /// <summary>
        /// A value the issue does not already hold: writing what is already there would make the
        /// read-back assertion pass without Jira having accepted anything.
        /// </summary>
        private async Task<string> ADueDateDifferentFromTheStoredOne()
        {
            var stored = await ReadDueDate();

            var candidate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

            return candidate == stored
                ? DateTime.UtcNow.AddDays(31).ToString("yyyy-MM-dd")
                : candidate;
        }

        private async Task<string?> ReadDueDate()
        {
            using var client = CreateRawClient(apiToken!);
            using var response = await client.GetAsync($"rest/api/2/issue/{ProbeIssueKey}?fields={DueDateField}");
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var value = document.RootElement.GetProperty("fields").GetProperty(DueDateField);

            return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
        }

        private static async Task<string> ReadOwnAccountId(HttpClient client)
        {
            using var response = await client.GetAsync("rest/api/2/myself");
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return document.RootElement.GetProperty("accountId").GetString()!;
        }

        private static string ProbeIssueKey
            => Environment.GetEnvironmentVariable(IssueEnvironmentVariable) ?? DefaultProbeIssueKey;

        private static string RestrictedUsername
            => Environment.GetEnvironmentVariable(UsernameEnvironmentVariable) ?? DefaultRestrictedUsername;

        private static HttpClient CreateRawClient(string token)
        {
            var client = new HttpClient { BaseAddress = new Uri($"{OrganizationUrl}/") };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{RestrictedUsername}:{token}")));

            return client;
        }

        private static JiraWorkTrackingConnector CreateSubject()
            => new(new IssueFactory(Mock.Of<ILogger<IssueFactory>>()),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                TestAuthStrategyFactory.CreateRealFactory(new FakeCryptoService()),
                new Lighthouse.Backend.Cache.Cache<string, object>());

        private WorkTrackingSystemConnection CreateRestrictedConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Jira restricted identity",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = OrganizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = RestrictedUsername, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken!, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
            ]);

            return connection;
        }
    }
}
