using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// DISTILL specifications for Epic 5500 slice 04 (Story #5505, ADR-142 §6) — the one signal an admin
    /// gets that their write-backs are emailing watchers, until slice 05's connection surface ships.
    ///
    /// Three properties carry the design: it is <b>one</b> line per connection per flush (a portfolio-wide
    /// refusal must not become a log storm), it aggregates <b>NotSuppressed only</b> (an item that never
    /// landed says nothing about permissions), and it is at <b>Warning</b> — deliberately louder than the
    /// LogDebug around it, which is invisible at production log levels.
    /// </summary>
    [TestFixture]
    [Category("epic-5500-quiet-writeback")]
    [Category("slice-04")]
    public class WriteBackSuppressionWarningTest
    {
        private const string SizeField = "customfield_10042";
        private const string ForecastField = "customfield_10099";

        private const string StaleValue = "stale";

        private Mock<IWorkTrackingConnectorFactory> connectorFactoryMock = null!;
        private Mock<IWorkTrackingConnector> connectorMock = null!;
        private Mock<ILogger<WriteBackService>> loggerMock = null!;
        private Mock<IWorkItemRepository> workItemRepositoryMock = null!;
        private Mock<IRepository<Feature>> featureRepositoryMock = null!;

        private List<string> warnings = null!;
        private List<Feature> features = null!;

        [SetUp]
        public void Setup()
        {
            connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorMock = new Mock<IWorkTrackingConnector>();
            loggerMock = new Mock<ILogger<WriteBackService>>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();

            warnings = [];
            features = [];

            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connectorMock.Object);

            workItemRepositoryMock.Setup(x => x.GetAll()).Returns([]);
            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);

            CaptureWarnings();
        }

        [Test]
        public async Task WriteFieldsToWorkItems_SomeWritesCouldNotBeSilenced_WarnsOnce()
        {
            var connection = GivenAJiraConnection();
            GivenTheTrackerAnswers(Noisy("PROJ-1", SizeField), Noisy("PROJ-1", ForecastField), Noisy("PROJ-2", SizeField));

            await WhenWriteBackRunsFor(connection, "PROJ-1", "PROJ-2");

            ThenWarningsLogged(1, "One line per connection per flush — a portfolio-wide refusal must not flood the log.");
        }

        [Test]
        public async Task WriteFieldsToWorkItems_SomeWritesCouldNotBeSilenced_NamesTheConnectionTheProjectsAndTheRemedy()
        {
            var connection = GivenAJiraConnection();
            GivenTheTrackerAnswers(Noisy("PROJ-1", SizeField), Noisy("OTHER-7", SizeField));

            await WhenWriteBackRunsFor(connection, "PROJ-1", "OTHER-7");

            var warning = TheWarning();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(warning, Does.Contain(connection.Name), "An admin with six connections needs to know which one.");
                Assert.That(warning, Does.Contain("PROJ"), "The permission is project-scoped, so the projects are the actionable part.");
                Assert.That(warning, Does.Contain("OTHER"));
                Assert.That(warning, Does.Contain("Administer Projects"), "A warning without the remedy is a complaint.");
            }
        }

        // ADR-142 §3 / D-A4 — the failure this guards is sending an admin to grant a permission that was
        // never the problem.
        [Test]
        public async Task WriteFieldsToWorkItems_TheWritesFailedOutright_DoesNotWarnAboutNotifications()
        {
            var connection = GivenAJiraConnection();
            GivenTheTrackerAnswers(Unknown("PROJ-1", SizeField), Unknown("PROJ-1", ForecastField));

            await WhenWriteBackRunsFor(connection, "PROJ-1");

            ThenWarningsLogged(0, "A 403 that survived the retry was never a suppression problem.");
        }

        [Test]
        public async Task WriteFieldsToWorkItems_EveryWriteWasSilenced_DoesNotWarn()
        {
            var connection = GivenAJiraConnection();
            GivenTheTrackerAnswers(Quiet("PROJ-1", SizeField), Quiet("PROJ-1", ForecastField));

            await WhenWriteBackRunsFor(connection, "PROJ-1");

            ThenWarningsLogged(0, "Nothing to report is nothing to log.");
        }

        [Test]
        public async Task WriteFieldsToWorkItems_OneProjectNoisyOneQuiet_WarnsAboutTheNoisyProjectOnly()
        {
            var connection = GivenAJiraConnection();
            GivenTheTrackerAnswers(Quiet("QUIET-1", SizeField), Noisy("NOISY-1", SizeField));

            await WhenWriteBackRunsFor(connection, "QUIET-1", "NOISY-1");

            var warning = TheWarning();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(warning, Does.Contain("NOISY"));
                Assert.That(warning, Does.Not.Contain("QUIET"),
                    "One connection can be quiet in one project and noisy in another — naming a silent project sends the admin to grant a permission it already has.");
            }
        }

        // D-A10, borrowed early: a reference that is not a Jira key is reported as unknown, never dropped
        // and never folded into a neighbour.
        [Test]
        public async Task WriteFieldsToWorkItems_AReferenceThatIsNotAnIssueKey_IsReportedRatherThanDropped()
        {
            var connection = GivenAJiraConnection();
            GivenTheTrackerAnswers(Noisy("12345", SizeField));

            await WhenWriteBackRunsFor(connection, "12345");

            var warning = TheWarning();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(warning, Does.Contain("12345").IgnoreCase.Or.Contain("unknown").IgnoreCase);
                Assert.That(warning, Does.Contain("Administer Projects"));
            }
        }

        [Test]
        public async Task WriteFieldsToWorkItems_TwoFlushesOnTheSameConnection_WarnsOncePerFlush()
        {
            var connection = GivenAJiraConnection();
            GivenTheTrackerAnswers(Noisy("PROJ-1", SizeField));

            await WhenWriteBackRunsFor(connection, "PROJ-1");
            await WhenWriteBackRunsFor(connection, "PROJ-1");

            ThenWarningsLogged(2, "The next refresh is new information, not a repeat of the last one.");
        }

        // --- Given ---

        private static WorkTrackingSystemConnection GivenAJiraConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = 42,
                Name = "Jira Production",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 1, Reference = SizeField, DisplayName = "Size" });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 2, Reference = ForecastField, DisplayName = "Forecast" });

            return connection;
        }

        private void GivenTheTrackerAnswers(params WriteBackItemResult[] answers)
        {
            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ReturnsAsync(new WriteBackResult { ItemResults = answers });
        }

        private static WriteBackItemResult Quiet(string workItemId, string field) => new()
        {
            WorkItemId = workItemId,
            TargetFieldReference = field,
            Success = true,
            NotificationSuppression = NotificationSuppression.Suppressed,
        };

        private static WriteBackItemResult Noisy(string workItemId, string field) => new()
        {
            WorkItemId = workItemId,
            TargetFieldReference = field,
            Success = true,
            NotificationSuppression = NotificationSuppression.NotSuppressed,
        };

        private static WriteBackItemResult Unknown(string workItemId, string field) => new()
        {
            WorkItemId = workItemId,
            TargetFieldReference = field,
            Success = false,
            ErrorMessage = "Jira returned 403 Forbidden",
            NotificationSuppression = NotificationSuppression.Unknown,
        };

        // --- When ---

        private async Task WhenWriteBackRunsFor(WorkTrackingSystemConnection connection, params string[] referenceIds)
        {
            features.Clear();

            var updates = new List<WriteBackFieldUpdate>();

            foreach (var referenceId in referenceIds)
            {
                features.Add(new Feature(new WorkItemBase
                {
                    ReferenceId = referenceId,
                    AdditionalFieldValues = new Dictionary<int, string?> { { 1, StaleValue }, { 2, StaleValue } },
                }));

                updates.Add(new WriteBackFieldUpdate { WorkItemId = referenceId, TargetFieldReference = SizeField, Value = "5" });
                updates.Add(new WriteBackFieldUpdate { WorkItemId = referenceId, TargetFieldReference = ForecastField, Value = "2026-09-01" });
            }

            await CreateSubject().WriteFieldsToWorkItems(connection, updates);
        }

        // --- Then ---

        private void ThenWarningsLogged(int expected, string because)
            => Assert.That(warnings, Has.Count.EqualTo(expected), because + " Logged: " + string.Join(" | ", warnings));

        private string TheWarning()
        {
            Assert.That(warnings, Has.Count.EqualTo(1), "Expected exactly one warning. Logged: " + string.Join(" | ", warnings));
            return warnings[0];
        }

        /// <summary>
        /// Counting Warnings by level rather than by message fragment: an unrelated second Warning then
        /// fails the count instead of hiding behind a match.
        /// </summary>
        private void CaptureWarnings()
        {
            loggerMock
                .Setup(l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(new InvocationAction(invocation => warnings.Add(invocation.Arguments[2]?.ToString() ?? string.Empty)));
        }

        private WriteBackService CreateSubject()
            => new(connectorFactoryMock.Object, loggerMock.Object, workItemRepositoryMock.Object, featureRepositoryMock.Object);
    }
}
