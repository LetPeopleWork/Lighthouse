using Lighthouse.Backend.Models.OAuth;
using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Lighthouse.Backend.Modules.Forecasting.Application.WriteBack;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Only Jira and Azure DevOps can write a value back into the tracker. A connector that cannot must not claim
    /// it can, so writing back is its own capability that only those two connectors take on. A write-back to any
    /// other connection still fails each update, as it always has, and stores nothing.
    /// </summary>
    [TestFixture]
    public class WriteBackTargetCapabilityArchUnitTest
    {
        private static readonly string WriteBackCapabilityName = typeof(IWriteBackTarget).FullName!;

        private const string WriteBackMember = "WriteFieldsToWorkItems";

        private const string StoredValue = "stored before the write-back";

        private static readonly string[] ConnectorsThatWriteBack = ["JiraWorkTrackingConnector", "AzureDevOpsWorkTrackingConnector"];

        private static readonly string[] EveryKnownConnector =
        [
            "JiraWorkTrackingConnector",
            "AzureDevOpsWorkTrackingConnector",
            "LinearWorkTrackingConnector",
            "CsvWorkTrackingConnector",
            "ServiceNowWorkTrackingConnector",
        ];

        private static readonly Type[] WriteBackParameters = [typeof(WorkTrackingSystemConnection), typeof(IReadOnlyList<WriteBackFieldUpdate>)];

        [Test]
        public void WritingBackIsItsOwnCapability_AndNoLongerAskedOfEveryConnector()
        {
            var capability = GivenTheWriteBackCapability();

            ThenTheCapabilityDeclaresTheWriteBack(capability);
            ThenEveryConnectorIsNoLongerAskedToWriteBack();
        }

        [Test]
        public void OnlyJiraAndAzureDevOpsAreWriteBackTargets()
        {
            var capability = GivenTheWriteBackCapability();

            var connectors = WhenEveryConnectorIsRead();

            ThenEveryKnownConnectorWasRead(connectors);
            ThenExactlyJiraAndAzureDevOpsWriteBack(connectors, capability);
        }

        [Test]
        public async Task AWriteBackToACsvConnection_FailsEachUpdateAndStoresNothing()
        {
            var scenario = GivenACsvConnectionWithTwoItemsWhoseValuesChange();

            var result = await WhenTheValuesAreWrittenBack(scenario);

            ThenEachUpdateFailed(result, scenario);
            ThenNoWrittenValueWasStored(scenario);
        }

        private static Type GivenTheWriteBackCapability()
        {
            var capability = typeof(IWorkTrackingConnector).Assembly.GetType(WriteBackCapabilityName);

            Assert.That(
                capability,
                Is.Not.Null,
                $"WHAT: there is no {WriteBackCapabilityName} in Lighthouse.Backend. WHY: without a write-back capability of its own, " +
                "every connector is asked to write back, and CSV, Linear and ServiceNow can only refuse what they claim to do. HOW: add " +
                "the interface in Modules/TrackerIntegration/Contracts, move WriteFieldsToWorkItems onto it, and let only the Jira " +
                "and Azure DevOps connectors take it on.");
            Assert.That(capability!.IsInterface, Is.True, $"WHAT: {WriteBackCapabilityName} is not an interface. WHY: a connector takes a capability on by implementing it. HOW: declare it as an interface.");

            return capability;
        }

        private static List<Type> WhenEveryConnectorIsRead()
            => [.. typeof(IWorkTrackingConnector).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IWorkTrackingConnector).IsAssignableFrom(type))];

        private static void ThenTheCapabilityDeclaresTheWriteBack(Type capability)
        {
            var writeBack = capability.GetMethod(WriteBackMember, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, WriteBackParameters);

            Assert.That(
                writeBack?.ReturnType,
                Is.EqualTo(typeof(Task<WriteBackResult>)),
                $"WHAT: {WriteBackCapabilityName} does not declare Task<WriteBackResult> {WriteBackMember}(WorkTrackingSystemConnection, " +
                "IReadOnlyList<WriteBackFieldUpdate>). WHY: the write-back service hands a connection and its updates to the capability " +
                "and reads one result per update back. HOW: move the member from IWorkTrackingConnector onto the capability unchanged.");
        }

        private static void ThenEveryConnectorIsNoLongerAskedToWriteBack()
        {
            var stillOnTheBase = typeof(IWorkTrackingConnector).GetMethod(WriteBackMember, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, WriteBackParameters);

            Assert.That(
                stillOnTheBase,
                Is.Null,
                $"WHAT: IWorkTrackingConnector still declares {WriteBackMember}. WHY: then CSV, Linear and ServiceNow must still claim a " +
                "write-back they can only refuse. HOW: remove the member from IWorkTrackingConnector; it lives on the write-back capability.");
        }

        private static void ThenEveryKnownConnectorWasRead(List<Type> connectors)
        {
            var read = connectors.Select(type => type.Name).ToList();

            Assert.That(
                read,
                Is.SupersetOf(EveryKnownConnector),
                $"WHAT: the connectors read were [{string.Join(", ", read)}], which does not hold all five of [{string.Join(", ", EveryKnownConnector)}]. " +
                "WHY: a connector that is not read is never checked, and the answer below would hold without looking. HOW: each connector " +
                "must remain a concrete class in Lighthouse.Backend implementing IWorkTrackingConnector.");
        }

        private static void ThenExactlyJiraAndAzureDevOpsWriteBack(List<Type> connectors, Type capability)
        {
            var writingBack = connectors.Where(capability.IsAssignableFrom).Select(type => type.Name).ToList();

            Assert.That(
                writingBack,
                Is.EquivalentTo(ConnectorsThatWriteBack),
                $"WHAT: the connectors that take on the write-back capability are [{string.Join(", ", writingBack)}], expected exactly " +
                $"[{string.Join(", ", ConnectorsThatWriteBack)}]. WHY: only Jira and Azure DevOps can write a value back; any other " +
                "connector would claim a write-back it can only refuse, and dropping one of the two would stop its write-backs. HOW: list " +
                "the capability on IJiraWorkTrackingConnector and IAzureDevOpsWorkTrackingConnector only, and remove the write-back member " +
                "from the CSV, Linear and ServiceNow connectors.");
        }

        private static CsvWriteBackScenario GivenACsvConnectionWithTwoItemsWhoseValuesChange()
        {
            var connection = new WorkTrackingSystemConnection { Id = 1, Name = "A CSV connection", WorkTrackingSystem = WorkTrackingSystems.Csv };
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 7, DisplayName = "Age", Reference = "Age", WorkTrackingSystemConnection = connection });

            var feature = new Feature(ItemWithStoredValue("F-1"));
            var workItem = new WorkItem(ItemWithStoredValue("W-1"), new Team());

            WriteBackFieldUpdate[] updates =
            [
                new() { WorkItemId = "F-1", TargetFieldReference = "Age", Value = "12" },
                new() { WorkItemId = "W-1", TargetFieldReference = "Age", Value = "3" },
            ];

            return new CsvWriteBackScenario(connection, feature, workItem, updates, new Mock<IRepository<Feature>>(), new Mock<IWorkItemRepository>());

            static WorkItemBase ItemWithStoredValue(string reference)
                => new() { ReferenceId = reference, AdditionalFieldValues = new Dictionary<int, string?> { { 7, StoredValue } } };
        }

        private static Task<WriteBackResult> WhenTheValuesAreWrittenBack(CsvWriteBackScenario scenario)
        {
            var connectorFactory = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactory
                .Setup(factory => factory.GetWorkTrackingConnector(WorkTrackingSystems.Csv))
                .Returns(new CsvWorkTrackingConnector(NullLogger<CsvWorkTrackingConnector>.Instance));
            scenario.Features.Setup(repository => repository.GetAll()).Returns([scenario.Feature]);
            scenario.WorkItems.Setup(repository => repository.GetAll()).Returns([scenario.WorkItem]);

            var service = new WriteBackService(connectorFactory.Object, NullLogger<WriteBackService>.Instance, scenario.WorkItems.Object, scenario.Features.Object, Mock.Of<IRepository<OAuthCredential>>());

            return service.WriteFieldsToWorkItems(scenario.Connection, scenario.Updates);
        }

        private static void ThenEachUpdateFailed(WriteBackResult result, CsvWriteBackScenario scenario)
        {
            var failed = result.ItemResults.Where(item => !item.Success).Select(item => (item.WorkItemId, item.TargetFieldReference)).ToList();
            var expected = scenario.Updates.Select(update => (update.WorkItemId, update.TargetFieldReference)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    result.ItemResults,
                    Has.Count.EqualTo(scenario.Updates.Length),
                    $"WHAT: a write-back of {scenario.Updates.Length} updates to a CSV connection answered {result.ItemResults.Count} item results. " +
                    "WHY: the caller reports one outcome per update; a skipped update looks as if nothing was asked. HOW: when the connection's " +
                    "connector cannot write back, fail inside the existing per-batch try so every update becomes a failed item result.");
                Assert.That(
                    failed,
                    Is.EquivalentTo(expected),
                    "WHAT: not every update to a CSV connection came back as a failed item result for its own work item and field. WHY: CSV " +
                    "cannot write a value back, and a write-back to it has always failed each update. HOW: when the connector is not a " +
                    "write-back target, throw NotSupportedException inside the existing try so each update fails as before.");
            }
        }

        private static void ThenNoWrittenValueWasStored(CsvWriteBackScenario scenario)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(scenario.Feature.AdditionalFieldValues[7], Is.EqualTo(StoredValue), "WHAT: the Feature's stored value changed. WHY: a failed write-back must leave the stored copy as it was. HOW: store only values the tracker accepted.");
                Assert.That(scenario.WorkItem.AdditionalFieldValues[7], Is.EqualTo(StoredValue), "WHAT: the Work Item's stored value changed. WHY: a failed write-back must leave the stored copy as it was. HOW: store only values the tracker accepted.");
            }

            scenario.Features.Verify(repository => repository.Save(), Times.Never(), "WHAT: a failed write-back saved. WHY: nothing was written, so nothing may be stored. HOW: save only when an update succeeded.");
        }

        private sealed record CsvWriteBackScenario(
            WorkTrackingSystemConnection Connection,
            Feature Feature,
            WorkItem WorkItem,
            WriteBackFieldUpdate[] Updates,
            Mock<IRepository<Feature>> Features,
            Mock<IWorkItemRepository> WorkItems);
    }
}
