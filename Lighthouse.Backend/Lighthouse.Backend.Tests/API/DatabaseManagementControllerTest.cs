using Lighthouse.Backend.API;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class DatabaseManagementControllerTest
    {
        private Mock<IDatabaseManagementService> serviceMock;
        private DatabaseManagementController subject;

        [SetUp]
        public void SetUp()
        {
            serviceMock = new Mock<IDatabaseManagementService>();
            subject = new DatabaseManagementController(serviceMock.Object);
        }

        [Test]
        public void GetStatus_ReturnsOkWithCapabilityStatus()
        {
            var status = new DatabaseCapabilityStatus(
                "sqlite",
                false,
                null,
                true,
                null,
                null,
                null);
            serviceMock.Setup(s => s.GetCapabilityStatus()).Returns(status);

            var result = subject.GetStatus();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult!.Value, Is.EqualTo(status));
            }
        }

        [Test]
        public void GetStatus_WhenOperationBlocked_ReturnsBlockedStatus()
        {
            var activeOp = new DatabaseOperationStatus("op-1", DatabaseOperationType.Backup, DatabaseOperationState.Executing, null);
            var status = new DatabaseCapabilityStatus(
                "sqlite",
                true,
                "A backup is currently in progress",
                true,
                null,
                null,
                activeOp);
            serviceMock.Setup(s => s.GetCapabilityStatus()).Returns(status);

            var result = subject.GetStatus();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var returnedStatus = okResult!.Value as DatabaseCapabilityStatus;
                Assert.That(returnedStatus!.IsOperationBlocked, Is.True);
                Assert.That(returnedStatus.ActiveOperation, Is.Not.Null);
            }
        }

        [Test]
        public async Task CreateBackup_ValidPassword_ReturnsAcceptedWithStatus()
        {
            var opStatus = new DatabaseOperationStatus("op-1", DatabaseOperationType.Backup, DatabaseOperationState.Executing, null);
            serviceMock.Setup(s => s.CreateBackup("myPassword")).ReturnsAsync(opStatus);

            var result = await subject.CreateBackup(new DatabaseManagementController.BackupRequest("myPassword"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<AcceptedResult>());
                var accepted = result as AcceptedResult;
                Assert.That(accepted!.Value, Is.EqualTo(opStatus));
            }
        }

        [Test]
        public async Task CreateBackup_EmptyPassword_ReturnsBadRequest()
        {
            var result = await subject.CreateBackup(new DatabaseManagementController.BackupRequest(""));

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateBackup_NullPassword_ReturnsBadRequest()
        {
            var result = await subject.CreateBackup(new DatabaseManagementController.BackupRequest(null!));

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateBackup_ServiceThrowsInvalidOperation_ReturnsConflict()
        {
            serviceMock.Setup(s => s.CreateBackup("pw")).ThrowsAsync(new InvalidOperationException("Operation blocked"));

            var result = await subject.CreateBackup(new DatabaseManagementController.BackupRequest("pw"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
                var conflict = result as ConflictObjectResult;
                Assert.That(conflict!.Value!.ToString(), Does.Contain("Operation blocked"));
            }
        }

        [Test]
        public void GetBackupArtifact_ValidOperationId_ReturnsFile()
        {
            var stream = new MemoryStream([0x50, 0x4B, 0x03, 0x04]);
            serviceMock.Setup(s => s.GetBackupArtifact("op-1")).Returns(stream);

            var result = subject.GetBackupArtifact("op-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<FileStreamResult>());
                var fileResult = result as FileStreamResult;
                Assert.That(fileResult!.ContentType, Is.EqualTo("application/zip"));
                Assert.That(fileResult.FileDownloadName, Does.StartWith("Lighthouse_Backup_"));
                Assert.That(fileResult.FileDownloadName, Does.EndWith(".zip"));
            }
        }

        [Test]
        public void GetBackupArtifact_InvalidOperationId_ReturnsNotFound()
        {
            serviceMock.Setup(s => s.GetBackupArtifact("bad-id")).Throws(new KeyNotFoundException("Not found"));

            var result = subject.GetBackupArtifact("bad-id");

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task RestoreBackup_ValidFileAndPassword_ReturnsAccepted()
        {
            var opStatus = new DatabaseOperationStatus("op-2", DatabaseOperationType.Restore, DatabaseOperationState.Executing, null);
            serviceMock.Setup(s => s.RestoreBackup(It.IsAny<Stream>(), "restorePw")).ReturnsAsync(opStatus);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));
            fileMock.Setup(f => f.Length).Returns(3);

            var result = await subject.RestoreBackup(fileMock.Object, "restorePw");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<AcceptedResult>());
                var accepted = result as AcceptedResult;
                Assert.That(accepted!.Value, Is.EqualTo(opStatus));
            }
        }

        [Test]
        public async Task RestoreBackup_NoFile_ReturnsBadRequest()
        {
            var result = await subject.RestoreBackup(null!, "pw");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task RestoreBackup_EmptyFile_ReturnsBadRequest()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            var result = await subject.RestoreBackup(fileMock.Object, "pw");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task RestoreBackup_EmptyPassword_ReturnsBadRequest()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);

            var result = await subject.RestoreBackup(fileMock.Object, "");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task RestoreBackup_ServiceThrowsInvalidOperation_ReturnsConflict()
        {
            serviceMock.Setup(s => s.RestoreBackup(It.IsAny<Stream>(), "pw"))
                .ThrowsAsync(new InvalidOperationException("Blocked"));

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));
            fileMock.Setup(f => f.Length).Returns(3);

            var result = await subject.RestoreBackup(fileMock.Object, "pw");

            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        }

        [Test]
        public async Task RestoreBackup_ServiceThrowsArgument_ReturnsBadRequest()
        {
            serviceMock.Setup(s => s.RestoreBackup(It.IsAny<Stream>(), "wrongPw"))
                .ThrowsAsync(new ArgumentException("Invalid backup or wrong password"));

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));
            fileMock.Setup(f => f.Length).Returns(3);

            var result = await subject.RestoreBackup(fileMock.Object, "wrongPw");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task ClearDatabase_ReturnsAcceptedWithStatus()
        {
            var opStatus = new DatabaseOperationStatus("op-3", DatabaseOperationType.Clear, DatabaseOperationState.Executing, null);
            serviceMock.Setup(s => s.ClearDatabase()).ReturnsAsync(opStatus);

            var result = await subject.ClearDatabase();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<AcceptedResult>());
                var accepted = result as AcceptedResult;
                Assert.That(accepted!.Value, Is.EqualTo(opStatus));
            }
        }

        [Test]
        public async Task ClearDatabase_ServiceThrowsInvalidOperation_ReturnsConflict()
        {
            serviceMock.Setup(s => s.ClearDatabase()).ThrowsAsync(new InvalidOperationException("Background work active"));

            var result = await subject.ClearDatabase();

            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        }

        [Test]
        public void GetOperationStatus_KnownOperation_ReturnsOkWithStatus()
        {
            var opStatus = new DatabaseOperationStatus("op-1", DatabaseOperationType.Backup, DatabaseOperationState.Completed, null);
            serviceMock.Setup(s => s.GetOperationStatus("op-1")).Returns(opStatus);

            var result = subject.GetOperationStatus("op-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult!.Value, Is.EqualTo(opStatus));
            }
        }

        [Test]
        public void GetOperationStatus_UnknownOperation_ReturnsNotFound()
        {
            serviceMock.Setup(s => s.GetOperationStatus("unknown")).Returns((DatabaseOperationStatus?)null);

            var result = subject.GetOperationStatus("unknown");

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }
    }
}
