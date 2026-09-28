using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API.WriteBack
{
    public class WorkTrackingSystemConnectionControllerWriteBackTest
    {
        private Mock<IRepository<WorkTrackingSystemConnection>> repositoryMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var connectorMock = new Mock<IWorkTrackingConnector>();
            connectorMock.Setup(c => c.GetPredefinedAdditionalFields(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns([]);
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            workTrackingConnectorFactoryMock.Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connectorMock.Object);
        }

        [Test]
        public async Task UpdateConnection_WithWriteBackMappings_PersistsMappings()
        {
            var existingConnection = CreateExistingConnection();
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();
            var connectionDto = CreateConnectionDtoWithWriteBackMapping(12);

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var connection = okResult!.Value as WorkTrackingSystemConnectionDto;

                Assert.That(connection!.WriteBackMappingDefinitions, Has.Count.EqualTo(1));
                Assert.That(connection.WriteBackMappingDefinitions[0].ValueSource, Is.EqualTo(WriteBackValueSource.WorkItemAgeCycleTime));
                Assert.That(connection.WriteBackMappingDefinitions[0].AdditionalFieldDefinitionId, Is.EqualTo(1));
            }

            repositoryMock.Verify(x => x.Update(It.IsAny<WorkTrackingSystemConnection>()));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task UpdateConnection_ExistingMapping_UpdatesInPlace()
        {
            var existingConnection = CreateExistingConnection();
            existingConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                Id = 99,
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = 1
            });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();
            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Option", Value = "Value" });
            connectionDto.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinitionDto
            {
                Id = 99,
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                AdditionalFieldDefinitionId = 2,
                TargetValueType = WriteBackTargetValueType.FormattedText,
                DateFormat = "yyyy-MM-dd"
            });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var connection = okResult!.Value as WorkTrackingSystemConnectionDto;

                Assert.That(connection!.WriteBackMappingDefinitions, Has.Count.EqualTo(1));
                Assert.That(connection.WriteBackMappingDefinitions[0].ValueSource, Is.EqualTo(WriteBackValueSource.WorkItemAgeCycleTime));
                Assert.That(connection.WriteBackMappingDefinitions[0].AppliesTo, Is.EqualTo(WriteBackAppliesTo.Portfolio));
                Assert.That(connection.WriteBackMappingDefinitions[0].AdditionalFieldDefinitionId, Is.EqualTo(2));
                Assert.That(connection.WriteBackMappingDefinitions[0].DateFormat, Is.EqualTo("yyyy-MM-dd"));
            }
        }

        [Test]
        public async Task UpdateConnection_RemovedMapping_RemovesMappingFromModel()
        {
            var existingConnection = CreateExistingConnection();
            existingConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                Id = 99,
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = 1
            });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();
            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Option", Value = "Value" });
            // Sending empty write-back mappings = remove all existing

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var connection = okResult!.Value as WorkTrackingSystemConnectionDto;

                Assert.That(connection!.WriteBackMappingDefinitions, Is.Empty);
            }
        }

        [Test]
        public async Task UpdateConnection_NewMappingToFieldAddedInSameSave_ReferencesTheAddedField()
        {
            var existingConnection = CreateExistingConnection();
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);
            WorkTrackingSystemConnection? updated = null;
            repositoryMock.Setup(x => x.Update(It.IsAny<WorkTrackingSystemConnection>()))
                .Callback<WorkTrackingSystemConnection>(c => updated = c);

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = -1, DisplayName = "New", Reference = "customfield_1" });
            connectionDto.WriteBackMappingDefinitions.Add(MappingDto(id: -1, fieldId: -1));

            var result = await CreateSubject().UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var addedField = updated!.AdditionalFieldDefinitions.Single(f => f.Reference == "customfield_1");
            var mapping = updated.WriteBackMappingDefinitions.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(mapping.AdditionalFieldDefinition, Is.SameAs(addedField));
                Assert.That(mapping.AdditionalFieldDefinitionId, Is.Null);
            }
        }

        [Test]
        public async Task UpdateConnection_ExistingMappingMovedToFieldAddedInSameSave_ReferencesTheAddedField()
        {
            var existingConnection = CreateExistingConnection();
            existingConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 1, DisplayName = "Old", Reference = "customfield_old" });
            existingConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                Id = 99,
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = 1
            });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = 1, DisplayName = "Old", Reference = "customfield_old" });
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = -2, DisplayName = "New", Reference = "customfield_1" });
            connectionDto.WriteBackMappingDefinitions.Add(MappingDto(id: 99, fieldId: -2));

            var result = await CreateSubject().UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var addedField = existingConnection.AdditionalFieldDefinitions.Single(f => f.Reference == "customfield_1");
            Assert.That(existingConnection.WriteBackMappingDefinitions.Single().AdditionalFieldDefinition, Is.SameAs(addedField));
        }

        [Test]
        public async Task UpdateConnection_MappingToUnknownUnsavedField_ReturnsBadRequest()
        {
            var existingConnection = CreateExistingConnection();
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = -1, DisplayName = "New", Reference = "customfield_1" });
            connectionDto.WriteBackMappingDefinitions.Add(MappingDto(id: -1, fieldId: -7));

            var result = await CreateSubject().UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                repositoryMock.Verify(x => x.Save(), Times.Never());
            }
        }

        [Test]
        public async Task UpdateConnection_ExistingMappingMovedToUnknownUnsavedField_ReturnsBadRequest()
        {
            var existingConnection = CreateExistingConnection();
            existingConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                Id = 99,
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = 1
            });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.WriteBackMappingDefinitions.Add(MappingDto(id: 99, fieldId: -7));

            var result = await CreateSubject().UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                repositoryMock.Verify(x => x.Save(), Times.Never());
            }
        }

        private static WriteBackMappingDefinitionDto MappingDto(int id, int fieldId)
            => new()
            {
                Id = id,
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = fieldId,
            };

        private WorkTrackingSystemConnection CreateExistingConnection()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Connection" };
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option", Value = "Value" });
            return connection;
        }

        [Test]
        public async Task UpdateConnection_InvalidWriteBackMappings_MissingTargetFieldReference_ReturnsBadRequest()
        {
            var existingConnection = CreateExistingConnection();
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();
            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Option", Value = "Value" });
            connectionDto.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinitionDto
            {
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = null,
            });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                repositoryMock.Verify(x => x.Save(), Times.Never());
            }
        }

        [Test]
        public async Task UpdateConnection_InvalidWriteBackMappings_FormattedTextWithoutDateFormat_ReturnsBadRequest()
        {
            var existingConnection = CreateExistingConnection();
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();
            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Option", Value = "Value" });
            connectionDto.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinitionDto
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                AdditionalFieldDefinitionId = 1,
                TargetValueType = WriteBackTargetValueType.FormattedText,
                DateFormat = null
            });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                repositoryMock.Verify(x => x.Save(), Times.Never());
            }
        }

        private WorkTrackingSystemConnectionDto CreateConnectionDtoWithWriteBackMapping(int id)
        {
            var dto = new WorkTrackingSystemConnectionDto { Id = id, Name = "Connection" };
            dto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Option", Value = "Value" });
            dto.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinitionDto
            {
                ValueSource = WriteBackValueSource.WorkItemAgeCycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                AdditionalFieldDefinitionId = 1,
            });
            return dto;
        }

        private WorkTrackingSystemConnectionController CreateSubject()
        {
            return new WorkTrackingSystemConnectionController(repositoryMock.Object, teamRepositoryMock.Object, portfolioRepositoryMock.Object, licenseServiceMock.Object, workTrackingConnectorFactoryMock.Object);
        }
    }
}
