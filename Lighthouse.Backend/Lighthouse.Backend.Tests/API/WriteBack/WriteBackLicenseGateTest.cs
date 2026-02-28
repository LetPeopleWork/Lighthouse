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
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API.WriteBack
{
    public class WriteBackLicenseGateTest
    {
        private Mock<IWorkTrackingSystemFactory> workTrackingSystemsFactoryMock;
        private Mock<IRepository<WorkTrackingSystemConnection>> repositoryMock;
        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;
        private Mock<ICryptoService> cryptoServiceMock;
        private Mock<ILicenseService> licenseServiceMock;
        private OptionalFeature linearIntegrationPreviewFeature;

        [SetUp]
        public void Setup()
        {
            workTrackingSystemsFactoryMock = new Mock<IWorkTrackingSystemFactory>();
            repositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            cryptoServiceMock = new Mock<ICryptoService>();
            licenseServiceMock = new Mock<ILicenseService>();
            linearIntegrationPreviewFeature = new OptionalFeature { Enabled = true, Id = 12, Key = OptionalFeatureKeys.LinearIntegrationKey };

            cryptoServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns((string input) => input);
        }

        [Test]
        public async Task CreateConnection_WithWriteBackMappings_NoPremium_ReturnsForbidden()
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);

            var subject = CreateSubject();
            var dto = new WorkTrackingSystemConnectionDto
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat,
            };
            dto.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinitionDto
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.WorkItemAge"
            });

            var result = await subject.CreateNewWorkTrackingSystemConnectionAsync(dto);

            using (Assert.EnterMultipleScope())
            {
                var objectResult = result.Result as ObjectResult;
                Assert.That(objectResult, Is.Not.Null);
                Assert.That(objectResult!.StatusCode, Is.EqualTo(403));
            }
        }

        [Test]
        public async Task CreateConnection_WithWriteBackMappings_Premium_Succeeds()
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject();
            var dto = new WorkTrackingSystemConnectionDto
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat,
            };
            dto.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinitionDto
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.WorkItemAge"
            });

            var result = await subject.CreateNewWorkTrackingSystemConnectionAsync(dto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var connection = okResult!.Value as WorkTrackingSystemConnectionDto;

                Assert.That(connection!.WriteBackMappingDefinitions, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task CreateConnection_NoWriteBackMappings_NoPremium_Succeeds()
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);

            var subject = CreateSubject();
            var dto = new WorkTrackingSystemConnectionDto
            {
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat,
            };

            var result = await subject.CreateNewWorkTrackingSystemConnectionAsync(dto);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        private WorkTrackingSystemConnectionsController CreateSubject()
        {
            var optionalFeatureRepositoryMock = new Mock<IRepository<OptionalFeature>>();
            optionalFeatureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>())).Returns(linearIntegrationPreviewFeature);

            return new WorkTrackingSystemConnectionsController(
                workTrackingSystemsFactoryMock.Object, repositoryMock.Object, workTrackingConnectorFactoryMock.Object, cryptoServiceMock.Object, optionalFeatureRepositoryMock.Object, licenseServiceMock.Object);
        }
    }
}
