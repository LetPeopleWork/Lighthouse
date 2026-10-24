using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Every Jira Release sweep writes one line saying how long it took and how much it read, so the
    /// operator who hears "picking a Release is slow" can say whether Jira or Lighthouse is slow.
    ///
    /// Driven through the connector's own entry points with Jira's HTTP answered in process, and the clock
    /// moved on by a fixed step for every request Jira is asked, so the time the line reports is exact.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6398-jira-release-loads-once")]
    [Category("slice-03")]
    [Category("us-03")]
    [Category("kpi-OUT-6398-sweep-cost-visible")]
    public class JiraReleaseSweepLineTest
    {
        private const string JiraReleaseSourceKey = "jira-release";
        private const string TheSweepLine = "Jira Release sweep for the";
        private const string ThePaymentsProject = "PAY";
        private const string TheHrProject = "HR";
        private const string TheDatedRelease = "10004";
        private const string TheUnusableAddressHost = "jira.payments.example.invalid";

        /// <summary>A typo in the connection form: the port is not a number, so no client can be built for it.</summary>
        private const string AnAddressThatCannotBeUsed = $"https://{TheUnusableAddressHost}:not-a-port/";

        private static readonly TimeSpan EachRequestTakes = TimeSpan.FromMilliseconds(125);

        /// <summary>The names the sweep line carries its values under, as a structured log keeps them.</summary>
        private static class Field
        {
            public const string SweepPurpose = "SweepPurpose";
            public const string ElapsedMs = "ElapsedMs";
            public const string Deployment = "Deployment";
            public const string ReleasesKept = "ReleasesKept";
            public const string VersionsRead = "VersionsRead";
            public const string VersionPages = "VersionPages";
            public const string ProjectCount = "ProjectCount";
            public const string ProjectsSkipped = "ProjectsSkipped";
            public const string Complete = "Complete";
        }

        private static readonly string[] TheDatedReleaseOnItsOwn = [TheDatedRelease];

        /// <summary>Every name the fixture gives the customer's Jira, none of which may reach the line.</summary>
        private static readonly string[] WhatNamesTheCustomer =
        [
            ThePaymentsProject, "Payments", TheHrProject, "Human Resources", "PAY 2026.11", "PAY 2026.10", "PAY 2026.09", "PAY 2025.12",
        ];

        private FakeTimeProvider clock = null!;
        private RecordingLogger<JiraWorkTrackingConnector> logger = null!;

        [SetUp]
        public void SetUp()
        {
            clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 24, 8, 5, 0, TimeSpan.Zero));
            logger = new RecordingLogger<JiraWorkTrackingConnector>();
        }

        /// <summary>
        /// Lena's Data Center: PAY holds four versions over two pages, of which two are still to ship; HR holds
        /// one. The version route cannot filter by status there, so all five are read and two are dropped.
        /// </summary>
        private static FakeJiraDataCenter PaymentsOnDataCenter() =>
            new FakeJiraDataCenter { VersionsPerPage = 2 }
                .WithProject(ThePaymentsProject, "Payments")
                .WithProject(TheHrProject, "Human Resources")
                .WithReleaseIn(ThePaymentsProject, TheDatedRelease, "PAY 2026.11", "2026-11-20")
                .WithShippedReleaseIn(ThePaymentsProject, "10003", "PAY 2026.10", "2026-10-15")
                .WithArchivedReleaseIn(ThePaymentsProject, "10002", "PAY 2025.12", "2025-12-01")
                .WithReleaseIn(ThePaymentsProject, "10005", "PAY 2026.09", releaseDate: null)
                .WithReleaseIn(TheHrProject, "10101", "HR onboarding", "2026-12-01");

        [Test]
        [Category("driving_port")]
        public async Task A_picker_sweep_on_Data_Center_writes_one_line_with_its_counts_and_time()
        {
            var jira = PaymentsOnDataCenter();

            await AConnectorOver(jira.Handler).GetOptions(FakeJiraDataCenter.AConnection(), JiraReleaseSourceKey);

            var line = TheOneSweepLine();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(line.Properties[Field.SweepPurpose]?.ToString(), Is.EqualTo(nameof(DeliverySourceSweepPurpose.Picker)));
                Assert.That(line.Properties[Field.Deployment]?.ToString(), Is.EqualTo("DataCenter"));
                Assert.That(line.Properties[Field.ReleasesKept], Is.EqualTo(3));
                Assert.That(line.Properties[Field.VersionsRead], Is.EqualTo(5),
                    "counted before the status filter, so the line shows what Data Center made Lighthouse page through.");
                Assert.That(line.Properties[Field.VersionPages], Is.EqualTo(3));
                Assert.That(line.Properties[Field.ProjectCount], Is.EqualTo(2));
                Assert.That(line.Properties[Field.ProjectsSkipped], Is.Zero);
                Assert.That(line.Properties[Field.Complete], Is.True);
                Assert.That(Convert.ToInt64(line.Properties[Field.ElapsedMs], System.Globalization.CultureInfo.InvariantCulture),
                    Is.EqualTo(jira.Requests.Count * (long)EachRequestTakes.TotalMilliseconds),
                    "every request of the sweep is Jira's time, from asking what kind of Jira this is to the last page.");
            }
        }

        [Test]
        [Category("driving_port")]
        public async Task A_picker_sweep_on_Cloud_counts_the_cloud_id_lookup_in_its_time()
        {
            var jira = new JiraCloudBehindTheGateway();
            var connection = JiraConnectorTestSetup.ATeamOnJiraCloud().WorkTrackingSystemConnection;
            connection.AuthenticationMethodKey = AuthenticationMethodKeys.JiraScopedToken;

            await AConnectorOver(jira).GetOptions(connection, JiraReleaseSourceKey);

            var line = TheOneSweepLine();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Properties[Field.Deployment]?.ToString(), Is.EqualTo("Cloud"));
                Assert.That(line.Properties[Field.ReleasesKept], Is.EqualTo(1));
                Assert.That(jira.Requests.Select(uri => uri.AbsolutePath), Has.Some.EndsWith(JiraCloudBehindTheGateway.CloudIdLookup));
                Assert.That(Convert.ToInt64(line.Properties[Field.ElapsedMs], System.Globalization.CultureInfo.InvariantCulture),
                    Is.EqualTo(jira.Requests.Count * (long)EachRequestTakes.TotalMilliseconds),
                    "on Cloud the site has to be asked for its cloud id before any Release, and that wait is part of the sweep.");
            }
        }

        [Test]
        public async Task A_list_remembered_from_the_last_sweep_writes_no_second_line()
        {
            var connector = AConnectorOver(PaymentsOnDataCenter().Handler);
            var connection = FakeJiraDataCenter.AConnection();

            await connector.GetOptions(connection, JiraReleaseSourceKey);
            await connector.GetOptions(connection, JiraReleaseSourceKey);

            Assert.That(SweepLines(), Has.Count.EqualTo(1), "the second list came from what the first sweep left, so nothing was swept.");
        }

        [TestCase(DeliverySourceSweepPurpose.Preview)]
        [TestCase(DeliverySourceSweepPurpose.Save)]
        [TestCase(DeliverySourceSweepPurpose.Refresh)]
        [Category("driving_port")]
        public async Task Every_sweep_to_resolve_bound_Releases_writes_one_line_named_for_the_caller_that_asked(DeliverySourceSweepPurpose purpose)
        {
            var connector = AConnectorOver(PaymentsOnDataCenter().WithWorkOn("PAY-1", TheDatedRelease).Handler);
            var connection = FakeJiraDataCenter.AConnection();

            await connector.ResolveMany(connection, JiraReleaseSourceKey, TheDatedReleaseOnItsOwn, purpose);
            await connector.ResolveMany(connection, JiraReleaseSourceKey, TheDatedReleaseOnItsOwn, purpose);

            var lines = SweepLines();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines.Select(line => line.Properties[Field.SweepPurpose]?.ToString()),
                    Is.EqualTo(Enumerable.Repeat(purpose.ToString(), 2)),
                    "nothing is remembered between two resolves, so each call sweeps and writes its own line.");
                Assert.That(lines.Select(line => line.Properties[Field.Complete]), Has.All.True);
            }
        }

        [Test]
        public async Task A_project_that_refuses_its_versions_makes_the_line_say_the_list_is_incomplete()
        {
            var jira = PaymentsOnDataCenter().WithProjectRefusingItsVersions(TheHrProject);

            await AConnectorOver(jira.Handler).GetOptions(FakeJiraDataCenter.AConnection(), JiraReleaseSourceKey);

            var line = TheOneSweepLine();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Properties[Field.Complete], Is.False);
                Assert.That(line.Properties[Field.ProjectsSkipped], Is.EqualTo(1));
                Assert.That(line.Properties[Field.ProjectCount], Is.EqualTo(2));
                Assert.That(line.Properties[Field.ReleasesKept], Is.EqualTo(2));
            }
        }

        [Test]
        public async Task A_Jira_that_cannot_be_reached_still_gets_its_line_with_nothing_invented()
        {
            var unreachable = new JiraThatCannotBeReached();

            await AConnectorOver(unreachable).GetOptions(FakeJiraDataCenter.AConnection(), JiraReleaseSourceKey);

            var line = TheOneSweepLine();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Properties[Field.Deployment]?.ToString(), Is.EqualTo("Unknown"));
                Assert.That(line.Properties[Field.ProjectCount], Is.Zero);
                Assert.That(line.Properties[Field.ReleasesKept], Is.Zero);
                Assert.That(line.Properties[Field.VersionsRead], Is.Zero);
                Assert.That(line.Properties[Field.VersionPages], Is.Zero);
                Assert.That(line.Properties[Field.Complete], Is.False);
                Assert.That(line.Failure, Is.Null, "the exception's text names the host; the could-not-ask line already carries it.");
                Assert.That(line.Message, Does.Not.Contain(JiraThatCannotBeReached.TheHost));
            }
        }

        [Test]
        public async Task A_connection_whose_address_cannot_be_used_is_named_as_unknown()
        {
            var connection = FakeJiraDataCenter.AConnection();
            connection.Options.Single(option => option.Key == JiraWorkTrackingOptionNames.Url).Value = AnAddressThatCannotBeUsed;

            await Assert.DoesNotThrowAsync(
                async () => await AConnectorOver(PaymentsOnDataCenter().Handler).GetOptions(connection, JiraReleaseSourceKey),
                "an address that cannot be used is a Jira that could not be asked, not a failure for the caller.");

            var line = TheOneSweepLine();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Properties[Field.Deployment]?.ToString(), Is.EqualTo("Unknown"),
                    "the client could not even be built, so nothing ever said which kind of Jira this is.");
                Assert.That(line.Properties[Field.ProjectCount], Is.Zero);
                Assert.That(line.Properties[Field.ReleasesKept], Is.Zero);
                Assert.That(line.Properties[Field.VersionsRead], Is.Zero);
                Assert.That(line.Properties[Field.VersionPages], Is.Zero);
                Assert.That(line.Properties[Field.ProjectsSkipped], Is.Zero);
                Assert.That(line.Properties[Field.Complete], Is.False);
                Assert.That(line.Failure, Is.Null);
                Assert.That(line.Message, Does.Not.Contain(TheUnusableAddressHost));
            }
        }

        [Test]
        public async Task A_Jira_that_will_not_say_which_kind_it_is_is_named_as_unknown()
        {
            var jira = PaymentsOnDataCenter();
            jira.HidesItsDeployment = true;

            await AConnectorOver(jira.Handler).GetOptions(FakeJiraDataCenter.AConnection(), JiraReleaseSourceKey);

            var line = TheOneSweepLine();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Properties[Field.Deployment]?.ToString(), Is.EqualTo("Unknown"));
                Assert.That(line.Properties[Field.ProjectCount], Is.EqualTo(2));
            }
        }

        [Test]
        public async Task The_line_names_no_host_connection_project_or_Release_of_the_customer()
        {
            var connection = FakeJiraDataCenter.AConnection();
            var host = new Uri(OptionOf(connection, JiraWorkTrackingOptionNames.Url)).Host;
            var username = OptionOf(connection, JiraWorkTrackingOptionNames.Username);
            var apiToken = OptionOf(connection, JiraWorkTrackingOptionNames.ApiToken);

            await AConnectorOver(PaymentsOnDataCenter().Handler).GetOptions(connection, JiraReleaseSourceKey);

            var line = TheOneSweepLine();
            var values = line.Properties.Values.Select(value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty).ToList();
            string[] customerWords = [.. WhatNamesTheCustomer, host, connection.Name ?? host, username, apiToken];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Properties.Keys, Is.EquivalentTo(JiraReleaseSweepCost.PropertyNames));
                Assert.That(values, Has.None.AnyOf(customerWords));
                Assert.That(customerWords.Where(word => line.Message.Contains(word, StringComparison.Ordinal)), Is.Empty);
            }
        }

        [Test]
        public async Task The_sweep_writes_its_project_count_in_its_line_and_nowhere_else()
        {
            await AConnectorOver(PaymentsOnDataCenter().Handler).GetOptions(FakeJiraDataCenter.AConnection(), JiraReleaseSourceKey);

            var sweepLine = TheOneSweepLine();
            var everyOtherLine = logger.Everything.Where(entry => !ReferenceEquals(entry, sweepLine)).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(everyOtherLine.Where(entry => entry.Properties.ContainsKey(Field.ProjectCount)), Is.Empty,
                    "at any level: a second line with the count is a second place an operator has to look.");
                Assert.That(everyOtherLine.Where(entry => entry.Message.Contains("projects", StringComparison.OrdinalIgnoreCase)), Is.Empty);
            }
        }

        [Test]
        public async Task A_sweep_that_ends_in_an_unexpected_failure_still_writes_what_it_had_counted_before_the_failure_goes_on()
        {
            var jira = new FakeJiraDataCenter { VersionsPerPage = 3 }.WithProject(ThePaymentsProject, "Payments");
            for (var version = 1; version <= 6; version++)
            {
                jira.WithReleaseIn(ThePaymentsProject, $"2000{version}", $"PAY 2027.0{version}", "2027-01-15");
            }

            var connector = AConnectorOver(new BreakingOnTheSecondVersionPage(jira.Handler));

            await Assert.CatchAsync<InvalidOperationException>(
                async () => await connector.GetOptions(FakeJiraDataCenter.AConnection(), JiraReleaseSourceKey),
                "a failure the sweep does not expect is not its to swallow.");

            var line = TheOneSweepLine();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line.Properties[Field.SweepPurpose]?.ToString(), Is.EqualTo(nameof(DeliverySourceSweepPurpose.Picker)));
                Assert.That(line.Properties[Field.Deployment]?.ToString(), Is.EqualTo("DataCenter"));
                Assert.That(line.Properties[Field.ProjectCount], Is.EqualTo(1));
                Assert.That(line.Properties[Field.VersionPages], Is.EqualTo(1), "the first page was read; the second never answered.");
                Assert.That(line.Properties[Field.VersionsRead], Is.EqualTo(3));
                Assert.That(line.Properties[Field.ReleasesKept], Is.Zero, "the project's read never finished, so nothing was kept.");
                Assert.That(line.Properties[Field.ProjectsSkipped], Is.Zero, "a project is skipped when its read is caught, and this one escaped.");
                Assert.That(line.Properties[Field.Complete], Is.False);
                Assert.That(line.Failure, Is.Null, "the failure's own text goes wherever the failure goes, not into the cost line.");
            }
        }

        private static string OptionOf(WorkTrackingSystemConnection connection, string key)
            => connection.Options.Single(option => option.Key == key).Value;

        private JiraWorkTrackingConnector AConnectorOver(HttpMessageHandler jira)
            => JiraConnectorTestSetup.AConnectorOver(new ClockMovingOnPerRequest(jira, clock, EachRequestTakes), logger, new Cache<string, object>(), clock);

        private List<RecordingLogger<JiraWorkTrackingConnector>.Entry> SweepLines()
            => [.. logger.Everything.Where(entry => entry.Message.StartsWith(TheSweepLine, StringComparison.Ordinal))];

        private RecordingLogger<JiraWorkTrackingConnector>.Entry TheOneSweepLine()
        {
            var lines = SweepLines();
            Assert.That(lines, Has.Count.EqualTo(1), "one sweep, one line.");
            return lines[0];
        }

        /// <summary>Jira answering after a fixed time per request, on the clock the connector reads.</summary>
        private sealed class ClockMovingOnPerRequest(HttpMessageHandler jira, FakeTimeProvider clock, TimeSpan step) : DelegatingHandler(jira)
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                clock.Advance(step);
                return await base.SendAsync(request, cancellationToken);
            }
        }

        /// <summary>
        /// A Jira Cloud site reached through Atlassian's gateway, so the site is asked for its cloud id before
        /// anything else. One project holding one Release still to ship.
        /// </summary>
        private sealed class JiraCloudBehindTheGateway : HttpMessageHandler
        {
            public const string CloudIdLookup = "/_edge/tenant_info";

            public ConcurrentQueue<Uri> Requests { get; } = new();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var uri = request.RequestUri!;
                Requests.Enqueue(uri);

                var path = uri.AbsolutePath;
                var body = path switch
                {
                    _ when path.EndsWith(CloudIdLookup, StringComparison.Ordinal) => "{\"cloudId\":\"cloud-6398\"}",
                    _ when path.EndsWith("/rest/api/3/project/search", StringComparison.Ordinal)
                        => $"{{\"isLast\":true,\"values\":[{{\"key\":\"{ThePaymentsProject}\",\"name\":\"Payments\"}}]}}",
                    _ when path.EndsWith($"/rest/api/3/project/{ThePaymentsProject}/version", StringComparison.Ordinal)
                        => $"{{\"startAt\":0,\"total\":1,\"isLast\":true,\"values\":[{{\"id\":\"{TheDatedRelease}\",\"name\":\"PAY 2026.11\"," +
                           "\"archived\":false,\"released\":false,\"releaseDate\":\"2026-11-20\"}]}",
                    _ => null,
                };

                return Task.FromResult(body is null
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
            }
        }

        /// <summary>A Jira whose host does not resolve, the way a mistyped or retired instance fails.</summary>
        private sealed class JiraThatCannotBeReached : HttpMessageHandler
        {
            public const string TheHost = "jira.payments.example.invalid";

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => throw new HttpRequestException($"No such host is known ({TheHost}:443)");
        }

        /// <summary>
        /// A Data Center that answers a project's first page of versions and then fails in a way none of the ways Jira
        /// can fail to answer covers, so the failure is not caught on the way out and the sweep ends half-way.
        /// </summary>
        private sealed class BreakingOnTheSecondVersionPage(HttpMessageHandler jira) : DelegatingHandler(jira)
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var uri = request.RequestUri!;
                var isALaterVersionPage = uri.AbsolutePath.EndsWith("/version", StringComparison.Ordinal)
                    && !uri.Query.Contains("startAt=0", StringComparison.Ordinal)
                    && uri.Query.Contains("startAt=", StringComparison.Ordinal);

                return isALaterVersionPage
                    ? throw new InvalidOperationException("The handler was in no state to send")
                    : base.SendAsync(request, cancellationToken);
            }
        }
    }
}
