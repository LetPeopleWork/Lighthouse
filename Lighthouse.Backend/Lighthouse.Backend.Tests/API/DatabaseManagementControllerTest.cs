using Lighthouse.Backend.Modules.Access.Adapters.Http;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Language.Flow;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            subject = new DatabaseManagementController(serviceMock.Object, TimeProvider.System);
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

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>().With.Property(nameof(BadRequestObjectResult.Value)).EqualTo("Password is required for backup."));
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
            ServiceRestoreFor("restorePw").ReturnsAsync(opStatus);

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));
            fileMock.Setup(f => f.Length).Returns(3);

            var result = await RestoreThroughTheController(fileMock.Object, "restorePw");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<AcceptedResult>());
                var accepted = result as AcceptedResult;
                Assert.That(accepted!.Value, Is.EqualTo(opStatus));
            }
        }

        [Test]
        public async Task RestoreBackup_StoppedForUnreadableCredentials_SendsTheFailureCodeAsTheTextTheSettingsPageReads()
        {
            var stopped = new DatabaseOperationStatus("op-2", DatabaseOperationType.Restore, DatabaseOperationState.Failed,
                "None of the stored credentials in this backup can be read with this installation's encryption key. Nothing has been changed.",
                DatabaseOperationFailure.CredentialsUnreadable);
            ServiceRestoreFor("restorePw").ReturnsAsync(stopped);

            var result = await RestoreThroughTheController(ABackupFile(), "restorePw");

            using var sent = JsonDocument.Parse(JsonSerializer.Serialize(((AcceptedResult)result).Value, AsTheApiSendsIt));
            Assert.That(sent.RootElement.GetProperty("failureCode").GetString(), Is.EqualTo("CredentialsUnreadable"),
                "The stopped restore must reach the settings page with failureCode \"CredentialsUnreadable\" as text: the page opens " +
                "its unreadable-credentials dialog on exactly that text. Keep the enum member's name and send enums as strings.");
        }

        [Test]
        public async Task RestoreBackup_NoFile_ReturnsBadRequest()
        {
            var result = await RestoreThroughTheController(null!, "pw");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>().With.Property(nameof(BadRequestObjectResult.Value)).EqualTo("A backup file is required."));
        }

        [Test]
        public async Task RestoreBackup_EmptyFile_ReturnsBadRequest()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(0);

            var result = await RestoreThroughTheController(fileMock.Object, "pw");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task RestoreBackup_EmptyPassword_ReturnsBadRequest()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);

            var result = await RestoreThroughTheController(fileMock.Object, "");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>().With.Property(nameof(BadRequestObjectResult.Value)).EqualTo("Password is required for restore."));
        }

        [Test]
        public async Task RestoreBackup_ServiceThrowsInvalidOperation_ReturnsConflict()
        {
            ServiceRestoreFor("pw")
                .ThrowsAsync(new InvalidOperationException("Blocked"));

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));
            fileMock.Setup(f => f.Length).Returns(3);

            var result = await RestoreThroughTheController(fileMock.Object, "pw");

            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        }

        [Test]
        public async Task RestoreBackup_ServiceThrowsArgument_ReturnsBadRequest()
        {
            ServiceRestoreFor("wrongPw")
                .ThrowsAsync(new ArgumentException("Invalid backup or wrong password"));

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));
            fileMock.Setup(f => f.Length).Returns(3);

            var result = await RestoreThroughTheController(fileMock.Object, "wrongPw");

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [TestCase(true, "Remove")]
        [TestCase(false, "StopRestore")]
        [TestCase(null, "StopRestore")]
        public async Task RestoreBackup_PassesTheOperatorsChoiceAboutUnreadableCredentialsToTheService(bool? removeUnreadableCredentials, string expectedChoice)
        {
            var opStatus = new DatabaseOperationStatus("op-2", DatabaseOperationType.Restore, DatabaseOperationState.Executing, null);
            ServiceRestoreFor("restorePw").ReturnsAsync(opStatus);

            var result = await RestoreThroughTheController(ABackupFile(), "restorePw", removeUnreadableCredentials);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<AcceptedResult>());
                Assert.That(ChoiceTheServiceWasGiven(), Is.EqualTo(expectedChoice),
                    $"The form field removeUnreadableCredentials was {removeUnreadableCredentials?.ToString() ?? "absent"}, but the service " +
                    $"was not asked to {expectedChoice}. Only a field set to true may remove the credentials a key cannot read; an absent " +
                    "or false field must stop the restore as before. Read the optional form field and map true to Remove, else StopRestore.");
            }
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

        [Test]
        public void Controller_HasSystemAdminRbacGuardAttribute()
        {
            var attribute = typeof(DatabaseManagementController)
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.SystemAdmin));
        }

        // The web defaults the controllers use, plus the string enum converter the application adds to them at start-up.
        // The application's own options are built inside its start-up and cannot be reached without a host.
        private static readonly JsonSerializerOptions AsTheApiSendsIt = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        private static IFormFile ABackupFile()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([1, 2, 3]));
            fileMock.Setup(f => f.Length).Returns(3);
            return fileMock.Object;
        }

        private ISetup<IDatabaseManagementService, Task<DatabaseOperationStatus>> ServiceRestoreFor(string password)
            => serviceMock.Setup(service => service.RestoreBackup(It.IsAny<Stream>(), password, It.IsAny<UnreadableCredentials>()));

        // A form without the field binds as false, which is the default the controller declares.
        private async Task<IActionResult> RestoreThroughTheController(IFormFile file, string password, bool? removeUnreadableCredentials = null)
        {
            subject.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            return removeUnreadableCredentials is bool remove
                ? await subject.RestoreBackup(file, password, remove)
                : await subject.RestoreBackup(file, password);
        }

        private string ChoiceTheServiceWasGiven()
        {
            var restore = serviceMock.Invocations.Single(invocation => invocation.Method.Name == nameof(IDatabaseManagementService.RestoreBackup));
            return restore.Arguments[2]?.ToString() ?? "(none)";
        }
    }
}
