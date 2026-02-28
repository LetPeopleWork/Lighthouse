using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class WorkTrackingSystemConnectionsControllerTest
    {
        private Mock<IWorkTrackingSystemFactory> workTrackingSystemsFactoryMock;

        private Mock<IRepository<WorkTrackingSystemConnection>> repositoryMock;

        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;

        private Mock<ICryptoService> cryptoServiceMock;

        private OptionalFeature linearIntegrationPreviewFeature;

        private Mock<ILicenseService> licenseServiceMock;

        [SetUp]
        public void Setup()
        {
            workTrackingSystemsFactoryMock = new Mock<IWorkTrackingSystemFactory>();
            repositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            cryptoServiceMock = new Mock<ICryptoService>();
            licenseServiceMock = new Mock<ILicenseService>();

            cryptoServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns((string input) => input);
            cryptoServiceMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns((string input) => input);

            linearIntegrationPreviewFeature = new OptionalFeature { Enabled = true, Id = 12, Key = OptionalFeatureKeys.LinearIntegrationKey };

            workTrackingSystemsFactoryMock.Setup(x => x.CreateDefaultConnectionForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns((WorkTrackingSystems s) => new WorkTrackingSystemConnection { WorkTrackingSystem = s });
        }

        [Test]
        [TestCase(WorkTrackingSystems.Jira, WorkTrackingSystems.AzureDevOps, WorkTrackingSystems.Linear, WorkTrackingSystems.Csv)]
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

                Assert.That(supportedSystems?.Count(), Is.EqualTo(Enum.GetValues<WorkTrackingSystems>().Length));
            }

            workTrackingSystemsFactoryMock.Verify(x => x.CreateDefaultConnectionForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>()), Times.Exactly(Enum.GetValues<WorkTrackingSystems>().Length));
        }

        [Test]
        public void GetSupportedWorkTrackingSystems_LinearIntegrationOff_SkipsLinearFromAvailableSystems()
        {
            linearIntegrationPreviewFeature.Enabled = false;

            var subject = CreateSubject();

            var result = subject.GetSupportedWorkTrackingSystemConnections();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var supportedSystems = okResult.Value as IEnumerable<WorkTrackingSystemConnectionDto>;

                var expected = Enum.GetValues<WorkTrackingSystems>().Length - 1;
                Assert.That(supportedSystems?.Count(), Is.EqualTo(expected));
            }
        }

        [Test]
        public void GetWorkTrackingSystemConnections_ReturnsAllConfiguredConnection()
        {
            var expectedConnections = new List<WorkTrackingSystemConnection>
            {
                new(),
                new()
            };

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
                WorkTrackingSystem = WorkTrackingSystems.Csv,
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
                Assert.That(connection.WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Csv));
                Assert.That(connection.Options, Has.Count.EqualTo(1));
                Assert.That(connection.Options.Single().Key, Is.EqualTo("MyKey"));
                Assert.That(connection.Options.Single().Value, Is.EqualTo("MyValue"));
                Assert.That(connection.Options.Single().IsSecret, Is.False);
            }

            repositoryMock.Verify(x => x.Add(It.IsAny<WorkTrackingSystemConnection>()));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        [TestCase(WorkTrackingSystems.AzureDevOps)]
        [TestCase(WorkTrackingSystems.Jira)]
        public async Task ValidateConnection_InvokesWorkItemService_ReturnsResult(WorkTrackingSystems workTrackingSystem)
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            var subject = CreateSubject();

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingConnectorServiceMock.Setup(x => x.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>())).ReturnsAsync(true);

            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(workTrackingSystem)).Returns(workTrackingConnectorServiceMock.Object);

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection", WorkTrackingSystem = workTrackingSystem };
            AddAdditionalFieldDefinitionToDto(connectionDto, 2);
            
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
        public async Task ValidateConnection_MoreThanOneAdditionalField_FailsIfNoPremium()
        {
            var subject = CreateSubject();

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingConnectorServiceMock.Setup(x => x.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>())).ReturnsAsync(true);
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(WorkTrackingSystems.Jira)).Returns(workTrackingConnectorServiceMock.Object);
            
            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            AddAdditionalFieldDefinitionToDto(connectionDto, 2);
                
            var result = await subject.ValidateConnection(connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ActionResult<bool>>());
                var objectResult = result.Result as ObjectResult;

                Assert.That(objectResult, Is.Not.Null);
                Assert.That(objectResult.StatusCode, Is.EqualTo(403));
                Assert.That(objectResult.Value, Is.False);
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
            cryptoServiceMock.Setup(x => x.Decrypt("SecretValue")).Throws<Exception>(); // Not already encrypted

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 0, Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Key", Value = "SecretValue", IsSecret = true });            

            await subject.ValidateConnection(connectionDto);

            workTrackingConnectorServiceMock.Verify(x => x.ValidateConnection(It.Is<WorkTrackingSystemConnection>(c => c.Options.Single().Value == "EncryptedSecret")));
        }

        [Test]
        public async Task ValidateConnection_ExistingConnectionWithEmptySecret_FetchesFromDatabase()
        {
            var existingConnection = new WorkTrackingSystemConnection { Id = 12, Name = "Connection" };
            existingConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "apiToken", Value = "ExistingEncryptedSecret", IsSecret = true });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingConnectorServiceMock.Setup(x => x.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>())).ReturnsAsync(true);
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(workTrackingConnectorServiceMock.Object);

            cryptoServiceMock.Setup(x => x.Decrypt("ExistingEncryptedSecret")).Returns("ExistingEncryptedSecret"); // Already encrypted

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "apiToken", Value = "", IsSecret = true });

            await subject.ValidateConnection(connectionDto);

            // Verify that the validation used the existing encrypted secret from DB
            workTrackingConnectorServiceMock.Verify(x => x.ValidateConnection(
                It.Is<WorkTrackingSystemConnection>(c => c.Options.Single(o => o.Key == "apiToken").Value == "ExistingEncryptedSecret")));
        }

        [Test]
        public async Task ValidateConnection_NewConnectionWithSecret_EncryptsNewValue()
        {
            var subject = CreateSubject();

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingConnectorServiceMock.Setup(x => x.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>())).ReturnsAsync(true);
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(workTrackingConnectorServiceMock.Object);

            cryptoServiceMock.Setup(x => x.Encrypt("NewSecretValue")).Returns("EncryptedNewSecret");
            cryptoServiceMock.Setup(x => x.Decrypt("NewSecretValue")).Throws<Exception>(); // Not encrypted yet

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 0, Name = "New Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "apiToken", Value = "NewSecretValue", IsSecret = true });

            await subject.ValidateConnection(connectionDto);

            // Verify that the new secret was encrypted
            workTrackingConnectorServiceMock.Verify(x => x.ValidateConnection(
                It.Is<WorkTrackingSystemConnection>(c => c.Options.Single(o => o.Key == "apiToken").Value == "EncryptedNewSecret")));
        }

        [Test]
        public async Task CreateNewWorkTrackingSystemConnection_WithAuthMethodKey_PreservesKey()
        {
            var newConnectionDto = new WorkTrackingSystemConnectionDto
            {
                Name = "Jira Cloud Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            var subject = CreateSubject();

            var result = await subject.CreateNewWorkTrackingSystemConnectionAsync(newConnectionDto);

            var okResult = result.Result as OkObjectResult;
            var connection = okResult?.Value as WorkTrackingSystemConnectionDto;

            Assert.That(connection?.AuthenticationMethodKey, Is.EqualTo(AuthenticationMethodKeys.JiraCloud));
        }

        [Test]
        [TestCase(WorkTrackingSystems.AzureDevOps, AuthenticationMethodKeys.AzureDevOpsPat)]
        [TestCase(WorkTrackingSystems.Linear, AuthenticationMethodKeys.LinearApiKey)]
        [TestCase(WorkTrackingSystems.Csv, AuthenticationMethodKeys.None)]
        [TestCase(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraCloud)]
        public async Task CreateNewWorkTrackingSystemConnection_WithAuthMethodKey_PreservesKeyForAllProviders(
            WorkTrackingSystems system, string authMethodKey)
        {
            var newConnectionDto = new WorkTrackingSystemConnectionDto
            {
                Name = "Connection",
                WorkTrackingSystem = system,
                AuthenticationMethodKey = authMethodKey,
            };
            
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            AddAdditionalFieldDefinitionToDto(newConnectionDto, 2);

            var subject = CreateSubject();

            var result = await subject.CreateNewWorkTrackingSystemConnectionAsync(newConnectionDto);

            var okResult = result.Result as OkObjectResult;
            var connection = okResult?.Value as WorkTrackingSystemConnectionDto;

            Assert.That(connection?.AuthenticationMethodKey, Is.EqualTo(authMethodKey));
        }

        [Test] public async Task CreateNewWorkTrackingSystemConnection_MoreThanOneAdditionalField_NoPremium_Fails()
        {
            var newConnectionDto = new WorkTrackingSystemConnectionDto
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            var subject = CreateSubject();
            
            AddAdditionalFieldDefinitionToDto(newConnectionDto, 2);

            var result = await subject.CreateNewWorkTrackingSystemConnectionAsync(newConnectionDto);
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ActionResult<WorkTrackingSystemConnectionDto>>());
                var objectResult = result.Result as ObjectResult;

                Assert.That(objectResult, Is.Not.Null);
                Assert.That(objectResult.StatusCode, Is.EqualTo(403));
                Assert.That(objectResult.Value, Is.Null);
            }
        }

        [Test]
        public async Task CreateNewWorkTrackingSystemConnection_InvalidWriteBackMappings_ReturnsBadRequest()
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            var newConnectionDto = new WorkTrackingSystemConnectionDto
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };
            newConnectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "MyKey", Value = "MyValue" });
            newConnectionDto.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinitionDto
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Forecast85",
                TargetValueType = WriteBackTargetValueType.FormattedText,
                DateFormat = null
            });

            var subject = CreateSubject();

            var result = await subject.CreateNewWorkTrackingSystemConnectionAsync(newConnectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                repositoryMock.Verify(x => x.Save(), Times.Never());
            }
        }

        private WorkTrackingSystemConnectionsController CreateSubject()
        {
            var optionalFeatureRepositoryMock = new Mock<IRepository<OptionalFeature>>();
            optionalFeatureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns(linearIntegrationPreviewFeature);

            return new WorkTrackingSystemConnectionsController(
                workTrackingSystemsFactoryMock.Object, repositoryMock.Object, workTrackingConnectorFactoryMock.Object, cryptoServiceMock.Object, optionalFeatureRepositoryMock.Object, licenseServiceMock.Object);
        }

        private void AddAdditionalFieldDefinitionToDto(WorkTrackingSystemConnectionDto connectionDto,
            int additionalFieldCount)
        {
            for (var count = 0; count < additionalFieldCount; count++)
            {
                connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto());
            }
        }
    }
}
