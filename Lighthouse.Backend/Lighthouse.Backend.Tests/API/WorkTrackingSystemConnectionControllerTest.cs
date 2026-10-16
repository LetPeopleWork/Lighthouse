using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Globalization;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Tests.API
{
    public class WorkTrackingSystemConnectionControllerTest
    {
        private Mock<IRepository<WorkTrackingSystemConnection>> repositoryMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;
        private Mock<ILogger<WorkTrackingSystemConnectionController>> loggerMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            licenseServiceMock = new Mock<ILicenseService>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            loggerMock = new Mock<ILogger<WorkTrackingSystemConnectionController>>();

            var connectorMock = new Mock<IWorkTrackingConnector>();
            connectorMock.Setup(c => c.GetPredefinedAdditionalFields(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns([]);
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            workTrackingConnectorFactoryMock.Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connectorMock.Object);
        }

        [Test]
        public async Task GetWorkTrackingSystemConnection_ConnectionExists_ReturnsConnection()
        {
            var existingConnection = new WorkTrackingSystemConnection
            {
                Id = 12,
                Name = "ADO",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();

            var result = await subject.GetWorkTrackingSystemConnection(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult?.StatusCode, Is.EqualTo(200));

                Assert.That(okResult?.Value, Is.InstanceOf<WorkTrackingSystemConnectionDto>());
                var dto = okResult?.Value as WorkTrackingSystemConnectionDto;
                Assert.That(dto?.Id, Is.EqualTo(12));
                Assert.That(dto?.Name, Is.EqualTo("ADO"));
            }
        }

        [Test]
        public async Task GetWorkTrackingSystemConnection_ConnectionDoesNotExist_ReturnsNotFound()
        {
            repositoryMock.Setup(x => x.GetById(12)).Returns((WorkTrackingSystemConnection?)null);

            var subject = CreateSubject();

            var result = await subject.GetWorkTrackingSystemConnection(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult?.StatusCode, Is.EqualTo(404));
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
        public async Task UpdateWorkTrackingSystemConnection_MoreThanOneAdditionalFieldAfterUpdate_ReturnsForbid()
        {
            var existingConnection = new WorkTrackingSystemConnection { Name = "Boring Old Name" };
            existingConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Option", Value = "Old Option Value" });
            existingConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition{Id = 1, Reference = "Ref", DisplayName = "Display" });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Fancy New Name" };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "Option", Value = "Nobody expects the Spanish Inquisition" });
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto{ Id = 1,  Reference = "Ref", DisplayName = "Display" });
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto{ Id = 2, Reference = "Ref", DisplayName = "Ref" });
            
            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ActionResult<WorkTrackingSystemConnectionDto>>());
                var objectResult = result.Result as ObjectResult;

                Assert.That(objectResult, Is.Not.Null);
                Assert.That(objectResult.StatusCode, Is.EqualTo(403));
                Assert.That(objectResult.Value, Is.Null);
            }

            repositoryMock.Verify(x => x.Update(It.IsAny<WorkTrackingSystemConnection>()), Times.Never);
            repositoryMock.Verify(x => x.Save(), Times.Never);
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
        public async Task DeleteWorkTrackingSystemConnection_ReferencedByTeam_ReturnsConflictAsync()
        {
            repositoryMock.Setup(x => x.Exists(12)).Returns(true);
            teamRepositoryMock.Setup(x => x.Exists(It.IsAny<Func<Team, bool>>())).Returns(true);
            portfolioRepositoryMock.Setup(x => x.Exists(It.IsAny<Func<Portfolio, bool>>())).Returns(false);

            var subject = CreateSubject();

            var result = await subject.DeleteWorkTrackingSystemConnectionAsync(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
                var conflictResult = result as ConflictObjectResult;

                Assert.That(conflictResult.StatusCode, Is.EqualTo(409));
            }

            repositoryMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task DeleteWorkTrackingSystemConnection_ReferencedByPortfolio_ReturnsConflictAsync()
        {
            repositoryMock.Setup(x => x.Exists(12)).Returns(true);
            teamRepositoryMock.Setup(x => x.Exists(It.IsAny<Func<Team, bool>>())).Returns(false);
            portfolioRepositoryMock.Setup(x => x.Exists(It.IsAny<Func<Portfolio, bool>>())).Returns(true);

            var subject = CreateSubject();

            var result = await subject.DeleteWorkTrackingSystemConnectionAsync(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
                var conflictResult = result as ConflictObjectResult;

                Assert.That(conflictResult.StatusCode, Is.EqualTo(409));
            }

            repositoryMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task UpdateWorkTrackingSystemConnection_EmptySecretValue_PreservesExistingSecretAsync()
        {
            var existingConnection = new WorkTrackingSystemConnection { Name = "Connection" };
            existingConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "apiToken", Value = "OriginalEncryptedSecret", IsSecret = true });
            existingConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "normalOption", Value = "OldValue", IsSecret = false });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Updated Name" };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "apiToken", Value = "", IsSecret = true });
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "normalOption", Value = "NewValue", IsSecret = false });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                
                // Verify the secret option kept its original value
                Assert.That(existingConnection.Options.Single(o => o.Key == "apiToken").Value, 
                    Is.EqualTo("OriginalEncryptedSecret"));
                
                // Verify the non-secret option was updated
                Assert.That(existingConnection.Options.Single(o => o.Key == "normalOption").Value, 
                    Is.EqualTo("NewValue"));
            }

            repositoryMock.Verify(x => x.Update(It.IsAny<WorkTrackingSystemConnection>()));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task UpdateWorkTrackingSystemConnection_NonEmptySecretValue_UpdatesSecretAsync()
        {
            var existingConnection = new WorkTrackingSystemConnection { Name = "Connection" };
            existingConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "apiToken", Value = "OriginalEncryptedSecret", IsSecret = true });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection" };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = "apiToken", Value = "NewSecretValue", IsSecret = true });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                
                // Verify the secret option was updated with new value
                Assert.That(existingConnection.Options.Single(o => o.Key == "apiToken").Value, 
                    Is.EqualTo("NewSecretValue"));
            }

            repositoryMock.Verify(x => x.Update(It.IsAny<WorkTrackingSystemConnection>()));
            repositoryMock.Verify(x => x.Save());
        }

        [TestCase(FailingStep.LookingUpTheTeamsAndPortfolios)]
        [TestCase(FailingStep.PublishingTheChange)]
        [Category("contract-shape:bounded-change")]
        public async Task UpdateWorkTrackingSystemConnection_StartingUpdatesFailsAfterAStoredSave_AnswersOkAndLogsOneError(FailingStep failingStep)
        {
            const int connectionId = 12;
            var existingConnection = new WorkTrackingSystemConnection { Id = connectionId, Name = "Connection" };
            repositoryMock.Setup(x => x.GetById(connectionId)).Returns(existingConnection);

            var failure = new InvalidOperationException("the database went away");
            if (failingStep == FailingStep.LookingUpTheTeamsAndPortfolios)
            {
                teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>())).Throws(failure);
            }
            else
            {
                teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                    .Returns(new List<Team> { new() { Id = 2 } }.AsQueryable());
                portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                    .Returns(new List<Portfolio>().AsQueryable());
                domainEventDispatcherMock.Setup(x => x.PublishAsync(It.IsAny<ConnectionFetchShapeChanged>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(failure);
            }

            var subject = CreateSubject();

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = connectionId, Name = "Connection" };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { DisplayName = "Sprint", Reference = "System.Sprint" });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(connectionId, connectionDto);

            var okResult = result.Result as OkObjectResult;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(okResult?.StatusCode, Is.EqualTo(200));
                Assert.That((okResult?.Value as WorkTrackingSystemConnectionDto)?.Id, Is.EqualTo(connectionId));
            }

            repositoryMock.Verify(x => x.Save(), Times.Once);
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(connectionId.ToString(CultureInfo.InvariantCulture))),
                    failure,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task UpdateWorkTrackingSystemConnection_FieldAddedToAConnectionNoTeamOrPortfolioUses_PublishesNothing()
        {
            const int connectionId = 12;
            var existingConnection = new WorkTrackingSystemConnection { Id = connectionId, Name = "Connection" };
            repositoryMock.Setup(x => x.GetById(connectionId)).Returns(existingConnection);
            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns(new List<Team>().AsQueryable());
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns(new List<Portfolio>().AsQueryable());

            var subject = CreateSubject();

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = connectionId, Name = "Connection" };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { DisplayName = "Sprint", Reference = "System.Sprint" });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(connectionId, connectionDto);

            Assert.That((result.Result as OkObjectResult)?.StatusCode, Is.EqualTo(200));
            repositoryMock.Verify(x => x.Save(), Times.Once);
            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.IsAny<ConnectionFetchShapeChanged>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task UpdateWorkTrackingSystemConnection_FieldAdded_PublishesTheTeamsAndPortfoliosOnThisConnectionOnly()
        {
            var subject = CreateSubjectForAConnectionWithOwnersHereAndElsewhere();
            ConnectionFetchShapeChanged? published = null;
            domainEventDispatcherMock.Setup(x => x.PublishAsync(It.IsAny<ConnectionFetchShapeChanged>(), It.IsAny<CancellationToken>()))
                .Callback<ConnectionFetchShapeChanged, CancellationToken>((change, _) => published = change)
                .Returns(Task.CompletedTask);

            await subject.UpdateWorkTrackingSystemConnectionAsync(OwnedConnectionId, AConnectionSaveWithAnAddedField());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(published?.ConnectionId, Is.EqualTo(OwnedConnectionId));
                Assert.That(published?.TeamIds, Is.EqualTo(TeamsOnTheOwnedConnection));
                Assert.That(published?.PortfolioIds, Is.EqualTo(PortfoliosOnTheOwnedConnection));
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task UpdateWorkTrackingSystemConnection_FieldAddedButUpdatesLeftForLater_PublishesNothing()
        {
            var subject = CreateSubjectForAConnectionWithOwnersHereAndElsewhere();

            await subject.UpdateWorkTrackingSystemConnectionAsync(OwnedConnectionId, AConnectionSaveWithAnAddedField(), startUpdates: false);

            repositoryMock.Verify(x => x.Save(), Times.Once);
            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.IsAny<ConnectionFetchShapeChanged>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task UpdateWorkTrackingSystemConnection_OnlyTheNameChanged_PublishesNothing()
        {
            var subject = CreateSubjectForAConnectionWithOwnersHereAndElsewhere();

            await subject.UpdateWorkTrackingSystemConnectionAsync(OwnedConnectionId, new WorkTrackingSystemConnectionDto { Id = OwnedConnectionId, Name = "Renamed" });

            repositoryMock.Verify(x => x.Save(), Times.Once);
            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.IsAny<ConnectionFetchShapeChanged>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private const int OwnedConnectionId = 12;

        private const int OtherConnectionId = 99;

        private static readonly int[] TeamsOnTheOwnedConnection = [2];

        private static readonly int[] PortfoliosOnTheOwnedConnection = [7];

        private WorkTrackingSystemConnectionController CreateSubjectForAConnectionWithOwnersHereAndElsewhere()
        {
            repositoryMock.Setup(x => x.GetById(OwnedConnectionId))
                .Returns(new WorkTrackingSystemConnection { Id = OwnedConnectionId, Name = "Connection" });

            var teams = new List<Team>
            {
                new() { Id = 2, WorkTrackingSystemConnectionId = OwnedConnectionId },
                new() { Id = 3, WorkTrackingSystemConnectionId = OtherConnectionId },
            };
            var portfolios = new List<Portfolio>
            {
                new() { Id = 7, WorkTrackingSystemConnectionId = OwnedConnectionId },
                new() { Id = 8, WorkTrackingSystemConnectionId = OtherConnectionId },
            };
            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns((Expression<Func<Portfolio, bool>> predicate) => portfolios.AsQueryable().Where(predicate));

            return CreateSubject();
        }

        private static WorkTrackingSystemConnectionDto AConnectionSaveWithAnAddedField()
        {
            var connectionDto = new WorkTrackingSystemConnectionDto { Id = OwnedConnectionId, Name = "Connection" };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { DisplayName = "Sprint", Reference = "System.Sprint" });
            return connectionDto;
        }

        public enum FailingStep
        {
            LookingUpTheTeamsAndPortfolios,
            PublishingTheChange,
        }

        private WorkTrackingSystemConnectionController CreateSubject()
        {
            return new WorkTrackingSystemConnectionController(repositoryMock.Object, teamRepositoryMock.Object, portfolioRepositoryMock.Object, licenseServiceMock.Object, workTrackingConnectorFactoryMock.Object, domainEventDispatcherMock.Object, loggerMock.Object);
        }
    }
}
