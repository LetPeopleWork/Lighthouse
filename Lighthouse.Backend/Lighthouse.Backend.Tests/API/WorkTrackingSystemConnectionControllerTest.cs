using System.Text.Json;
using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Connections;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

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

        private const int StoredIdOfTheNewField = 50;

        private static readonly int?[] TheNewFieldStoredBeforeTheDefaultNamesIt = [null, StoredIdOfTheNewField];

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            licenseServiceMock = new Mock<ILicenseService>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();

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
                ParentOverrideForTeams = 5,
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
                Assert.That(dto?.Defaults?.ParentOverrideForTeams, Is.EqualTo(5));
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
        [TestCase(WorkTrackingSystems.Jira, "https://x.atlassian.net/jira/", "https://x.atlassian.net")]
        [TestCase(WorkTrackingSystems.Jira, "https://host/jira/", "https://host/jira")]
        [TestCase(WorkTrackingSystems.AzureDevOps, "https://x.atlassian.net/jira/", "https://x.atlassian.net/jira/")]
        public async Task UpdateWorkTrackingSystemConnection_UrlOption_IsStoredAsTheSiteAddressOnlyForJira(WorkTrackingSystems workTrackingSystem, string enteredUrl, string expectedUrl)
        {
            var existingConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = workTrackingSystem };
            existingConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = "https://old.example.com" });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);

            var subject = CreateSubject();

            var connectionDto = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Connection", WorkTrackingSystem = workTrackingSystem };
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = JiraWorkTrackingOptionNames.Url, Value = enteredUrl });
            connectionDto.Options.Add(new WorkTrackingSystemConnectionOptionDto { Key = JiraWorkTrackingOptionNames.Username, Value = enteredUrl });

            await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(existingConnection.Options.Single(o => o.Key == JiraWorkTrackingOptionNames.Url).Value, Is.EqualTo(expectedUrl));
                Assert.That(existingConnection.Options.Single(o => o.Key == JiraWorkTrackingOptionNames.Username).Value, Is.EqualTo(enteredUrl));
            }

            repositoryMock.Verify(x => x.Save());
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

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public async Task UpdateWorkTrackingSystemConnection_ADefaultThisVersionCannotHoldYet_IsRefusedAndNothingIsStored()
        {
            var existingConnection = new WorkTrackingSystemConnection { Name = "Jira DC", WorkTrackingSystem = WorkTrackingSystems.Jira };
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);
            var subject = CreateSubject();
            subject.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            subject.ProblemDetailsFactory = new PlainProblemDetailsFactory();
            var connectionDto = new WorkTrackingSystemConnectionDto
            {
                Id = 12,
                Name = "Renamed",
                Defaults = new ConnectionDefaultsDto
                {
                    NotHeldByThisBuild = new()
                    {
                        ["aDefaultFromANewerVersion"] = JsonSerializer.SerializeToElement(5),
                        ["anotherDefaultFromANewerVersion"] = JsonSerializer.SerializeToElement(6),
                    },
                },
            };

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            var refusal = result.Result as ObjectResult;
            var problem = refusal?.Value as ProblemDetails;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.StatusCode, Is.EqualTo(400));
                Assert.That(problem?.Extensions["code"], Is.EqualTo("connection-default-not-supported"));
                Assert.That(problem?.Title, Is.EqualTo("This connection cannot hold these defaults."));
                Assert.That(problem?.Detail, Is.EqualTo("Not supported for this connection by this version of Lighthouse: aDefaultFromANewerVersion, anotherDefaultFromANewerVersion."));
                Assert.That(existingConnection.Name, Is.EqualTo("Jira DC"));
            }

            repositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [TestCase(9999, TestName = "UpdateWorkTrackingSystemConnection_ADefaultNamingAMadeUpField_IsRefusedAndNothingIsStored")]
        [TestCase(77, TestName = "UpdateWorkTrackingSystemConnection_ADefaultNamingAnotherConnectionsField_IsRefusedAndNothingIsStored")]
        [TestCase(-1, TestName = "UpdateWorkTrackingSystemConnection_ADefaultNamingAPlaceholderNoFieldInTheSaveCarries_IsRefusedAndNothingIsStored")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task UpdateWorkTrackingSystemConnection_ADefaultNamingNoFieldOfThisConnection_IsRefusedAndNothingIsStored(int namedFieldId)
        {
            const int CausedById = 5;
            var existingConnection = new WorkTrackingSystemConnection { Id = 12, Name = "Jira DC", WorkTrackingSystem = WorkTrackingSystems.Jira, ParentOverrideForTeams = CausedById };
            existingConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = CausedById, DisplayName = "Caused by", Reference = "customfield_10500" });
            var otherConnection = new WorkTrackingSystemConnection { Id = 13, Name = "Jira DC Treasury", WorkTrackingSystem = WorkTrackingSystems.Jira };
            otherConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 77, DisplayName = "Treasury Caused by", Reference = "customfield_10500" });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);
            repositoryMock.Setup(x => x.GetById(13)).Returns(otherConnection);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            var subject = CreateSubject();
            subject.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            subject.ProblemDetailsFactory = new PlainProblemDetailsFactory();
            var connectionDto = new WorkTrackingSystemConnectionDto
            {
                Id = 12,
                Name = "Jira DC",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Defaults = new ConnectionDefaultsDto { ParentOverrideForTeams = namedFieldId },
            };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = CausedById, DisplayName = "Caused by", Reference = "customfield_10500" });
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = -2, DisplayName = "Epic Link", Reference = "customfield_10008" });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            var refusal = result.Result as ObjectResult;
            var problem = refusal?.Value as ProblemDetails;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.StatusCode, Is.EqualTo(400));
                Assert.That(problem?.Extensions["code"], Is.EqualTo("connection-default-field-unknown"));
                Assert.That(problem?.Title, Is.EqualTo("A default names a field this connection does not have."));
                Assert.That(problem?.Detail, Is.EqualTo("Not an Additional Field of this connection: parentOverrideForTeams."));
                Assert.That(existingConnection.ParentOverrideForTeams, Is.EqualTo(CausedById));
            }

            repositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public async Task UpdateWorkTrackingSystemConnection_ThePortfoliosDefaultNamingNoFieldOfThisConnection_IsRefusedAndNothingIsStored()
        {
            const int CausedById = 5;
            var existingConnection = new WorkTrackingSystemConnection { Id = 12, Name = "Jira DC", WorkTrackingSystem = WorkTrackingSystems.Jira, ParentOverrideForPortfolios = CausedById };
            existingConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = CausedById, DisplayName = "Caused by", Reference = "customfield_10500" });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            var subject = CreateSubject();
            subject.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            subject.ProblemDetailsFactory = new PlainProblemDetailsFactory();
            var connectionDto = new WorkTrackingSystemConnectionDto
            {
                Id = 12,
                Name = "Jira DC",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Defaults = new ConnectionDefaultsDto { ParentOverrideForPortfolios = 9999 },
            };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = CausedById, DisplayName = "Caused by", Reference = "customfield_10500" });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            var refusal = result.Result as ObjectResult;
            var problem = refusal?.Value as ProblemDetails;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.StatusCode, Is.EqualTo(400));
                Assert.That(problem?.Extensions["code"], Is.EqualTo("connection-default-field-unknown"));
                Assert.That(problem?.Detail, Is.EqualTo("Not an Additional Field of this connection: parentOverrideForPortfolios."));
                Assert.That(existingConnection.ParentOverrideForPortfolios, Is.EqualTo(CausedById));
            }

            repositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task UpdateWorkTrackingSystemConnection_ACopyReadBeforeSomeoneElsesSave_IsRefusedAsStaleAndNothingIsStored()
        {
            var existingConnection = new WorkTrackingSystemConnection { Id = 12, Name = "Jira DC", WorkTrackingSystem = WorkTrackingSystems.Jira, ConcurrencyToken = Guid.NewGuid() };
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);
            var subject = CreateSubject();
            var staleCopy = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Renamed", ConcurrencyToken = Guid.NewGuid() };

            var stale = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => subject.UpdateWorkTrackingSystemConnectionAsync(12, staleCopy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stale?.Message, Is.EqualTo("Connection 12 was saved by someone else after this copy was read."));
                Assert.That(existingConnection.Name, Is.EqualTo("Jira DC"));
            }

            repositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task UpdateWorkTrackingSystemConnection_RemovingTheFieldTheTeamsDefaultNames_IsRefusedAsInUseAndNothingIsStored()
        {
            const int CausedById = 5;
            var existingConnection = new WorkTrackingSystemConnection { Id = 12, Name = "Jira DC", WorkTrackingSystem = WorkTrackingSystems.Jira, ParentOverrideForTeams = CausedById };
            existingConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = CausedById, DisplayName = "Caused by", Reference = "customfield_10500" });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);
            var subject = CreateSubject();
            subject.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            subject.ProblemDetailsFactory = new PlainProblemDetailsFactory();
            var withoutTheField = new WorkTrackingSystemConnectionDto { Id = 12, Name = "Jira DC", WorkTrackingSystem = WorkTrackingSystems.Jira };

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, withoutTheField);

            var refusal = result.Result as ObjectResult;
            var problem = refusal?.Value as ProblemDetails;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.StatusCode, Is.EqualTo(409));
                Assert.That(problem?.Extensions["code"], Is.EqualTo("additional-field-in-use"));
                Assert.That(problem?.Title, Is.EqualTo("An Additional Field this save removes is still in use."));
                Assert.That(problem?.Detail, Is.EqualTo("\"Caused by\" cannot be removed. It is the Teams' Parent Override Field default. Nothing was saved."));
            }

            repositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task UpdateWorkTrackingSystemConnection_TheTeamsDefaultMovedToAnotherStoredField_StoresTheDefault()
        {
            var existingConnection = new WorkTrackingSystemConnection { Id = 12, Name = "Jira DC", WorkTrackingSystem = WorkTrackingSystems.Jira, ParentOverrideForTeams = 5 };
            existingConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 5, DisplayName = "Caused by", Reference = "customfield_10500" });
            existingConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 6, DisplayName = "Epic Link", Reference = "customfield_10008" });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            var subject = CreateSubject();
            var connectionDto = new WorkTrackingSystemConnectionDto
            {
                Id = 12,
                Name = "Jira DC",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Defaults = new ConnectionDefaultsDto { ParentOverrideForTeams = 6 },
            };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = 5, DisplayName = "Caused by", Reference = "customfield_10500" });
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = 6, DisplayName = "Epic Link", Reference = "customfield_10008" });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                Assert.That(existingConnection.ParentOverrideForTeams, Is.EqualTo(6));
            }
        }

        [Test]
        [Category("story-6313")]
        public async Task UpdateWorkTrackingSystemConnection_TheTeamsEstimationDefaultSent_StoresTheWholeBundle()
        {
            var existingConnection = new WorkTrackingSystemConnection { Id = 12, Name = "Jira DC", WorkTrackingSystem = WorkTrackingSystems.Jira };
            existingConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 6, DisplayName = "Story Points", Reference = "customfield_10016" });
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            var subject = CreateSubject();
            var connectionDto = new WorkTrackingSystemConnectionDto
            {
                Id = 12,
                Name = "Jira DC",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Defaults = new ConnectionDefaultsDto
                {
                    EstimationForTeams = new EstimationDefaultDto { AdditionalFieldDefinitionId = 6, Unit = "Sizes", UseNonNumericEstimation = true, EstimationCategoryValues = ["S", "M"] },
                },
            };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = 6, DisplayName = "Story Points", Reference = "customfield_10016" });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                Assert.That(existingConnection.EstimationForTeams, Is.EqualTo(new EstimationBundle(6, "Sizes", true, ["S", "M"])));
            }
        }

        [Test]
        public async Task UpdateWorkTrackingSystemConnection_TheTeamsDefaultNamesAFieldAddedInThisSave_StoresTheFieldFirstAndThenPointsTheDefaultAtIt()
        {
            var existingConnection = new WorkTrackingSystemConnection { Id = 12, Name = "Jira DC", WorkTrackingSystem = WorkTrackingSystems.Jira };
            repositoryMock.Setup(x => x.GetById(12)).Returns(existingConnection);
            var defaultAtEachSave = new List<int?>();
            repositoryMock.Setup(x => x.Save()).Callback(() =>
            {
                existingConnection.AdditionalFieldDefinitions.ForEach(field => field.Id = field.Id == 0 ? StoredIdOfTheNewField : field.Id);
                defaultAtEachSave.Add(existingConnection.ParentOverrideForTeams);
            });
            var subject = CreateSubject();
            var connectionDto = new WorkTrackingSystemConnectionDto
            {
                Id = 12,
                Name = "Jira DC",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Defaults = new ConnectionDefaultsDto { ParentOverrideForTeams = -2 },
            };
            connectionDto.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinitionDto { Id = -2, DisplayName = "Epic Link", Reference = "customfield_10008" });

            var result = await subject.UpdateWorkTrackingSystemConnectionAsync(12, connectionDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                Assert.That(defaultAtEachSave, Is.EqualTo(TheNewFieldStoredBeforeTheDefaultNamesIt));
            }
        }

        private sealed class PlainProblemDetailsFactory : ProblemDetailsFactory
        {
            public override ProblemDetails CreateProblemDetails(
                HttpContext httpContext, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
                => new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

            public override ValidationProblemDetails CreateValidationProblemDetails(
                HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
                => new(modelStateDictionary) { Status = statusCode, Title = title };
        }

        private WorkTrackingSystemConnectionController CreateSubject()
        {
            return new WorkTrackingSystemConnectionController(repositoryMock.Object, teamRepositoryMock.Object, portfolioRepositoryMock.Object, licenseServiceMock.Object, workTrackingConnectorFactoryMock.Object, new ConnectionSaveService(repositoryMock.Object, teamRepositoryMock.Object, portfolioRepositoryMock.Object, domainEventDispatcherMock.Object, Mock.Of<ITerminologyService>(), Mock.Of<ILogger<ConnectionSaveService>>()));
        }
    }
}
