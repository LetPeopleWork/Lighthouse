using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class DatabaseOperationTrackerTest
    {
        private DatabaseOperationTracker subject;

        [SetUp]
        public void SetUp()
        {
            subject = new DatabaseOperationTracker();
        }

        [Test]
        public void StartOperation_ReturnsStatusWithOperationId()
        {
            var status = subject.StartOperation("op-1", DatabaseOperationType.Backup);

            Assert.That(status.OperationId, Is.EqualTo("op-1"));
        }

        [Test]
        public void StartOperation_SetsStateToAdmitted()
        {
            var status = subject.StartOperation("op-1", DatabaseOperationType.Backup);

            Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Admitted));
        }

        [Test]
        public void StartOperation_SetsOperationType()
        {
            var status = subject.StartOperation("op-1", DatabaseOperationType.Restore);

            Assert.That(status.OperationType, Is.EqualTo(DatabaseOperationType.Restore));
        }

        [Test]
        public void GetStatus_ExistingOperation_ReturnsStatus()
        {
            subject.StartOperation("op-1", DatabaseOperationType.Backup);

            var status = subject.GetStatus("op-1");

            Assert.That(status, Is.Not.Null);
        }

        [Test]
        public void GetStatus_NonExistentOperation_ReturnsNull()
        {
            var status = subject.GetStatus("nonexistent");

            Assert.That(status, Is.Null);
        }

        [Test]
        public void TransitionTo_UpdatesState()
        {
            subject.StartOperation("op-1", DatabaseOperationType.Backup);

            subject.TransitionTo("op-1", DatabaseOperationState.Executing);

            var status = subject.GetStatus("op-1");
            Assert.That(status!.State, Is.EqualTo(DatabaseOperationState.Executing));
        }

        [Test]
        public void TransitionTo_NonExistentOperation_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => subject.TransitionTo("nonexistent", DatabaseOperationState.Executing));
        }

        [Test]
        public void TransitionToFailed_SetsStateAndReason()
        {
            subject.StartOperation("op-1", DatabaseOperationType.Restore);

            subject.TransitionToFailed("op-1", "Invalid backup file");

            var status = subject.GetStatus("op-1");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(status!.State, Is.EqualTo(DatabaseOperationState.Failed));
                Assert.That(status.FailureReason, Is.EqualTo("Invalid backup file"));
            }
        }

        [Test]
        public void GetLatestStatus_NoOperations_ReturnsNull()
        {
            var status = subject.GetLatestStatus();

            Assert.That(status, Is.Null);
        }

        [Test]
        public void GetLatestStatus_MultipleOperations_ReturnsLatest()
        {
            subject.StartOperation("op-1", DatabaseOperationType.Backup);
            subject.StartOperation("op-2", DatabaseOperationType.Restore);

            var status = subject.GetLatestStatus();

            Assert.That(status!.OperationId, Is.EqualTo("op-2"));
        }

        [Test]
        public void TransitionTo_RestartPending_MaintainsStateAcrossQueries()
        {
            subject.StartOperation("op-1", DatabaseOperationType.Restore);
            subject.TransitionTo("op-1", DatabaseOperationState.Executing);
            subject.TransitionTo("op-1", DatabaseOperationState.RestartPending);

            var status = subject.GetStatus("op-1");

            Assert.That(status!.State, Is.EqualTo(DatabaseOperationState.RestartPending));
        }
    }
}
