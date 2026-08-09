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
    /// Epic 5500 slice 04 (ADR-142 §5), Azure DevOps half: the flag it already passes has to become a
    /// reported fact, so the suppression rollup means the same thing whichever tracker answered. The flag
    /// itself is asserted in <c>AzureDevOpsBatchedWriteBackTest</c>; this is about the result.
    ///
    /// The rule these three pin, and the one Jira follows too: an attempt that landed is
    /// <c>Suppressed</c>, an attempt that failed is <c>Unknown</c>, and an attempt never made is
    /// <c>NotApplicable</c>.
    /// </summary>
    [TestFixture]
    [Category("epic-5500-quiet-writeback")]
    [Category("slice-04")]
    public class AzureDevOpsSuppressionOutcomeTest
    {
        private const string AgeField = "Custom.Age";

        private Mock<WorkItemTrackingHttpClient> witClientMock = null!;

        [SetUp]
        public void Setup()
        {
            witClientMock = new Mock<WorkItemTrackingHttpClient>(
                new Uri("https://dev.azure.com/lighthouse-test"), new VssCredentials());

            witClientMock
                .Setup(c => c.UpdateWorkItemAsync(
                    It.IsAny<JsonPatchDocument>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                    It.IsAny<bool?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AdoWorkItem());
        }

        [Test]
        public async Task UpdateItem_WriteAccepted_ReportsTheWriteAsSuppressed()
        {
            var results = await CreateSubject().UpdateItem("https://dev.azure.com/lighthouse-test", witClientMock.Object, "42", [
                Update("42", AgeField, "5"),
            ]);

            Assert.That(results.Single().NotificationSuppression, Is.EqualTo(NotificationSuppression.Suppressed),
                "Azure DevOps suppresses without asking for a permission — it says so, and never lands in the Jira rollup.");
        }

        [Test]
        public async Task UpdateItem_AzureDevOpsRefusedTheWrite_SaysTheSuppressionOutcomeIsUnknown()
        {
            witClientMock
                .Setup(c => c.UpdateWorkItemAsync(
                    It.IsAny<JsonPatchDocument>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                    It.IsAny<bool?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("TF401320: Rule Error"));

            var results = await CreateSubject().UpdateItem("https://dev.azure.com/lighthouse-test", witClientMock.Object, "42", [
                Update("42", AgeField, "5"),
            ]);

            Assert.That(results.Single().NotificationSuppression, Is.EqualTo(NotificationSuppression.Unknown),
                "Nothing was written, so claiming the write was quiet would be claiming something about a write that did not happen.");
        }

        [Test]
        public async Task UpdateItem_WorkItemIdIsNotANumber_SaysNothingAboutSuppression()
        {
            var results = await CreateSubject().UpdateItem("https://dev.azure.com/lighthouse-test", witClientMock.Object, "not-a-number", [
                Update("not-a-number", AgeField, "5"),
            ]);

            Assert.That(results.Single().NotificationSuppression, Is.EqualTo(NotificationSuppression.NotApplicable),
                "The request never left the process, so the question never arose — which is not the same as arising unanswered.");
        }

        private static AzureDevOpsWorkTrackingConnector CreateSubject()
        {
            var factoryMock = new Mock<IWorkTrackingAuthStrategyFactory>();
            factoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns(Mock.Of<IWorkTrackingAuthStrategy>());

            return new AzureDevOpsWorkTrackingConnector(
                Mock.Of<ILogger<AzureDevOpsWorkTrackingConnector>>(), factoryMock.Object);
        }

        private static WriteBackFieldUpdate Update(string workItemId, string fieldReference, string value)
            => new() { WorkItemId = workItemId, TargetFieldReference = fieldReference, Value = value };
    }
}
