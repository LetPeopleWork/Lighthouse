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
        /// The one project on this site whose permission scheme does NOT grant ADMINISTER_PROJECTS to
        /// every licensed user. Deleting it deletes this test's reason to exist (SPIKE-03, incidental
        /// finding).
        /// </summary>
        private const string RestrictedProjectKey = "SPIKEPRM";

        private const string DueDateField = "duedate";

        private const string TokenEnvironmentVariable = "JiraLighthouseRestrictedIntegrationTestToken";
        private const string UsernameEnvironmentVariable = "JiraLighthouseRestrictedIntegrationTestUsername";
        private const string DefaultRestrictedUsername = "benjamin@letpeople.work";

        private string issueKey = string.Empty;
        private string? apiToken;

        [OneTimeSetUp]
        public async Task CreateScratchIssue()
        {
            apiToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);
            if (string.IsNullOrEmpty(apiToken))
            {
                return;
            }

            using var client = CreateRawClient(apiToken);

            var payload = JsonSerializer.Serialize(new
            {
                fields = new
                {
                    project = new { key = RestrictedProjectKey },
                    summary = $"Lighthouse write-back suppression probe {Guid.NewGuid():N}",
                    issuetype = new { name = "Task" },
                },
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("rest/api/2/issue", content);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            issueKey = document.RootElement.GetProperty("key").GetString()!;
        }

        [OneTimeTearDown]
        public async Task DeleteScratchIssue()
        {
            if (string.IsNullOrEmpty(issueKey) || string.IsNullOrEmpty(apiToken))
            {
                return;
            }

            using var client = CreateRawClient(apiToken);

            try
            {
                using var response = await client.DeleteAsync($"rest/api/2/issue/{issueKey}?deleteSubtasks=true");
                if (!response.IsSuccessStatusCode)
                {
                    TestContext.Progress.WriteLine($"Failed to delete scratch issue {issueKey}: HTTP {(int)response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                TestContext.Progress.WriteLine($"Failed to delete scratch issue {issueKey}: {ex.Message}");
            }

            issueKey = string.Empty;
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

            var dueDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

            var result = await CreateSubject().WriteFieldsToWorkItems(CreateRestrictedConnection(), [
                new WriteBackFieldUpdate { WorkItemId = issueKey, TargetFieldReference = DueDateField, Value = dueDate },
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

            Assert.That(issueKey, Is.Not.Empty, "The scratch issue was not created; the restricted credential cannot write to " + RestrictedProjectKey + ".");
        }

        private async Task<string?> ReadDueDate()
        {
            using var client = CreateRawClient(apiToken!);
            using var response = await client.GetAsync($"rest/api/2/issue/{issueKey}?fields={DueDateField}");
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var value = document.RootElement.GetProperty("fields").GetProperty(DueDateField);

            return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
        }

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
                TestAuthStrategyFactory.CreateRealFactory(new FakeCryptoService()));

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
