using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using System.Collections.Concurrent;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class DatabaseMaintenanceGateTest
    {
        private ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;
        private DatabaseMaintenanceGate subject;

        [SetUp]
        public void SetUp()
        {
            updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
            subject = new DatabaseMaintenanceGate(updateStatuses);
        }

        [Test]
        public void TryAcquire_NoActiveWork_ReturnsTrue()
        {
            var result = subject.TryAcquire(DatabaseOperationType.Backup, "op-1");

            Assert.That(result.Acquired, Is.True);
        }

        [Test]
        public void TryAcquire_NoActiveWork_SetsBlockedReasonToNull()
        {
            var result = subject.TryAcquire(DatabaseOperationType.Backup, "op-1");

            Assert.That(result.BlockedReason, Is.Null);
        }

        [Test]
        public void TryAcquire_BackgroundUpdateQueued_ReturnsFalse()
        {
            var key = new UpdateKey(UpdateType.Team, 1);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued };

            var result = subject.TryAcquire(DatabaseOperationType.Backup, "op-1");

            Assert.That(result.Acquired, Is.False);
        }

        [Test]
        public void TryAcquire_BackgroundUpdateInProgress_ReturnsFalse()
        {
            var key = new UpdateKey(UpdateType.Forecasts, 2);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Forecasts, Id = 2, Status = UpdateProgress.InProgress };

            var result = subject.TryAcquire(DatabaseOperationType.Restore, "op-2");

            Assert.That(result.Acquired, Is.False);
        }

        [Test]
        public void TryAcquire_BackgroundUpdateInProgress_SetsBlockedReason()
        {
            var key = new UpdateKey(UpdateType.Forecasts, 2);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Forecasts, Id = 2, Status = UpdateProgress.InProgress };

            var result = subject.TryAcquire(DatabaseOperationType.Restore, "op-2");

            Assert.That(result.BlockedReason, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void TryAcquire_BackgroundUpdateCompleted_ReturnsTrue()
        {
            var key = new UpdateKey(UpdateType.Team, 1);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Completed };

            var result = subject.TryAcquire(DatabaseOperationType.Backup, "op-1");

            Assert.That(result.Acquired, Is.True);
        }

        [Test]
        public void TryAcquire_BackupAlreadyActive_RestoreReturnsFalseWithPendingReason()
        {
            subject.TryAcquire(DatabaseOperationType.Backup, "op-backup");

            var result = subject.TryAcquire(DatabaseOperationType.Restore, "op-restore");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Acquired, Is.False);
                Assert.That(result.PendingBehindBackup, Is.True);
            }
        }

        [Test]
        public void TryAcquire_BackupAlreadyActive_ClearReturnsFalseWithPendingReason()
        {
            subject.TryAcquire(DatabaseOperationType.Backup, "op-backup");

            var result = subject.TryAcquire(DatabaseOperationType.Clear, "op-clear");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Acquired, Is.False);
                Assert.That(result.PendingBehindBackup, Is.True);
            }
        }

        [Test]
        public void TryAcquire_RestoreAlreadyActive_BackupReturnsFalse()
        {
            subject.TryAcquire(DatabaseOperationType.Restore, "op-restore");

            var result = subject.TryAcquire(DatabaseOperationType.Backup, "op-backup");

            Assert.That(result.Acquired, Is.False);
        }

        [Test]
        public void TryAcquire_ClearAlreadyActive_BackupReturnsFalse()
        {
            subject.TryAcquire(DatabaseOperationType.Clear, "op-clear");

            var result = subject.TryAcquire(DatabaseOperationType.Backup, "op-backup");

            Assert.That(result.Acquired, Is.False);
        }

        [Test]
        public void Release_AfterAcquire_AllowsNewAcquisition()
        {
            subject.TryAcquire(DatabaseOperationType.Backup, "op-1");
            subject.Release("op-1");

            var result = subject.TryAcquire(DatabaseOperationType.Restore, "op-2");

            Assert.That(result.Acquired, Is.True);
        }

        [Test]
        public void Release_UnknownOperationId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => subject.Release("nonexistent"));
        }

        [Test]
        public void IsBlocked_NoActiveWork_ReturnsFalse()
        {
            Assert.That(subject.IsBlocked, Is.False);
        }

        [Test]
        public void IsBlocked_BackgroundWorkActive_ReturnsTrue()
        {
            var key = new UpdateKey(UpdateType.Team, 1);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.InProgress };

            Assert.That(subject.IsBlocked, Is.True);
        }

        [Test]
        public void IsBlocked_DatabaseOperationActive_ReturnsTrue()
        {
            subject.TryAcquire(DatabaseOperationType.Backup, "op-1");

            Assert.That(subject.IsBlocked, Is.True);
        }

        [Test]
        public void BlockedReason_BackgroundWorkActive_DescribesBackgroundWork()
        {
            var key = new UpdateKey(UpdateType.Team, 1);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.InProgress };

            Assert.That(subject.BlockedReason, Does.Contain("background"));
        }

        [Test]
        public void BlockedReason_DatabaseOperationActive_DescribesActiveOperation()
        {
            subject.TryAcquire(DatabaseOperationType.Backup, "op-1");

            Assert.That(subject.BlockedReason, Does.Contain("Backup"));
        }

        [Test]
        public void ActiveOperationId_NoOperation_ReturnsNull()
        {
            Assert.That(subject.ActiveOperationId, Is.Null);
        }

        [Test]
        public void ActiveOperationId_AfterAcquire_ReturnsOperationId()
        {
            subject.TryAcquire(DatabaseOperationType.Backup, "op-42");

            Assert.That(subject.ActiveOperationId, Is.EqualTo("op-42"));
        }

        [Test]
        public void ActiveOperationType_AfterAcquire_ReturnsOperationType()
        {
            subject.TryAcquire(DatabaseOperationType.Restore, "op-42");

            Assert.That(subject.ActiveOperationType, Is.EqualTo(DatabaseOperationType.Restore));
        }

        [Test]
        public void TryAcquire_TwoBackupsSimultaneously_SecondFails()
        {
            subject.TryAcquire(DatabaseOperationType.Backup, "op-1");

            var result = subject.TryAcquire(DatabaseOperationType.Backup, "op-2");

            Assert.That(result.Acquired, Is.False);
        }
    }
}
