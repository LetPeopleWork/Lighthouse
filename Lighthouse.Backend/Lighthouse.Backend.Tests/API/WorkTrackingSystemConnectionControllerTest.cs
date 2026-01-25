using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class WorkTrackingSystemConnectionControllerTest
    {
        private Mock<IRepository<WorkTrackingSystemConnection>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
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

        private WorkTrackingSystemConnectionController CreateSubject()
        {
            return new WorkTrackingSystemConnectionController(repositoryMock.Object);
        }
    }
}
