using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// DISTILL specifications for the staging seam itself (Epic 5500 slice 01, ADR-144 D-A7). These say
    /// what <see cref="WriteBackCollector"/> promises in isolation; the scenarios in
    /// <c>API/Integration/QuietWriteBack</c> say what a refresh promises through it.
    /// </summary>
    public class WriteBackCollectorTest
    {
        private const string FieldReference = "customfield_10042";

        private Mock<IWriteBackService> writeBackServiceMock;
        private Mock<ILogger<WriteBackCollector>> loggerMock;

        [SetUp]
        public void Setup()
        {
            writeBackServiceMock = new Mock<IWriteBackService>();
            loggerMock = new Mock<ILogger<WriteBackCollector>>();

            writeBackServiceMock
                .Setup(s => s.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ReturnsAsync((WorkTrackingSystemConnection _, IReadOnlyList<WriteBackFieldUpdate> updates) => new WriteBackResult
                {
                    ItemResults = [.. updates.Select(u => new WriteBackItemResult
                    {
                        WorkItemId = u.WorkItemId,
                        TargetFieldReference = u.TargetFieldReference,
                        Success = true,
                    })],
                });
        }

        [Test]
        public void Stage_DoesNotWriteAnything()
        {
            var subject = CreateSubject();

            subject.Stage(Connection(1), [Update("PROJ-1", FieldReference, "5")]);

            writeBackServiceMock.Verify(
                s => s.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never,
                "Staging is a dictionary upsert. The only member allowed to perform I/O is FlushAsync.");
        }

        // AC-04.2 - the same field resolved by more than one pass produces exactly one write.
        [Test]
        public async Task Stage_SameFieldTwice_FlushWritesItOnceCarryingTheLaterValue()
        {
            var connection = Connection(1);
            var subject = CreateSubject();

            subject.Stage(connection, [Update("PROJ-1", FieldReference, "5")]);
            subject.Stage(connection, [Update("PROJ-1", FieldReference, "8")]);

            await subject.FlushAsync();

            writeBackServiceMock.Verify(
                s => s.WriteFieldsToWorkItems(connection, It.Is<IReadOnlyList<WriteBackFieldUpdate>>(
                    updates => updates.Count == 1 && updates[0].Value == "8")),
                Times.Once,
                "The later pass holds the fresher value, so the later stage wins.");
        }

        // AC-04.3 at the seam: an execution that staged nothing costs a dictionary-count check.
        [Test]
        public async Task FlushAsync_NothingStaged_WritesNothing()
        {
            var subject = CreateSubject();

            var results = await subject.FlushAsync();

            Assert.That(results, Is.Empty);
            writeBackServiceMock.Verify(
                s => s.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task FlushAsync_TwoConnections_WritesOncePerConnection()
        {
            var first = Connection(1);
            var second = Connection(2);
            var subject = CreateSubject();

            subject.Stage(first, [Update("PROJ-1", FieldReference, "5")]);
            subject.Stage(second, [Update("42", FieldReference, "5")]);

            await subject.FlushAsync();

            writeBackServiceMock.Verify(s => s.WriteFieldsToWorkItems(first, It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()), Times.Once);
            writeBackServiceMock.Verify(s => s.WriteFieldsToWorkItems(second, It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()), Times.Once);
        }

        // AC-04.5 - per-item semantics reach the caller unchanged.
        [Test]
        public async Task FlushAsync_ReportsEachItemResultVerbatim()
        {
            var connection = Connection(1);
            writeBackServiceMock
                .Setup(s => s.WriteFieldsToWorkItems(connection, It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults =
                    [
                        new WriteBackItemResult { WorkItemId = "PROJ-1", TargetFieldReference = FieldReference, Success = true },
                        new WriteBackItemResult { WorkItemId = "PROJ-2", TargetFieldReference = FieldReference, Success = false, ErrorMessage = "Field not found" },
                    ],
                });

            var subject = CreateSubject();
            subject.Stage(connection, [Update("PROJ-1", FieldReference, "5"), Update("PROJ-2", FieldReference, "5")]);

            var results = await subject.FlushAsync();

            var itemResults = results.Single().ItemResults;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(itemResults.Single(r => r.WorkItemId == "PROJ-1").Success, Is.True);
                Assert.That(itemResults.Single(r => r.WorkItemId == "PROJ-2").ErrorMessage, Is.EqualTo("Field not found"));
            }
        }

        [Test]
        public async Task FlushAsync_CalledTwice_DoesNotRewriteWhatItAlreadyWrote()
        {
            var connection = Connection(1);
            var subject = CreateSubject();
            subject.Stage(connection, [Update("PROJ-1", FieldReference, "5")]);

            await subject.FlushAsync();
            await subject.FlushAsync();

            writeBackServiceMock.Verify(
                s => s.WriteFieldsToWorkItems(connection, It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Once,
                "Flushing clears the staging area, so a second terminal flush in the same scope is a no-op.");
        }

        private WriteBackCollector CreateSubject() => new(writeBackServiceMock.Object, loggerMock.Object);

        private static WorkTrackingSystemConnection Connection(int id)
            => new() { Id = id, Name = $"Connection {id}", WorkTrackingSystem = WorkTrackingSystems.Jira };

        private static WriteBackFieldUpdate Update(string workItemId, string fieldReference, string value)
            => new() { WorkItemId = workItemId, TargetFieldReference = fieldReference, Value = value };
    }
}
