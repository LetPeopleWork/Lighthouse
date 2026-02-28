using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API.WriteBack
{
    public class WorkTrackingSystemConnectionControllerWriteBackTest
    {
        private Mock<IRepository<WorkTrackingSystemConnection>> repositoryMock;
        private Mock<ILicenseService> licenseServiceMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
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
                Assert.That(connection.WriteBackMappingDefinitions[0].ValueSource, Is.EqualTo(WriteBackValueSource.WorkItemAge));
                Assert.That(connection.WriteBackMappingDefinitions[0].TargetFieldReference, Is.EqualTo("Custom.WorkItemAge"));
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
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.WorkItemAge"
            });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();
            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Option", Value = "Value" });
            connectionDto.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinitionDto
            {
                Id = 99,
                ValueSource = WriteBackValueSource.CycleTime,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.CycleTime",
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
                Assert.That(connection.WriteBackMappingDefinitions[0].ValueSource, Is.EqualTo(WriteBackValueSource.CycleTime));
                Assert.That(connection.WriteBackMappingDefinitions[0].AppliesTo, Is.EqualTo(WriteBackAppliesTo.Portfolio));
                Assert.That(connection.WriteBackMappingDefinitions[0].TargetFieldReference, Is.EqualTo("Custom.CycleTime"));
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
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.WorkItemAge"
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
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "",
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
                TargetFieldReference = "Custom.Forecast85",
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
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.WorkItemAge",
            });
            return dto;
        }

        private WorkTrackingSystemConnectionController CreateSubject()
        {
            return new WorkTrackingSystemConnectionController(repositoryMock.Object, licenseServiceMock.Object);
        }
    }
}
