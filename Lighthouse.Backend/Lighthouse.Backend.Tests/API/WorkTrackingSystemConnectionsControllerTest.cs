using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework.Internal;

namespace Lighthouse.Backend.Tests.API
{
    public class WorkTrackingSystemConnectionsControllerTest
    {
        private Mock<IWorkTrackingSystemFactory> workTrackingSystemsFactoryMock;

        private Mock<IRepository<WorkTrackingSystemConnection>> repositoryMock;

        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;

        private Mock<ICryptoService> cryptoServiceMock;

        private OptionalFeature linearIntegreationPreviewFeature;

        [SetUp]
        public void Setup()
        {
            workTrackingSystemsFactoryMock = new Mock<IWorkTrackingSystemFactory>();
            repositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            cryptoServiceMock = new Mock<ICryptoService>();

            cryptoServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns((string input) => { return input; });
            cryptoServiceMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns((string input) => { return input; });

            linearIntegreationPreviewFeature = new OptionalFeature { Enabled = true, Id = 12, Key = OptionalFeatureKeys.LinearIntegrationKey };

            workTrackingSystemsFactoryMock.Setup(x => x.CreateDefaultConnectionForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns((WorkTrackingSystems s) => new WorkTrackingSystemConnection { WorkTrackingSystem = s });
        }

        [Test]
        [TestCase(WorkTrackingSystems.Jira, WorkTrackingSystems.AzureDevOps, WorkTrackingSystems.Linear)]
        public void GetSupportedWorkTrackingSystems_ReturnsDefaultSystemsFromFactory(params WorkTrackingSystems[] workTrackingSystems)
        {
            foreach (var workTrackingSystem in workTrackingSystems)
            {
                workTrackingSystemsFactoryMock.Setup(x => x.CreateDefaultConnectionForWorkTrackingSystem(workTrackingSystem)).Returns(new WorkTrackingSystemConnection());
            }

            var subject = CreateSubject();

            var result = subject.GetSupportedWorkTrackingSystemConnections();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var supportedSystems = okResult.Value as IEnumerable<WorkTrackingSystemConnectionDto>;

                // CSV is a built-in connector and is excluded from the supported list
                Assert.That(supportedSystems?.Count(), Is.EqualTo(Enum.GetValues<WorkTrackingSystems>().Length - 1));
            }

            workTrackingSystemsFactoryMock.Verify(x => x.CreateDefaultConnectionForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>()), Times.Exactly(Enum.GetValues<WorkTrackingSystems>().Length - 1));
        }

        [Test]
        public void GetSupportedWorkTrackingSystems_ExcludesCsvBuiltin()
        {
            var subject = CreateSubject();

            var result = subject.GetSupportedWorkTrackingSystemConnections();

            var okResult = result.Result as OkObjectResult;
            var supportedSystems = okResult.Value as IEnumerable<WorkTrackingSystemConnectionDto>;

            Assert.That(supportedSystems?.Any(s => s.WorkTrackingSystem == WorkTrackingSystems.Csv), Is.False);
        }

        [Test]
        public void GetSupportedWorkTrackingSystems_LinearIntegrationOff_SkipsLinearFromAvailableSystems()
        {
            linearIntegreationPreviewFeature.Enabled = false;

            var subject = CreateSubject();

            var result = subject.GetSupportedWorkTrackingSystemConnections();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var supportedSystems = okResult.Value as IEnumerable<WorkTrackingSystemConnectionDto>;

                // CSV excluded and Linear disabled
                var expected = Enum.GetValues<WorkTrackingSystems>().Length - 2;
                Assert.That(supportedSystems?.Count(), Is.EqualTo(expected));
            }
        }

        [Test]
        public void GetWorkTrackingSystemConnections_ReturnsAllConfiguredConnection()
        {
            var expectedConnections = new List<WorkTrackingSystemConnection>();
            expectedConnections.Add(new WorkTrackingSystemConnection());
            expectedConnections.Add(new WorkTrackingSystemConnection());

            repositoryMock.Setup(x => x.GetAll()).Returns(expectedConnections);

            var subject = CreateSubject();

            var result = subject.GetWorkTrackingSystemConnections();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var connections = okResult.Value as IEnumerable<WorkTrackingSystemConnectionDto>;

                Assert.That(connections?.Count(), Is.EqualTo(expectedConnections.Count));
            }
        }

        [Test]
        public async Task CreateNewWorkTrackingSystemConnection_GivenConnectionDto_CreatesAndSavesNewConnectionAsync()
        {
            var newConnectionDto = new WorkTrackingSystemConnectionDto
            {
                Name = "Test",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };
            newConnectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "MyKey", Value = "MyValue", IsSecret = false });

            var subject = CreateSubject();

            var result = await subject.CreateNewWorkTrackingSystemConnectionAsync(newConnectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var connection = okResult.Value as WorkTrackingSystemConnectionDto;
                Assert.That(connection.Name, Is.EqualTo("Test"));
                Assert.That(connection.WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Jira));
                Assert.That(connection.Options, Has.Count.EqualTo(1));
                Assert.That(connection.Options.Single().Key, Is.EqualTo("MyKey"));
                Assert.That(connection.Options.Single().Value, Is.EqualTo("MyValue"));
                Assert.That(connection.Options.Single().IsSecret, Is.False);
            }

            repositoryMock.Verify(x => x.Add(It.IsAny<WorkTrackingSystemConnection>()));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task CreateNewWorkTrackingSystemConnection_GivenCsv_ReturnsBadRequest()
        {
            var newConnectionDto = new WorkTrackingSystemConnectionDto
            {
                Name = "CSV",
                WorkTrackingSystem = WorkTrackingSystems.Csv,
            };

            var subject = CreateSubject();

            var result = await subject.CreateNewWorkTrackingSystemConnectionAsync(newConnectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            }
        }

        [Test]
        public async Task UpdateWorkTrackingSystemConnection_ConnectionExists_SavesChangesAsync()
        {
            var existingConnection = new WorkTrackingSystemConnection { Name = "Boring Old Name" };
            existingConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option", Value = "Old Option Value" });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Fancy New Name" };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Option", Value = "Nobody expects the Spanish Inquisition" });
            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;

                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                var connection = okResult.Value as WorkTrackingSystemConnectionDto;

                Assert.That(connection.Name, Is.EqualTo("Fancy New Name"));
                Assert.That(connection.Options, Has.Count.EqualTo(1));
                Assert.That(connection.Options.Single().Value, Is.EqualTo("Nobody expects the Spanish Inquisition"));
            }

            repositoryMock.Verify(x => x.Update(It.IsAny<WorkTrackingSystemConnection>()));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task UpdateWorkTrackingSystemConnection_ConnectionDoesNotExist_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, new WorkTrackingSystemConnectionDto());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = result.Result as NotFoundResult;

                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task DeleteWorkTrackingSystemConnection_ConnectionExists_DeletesAsync()
        {
            repositoryMock.Setup(x => x.Exists(12)).Returns(true);

            var subject = CreateSubject();

            var result = await subject.DeleteWorkTrackingSystemConnectionAsync(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkResult>());
                var okResult = result as OkResult;

                Assert.That(okResult.StatusCode, Is.EqualTo(200));
            }

            repositoryMock.Verify(x => x.Remove(12));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task DeleteWorkTrackingSystemConnection_ConnectionDoesNotExist_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.DeleteWorkTrackingSystemConnectionAsync(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = result as NotFoundResult;

                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task DeleteWorkTrackingSystemConnection_CsvConnection_ReturnsBadRequest()
        {
            var csvConnection = new WorkTrackingSystemConnection { Id = 12, Name = "CSV", WorkTrackingSystem = WorkTrackingSystems.Csv };
            repositoryMock.Setup(x => x.Exists(12)).Returns(true);
            repositoryMock.Setup(x => x.GetById(12)).Returns(csvConnection);

            var subject = CreateSubject();

            var result = await subject.DeleteWorkTrackingSystemConnectionAsync(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            }

            // Verify that Remove and Save were never called
            repositoryMock.Verify(x => x.Remove(12), Times.Never);
            repositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        [TestCase(WorkTrackingSystems.AzureDevOps)]
        [TestCase(WorkTrackingSystems.Jira)]
        public async Task ValidateConnection_InvokesWorkItemService_ReturnsResult(WorkTrackingSystems workTrackingSystem)
        {
            var subject = CreateSubject();

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingConnectorServiceMock.Setup(x => x.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>())).ReturnsAsync(true);

            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(workTrackingSystem)).Returns(workTrackingConnectorServiceMock.Object);

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection", WorkTrackingSystem = workTrackingSystem };
            var result = await subject.ValidateConnection(connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ActionResult<bool>>());
                var okResult = result.Result as OkObjectResult;

                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.True);
            }
        }

        [Test]
        public async Task ValidateConnection_HasSecretConnectionOption_Encrypts()
        {
            var subject = CreateSubject();

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingConnectorServiceMock.Setup(x => x.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>())).ReturnsAsync(true);
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(workTrackingConnectorServiceMock.Object);

            cryptoServiceMock.Setup(x => x.Encrypt("SecretValue")).Returns("EncryptedSecret");

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Key", Value = "SecretValue", IsSecret = true });            

            await subject.ValidateConnection(connectionDto);

            workTrackingConnectorServiceMock.Verify(x => x.ValidateConnection(It.Is<WorkTrackingSystemConnection>(c => c.Options.Single().Value == "EncryptedSecret")));
        }

        private WorkTrackingSystemConnectionsController CreateSubject()
        {
            var optionalFeatureRepositoryMock = new Mock<IRepository<OptionalFeature>>();
            optionalFeatureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns(linearIntegreationPreviewFeature);

            return new WorkTrackingSystemConnectionsController(workTrackingSystemsFactoryMock.Object, repositoryMock.Object, workTrackingConnectorFactoryMock.Object, cryptoServiceMock.Object, optionalFeatureRepositoryMock.Object);
        }
    }
}
