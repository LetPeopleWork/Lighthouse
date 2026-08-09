using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Moq;
using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// AC-05.6 and AC-04.4's second half: Azure DevOps write-back must keep asking the tracker to stay
    /// silent, and batching must not quietly drop that. Neither the acceptance scenarios (which stop at
    /// <c>IWorkTrackingConnector</c>) nor the live fixture (which has no second identity to watch a
    /// notification arrive) can see the flag — so it is asserted where it is actually passed, by
    /// intercepting the SDK client. Precedent: <c>GetAllStateTransitionsThrottled</c>.
    /// </summary>
    [TestFixture]
    [Category("epic-5500-quiet-writeback")]
    [Category("slice-02")]
    public class AzureDevOpsBatchedWriteBackTest
    {
        private const string AgeField = "Custom.Age";
        private const string DateField = "Microsoft.VSTS.Scheduling.TargetDate";

        private Mock<WorkItemTrackingHttpClient> witClientMock = null!;
        private List<(JsonPatchDocument Patch, bool? SuppressNotifications)> recordedCalls = null!;

        [SetUp]
        public void Setup()
        {
            recordedCalls = [];

            witClientMock = new Mock<WorkItemTrackingHttpClient>(
                new Uri("https://dev.azure.com/lighthouse-test"), new VssCredentials());

            witClientMock
                .Setup(c => c.UpdateWorkItemAsync(
                    It.IsAny<JsonPatchDocument>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                    It.IsAny<bool?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Callback((JsonPatchDocument patch, int _, bool? _1, bool? _2,
                    bool? suppressNotifications, WorkItemExpand? _3, object _4, CancellationToken _5) =>
                    recordedCalls.Add((patch, suppressNotifications)))
                .ReturnsAsync(new AdoWorkItem());
        }

        [Test]
        public async Task UpdateItem_BatchedWrite_StillAsksAzureDevOpsToSuppressNotifications()
        {
            await CreateSubject().UpdateItem("https://dev.azure.com/lighthouse-test", witClientMock.Object, "42", [
                Update("42", AgeField, "5"),
                Update("42", DateField, "2026-09-01"),
            ]);

            Assert.That(recordedCalls, Has.Count.EqualTo(1),
                "Two changed fields on one work item are one patch document.");
            Assert.That(recordedCalls[0].SuppressNotifications, Is.True,
                "The whole point of the epic: a write-back must not notify. Batching must not drop the flag.");
        }

        [Test]
        public async Task UpdateItem_BatchedWrite_CarriesOneOperationPerField()
        {
            await CreateSubject().UpdateItem("https://dev.azure.com/lighthouse-test", witClientMock.Object, "42", [
                Update("42", AgeField, "5"),
                Update("42", DateField, "2026-09-01"),
            ]);

            var paths = recordedCalls.Single().Patch.Select(operation => operation.Path);
            Assert.That(paths, Is.EquivalentTo(new[] { $"/fields/{AgeField}", $"/fields/{DateField}" }));
        }

        [Test]
        public async Task UpdateItem_WorkItemIdIsNotANumber_RefusesEveryFieldWithoutCallingAzureDevOps()
        {
            var results = await CreateSubject().UpdateItem("https://dev.azure.com/lighthouse-test", witClientMock.Object, "not-a-number", [
                Update("not-a-number", AgeField, "5"),
                Update("not-a-number", DateField, "2026-09-01"),
            ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recordedCalls, Is.Empty);
                Assert.That(results, Has.Count.EqualTo(2),
                    "Every update still gets its own result — the caller asked about two fields.");
                Assert.That(results.Any(r => r.Success), Is.False);
            }
        }

        private static AzureDevOpsWorkTrackingConnector CreateSubject()
        {
            var strategyMock = new Mock<IWorkTrackingAuthStrategy>();
            var factoryMock = new Mock<IWorkTrackingAuthStrategyFactory>();
            factoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns(strategyMock.Object);

            return new AzureDevOpsWorkTrackingConnector(
                Mock.Of<ILogger<AzureDevOpsWorkTrackingConnector>>(), factoryMock.Object);
        }

        private static WriteBackFieldUpdate Update(string workItemId, string fieldReference, string value)
            => new() { WorkItemId = workItemId, TargetFieldReference = fieldReference, Value = value };
    }
}
