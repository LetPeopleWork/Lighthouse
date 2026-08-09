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
        private const string DateField = "Microsoft.VSTS.Scheduling.TargetDate";
        private const string PoisonField = "Custom.DoesNotExist";

        private Mock<WorkItemTrackingHttpClient> witClientMock = null!;
        private int patchCount;

        [SetUp]
        public void Setup()
        {
            patchCount = 0;

            witClientMock = new Mock<WorkItemTrackingHttpClient>(
                new Uri("https://dev.azure.com/lighthouse-test"), new VssCredentials());

            AnswerPatchesWith(_ => new AdoWorkItem());
        }

        private void RefuseEveryPatch()
            => AnswerPatchesWith(_ => throw new InvalidOperationException("TF401320: Rule Error"));

        /// <summary>
        /// What Azure DevOps was measured doing (ADR-143): a mixed patch document is rejected whole, and
        /// the same operations sent alone then apply.
        /// </summary>
        private void RefuseBatchesOfMoreThanOneOperation()
            => AnswerPatchesWith(patch => patch.Count > 1
                ? throw new InvalidOperationException("TF401320: Rule Error")
                : new AdoWorkItem());

        private void RefuseAnyPatchTouching(string poisonField)
            => AnswerPatchesWith(patch => patch.Any(operation => operation.Path.EndsWith(poisonField, StringComparison.Ordinal))
                ? throw new InvalidOperationException($"TF401326: Field '{poisonField}' does not exist")
                : new AdoWorkItem());

        private void AnswerPatchesWith(Func<JsonPatchDocument, AdoWorkItem> answer)
        {
            witClientMock
                .Setup(c => c.UpdateWorkItemAsync(
                    It.IsAny<JsonPatchDocument>(), It.IsAny<int>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                    It.IsAny<bool?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns((JsonPatchDocument patch, int _, bool? _1, bool? _2, bool? _3,
                    WorkItemExpand? _4, object _5, CancellationToken _6) =>
                {
                    patchCount++;
                    return Task.FromResult(answer(patch));
                });
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
            RefuseEveryPatch();

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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results.Single().NotificationSuppression, Is.EqualTo(NotificationSuppression.NotApplicable),
                    "The request never left the process, so the question never arose — which is not the same as arising unanswered.");
                Assert.That(results.Single().ErrorMessage, Does.Contain("not-a-number"),
                    "An administrator reading the log needs to know which reference Azure DevOps could not take.");
            }
        }

        [Test]
        public async Task UpdateItem_TheOnlyFieldIsRefused_DoesNotRetryIt()
        {
            RefuseEveryPatch();

            var results = await CreateSubject().UpdateItem("https://dev.azure.com/lighthouse-test", witClientMock.Object, "42", [
                Update("42", AgeField, "5"),
            ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(patchCount, Is.EqualTo(1),
                    "There is nothing to isolate a single operation from, so a retry only costs a second call.");
                Assert.That(results.Single().NotificationSuppression, Is.EqualTo(NotificationSuppression.Unknown));
            }
        }

        [Test]
        public async Task UpdateItem_BatchRefusedThenFieldsAccepted_ReportsSuppressionPerField()
        {
            RefuseBatchesOfMoreThanOneOperation();

            var results = await CreateSubject().UpdateItem("https://dev.azure.com/lighthouse-test", witClientMock.Object, "42", [
                Update("42", AgeField, "5"),
                Update("42", DateField, "2026-09-01"),
            ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Has.Count.EqualTo(2),
                    "Every field asked about gets an answer — an empty result set would make the outcome assertion below vacuous.");
                Assert.That(results.Select(result => result.NotificationSuppression),
                    Is.All.EqualTo(NotificationSuppression.Suppressed),
                    "A field that landed on the unbatched retry was still written quietly — the fallback changes the call count, not the silence.");
            }
        }

        [Test]
        public async Task UpdateItem_OneFieldRefusedOnTheRetry_ReportsThatFieldAloneAsUnknown()
        {
            RefuseAnyPatchTouching(PoisonField);

            var results = await CreateSubject().UpdateItem("https://dev.azure.com/lighthouse-test", witClientMock.Object, "42", [
                Update("42", AgeField, "5"),
                Update("42", PoisonField, "nonsense"),
            ]);

            var byField = results.ToDictionary(result => result.TargetFieldReference);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(byField[AgeField].NotificationSuppression, Is.EqualTo(NotificationSuppression.Suppressed),
                    "The good field landed, quietly, on its own retry.");
                Assert.That(byField[PoisonField].NotificationSuppression, Is.EqualTo(NotificationSuppression.Unknown),
                    "The offending field never landed, so nothing can be claimed about how quietly it did so.");
            }
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
