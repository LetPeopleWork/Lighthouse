using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class ApiKeyOwnerReconciliationSeederTests
    {
        private Mock<IApiKeyRepository> apiKeyRepositoryMock;
        private Mock<IRepository<UserProfile>> userProfileRepositoryMock;
        private Mock<ILogger<ApiKeyOwnerReconciliationSeeder>> loggerMock;

        [SetUp]
        public void Setup()
        {
            apiKeyRepositoryMock = new Mock<IApiKeyRepository>();
            userProfileRepositoryMock = new Mock<IRepository<UserProfile>>();
            loggerMock = new Mock<ILogger<ApiKeyOwnerReconciliationSeeder>>();

            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([]);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([]);
            apiKeyRepositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);
        }

        [Test]
        public async Task Seed_NoApiKeys_DoesNotSave()
        {
            var subject = CreateSubject();

            await subject.Seed();

            apiKeyRepositoryMock.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task Seed_KeyWithOwnerSubjectAlreadySet_IsSkipped()
        {
            var existingKey = new ApiKey { Id = 1, CreatedByUser = "alice", OwnerSubject = "alice-subject" };
            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([existingKey]);

            var subject = CreateSubject();
            await subject.Seed();

            Assert.That(existingKey.OwnerUserProfileId, Is.Null);
            apiKeyRepositoryMock.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task Seed_KeyWithOwnerUserProfileIdAlreadySet_IsSkipped()
        {
            var existingKey = new ApiKey { Id = 1, CreatedByUser = "alice", OwnerUserProfileId = 99 };
            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([existingKey]);

            var subject = CreateSubject();
            await subject.Seed();

            Assert.That(existingKey.OwnerSubject, Is.Null);
            apiKeyRepositoryMock.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task Seed_UnlinkedKeyMatchesSingleProfileByDisplayName_SetsOwnerFields()
        {
            var profile = new UserProfile { Id = 5, Subject = "alice-subject", DisplayName = "Alice Smith" };
            var apiKey = new ApiKey { Id = 1, CreatedByUser = "Alice Smith" };

            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([apiKey]);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([profile]);

            var subject = CreateSubject();
            await subject.Seed();

            Assert.That(apiKey.OwnerSubject, Is.EqualTo("alice-subject"));
            Assert.That(apiKey.OwnerUserProfileId, Is.EqualTo(5));
        }

        [Test]
        public async Task Seed_UnlinkedKeyMatchesSingleProfileByEmail_SetsOwnerFields()
        {
            var profile = new UserProfile { Id = 7, Subject = "bob-subject", DisplayName = "Bob Jones", Email = "bob@example.com" };
            var apiKey = new ApiKey { Id = 2, CreatedByUser = "bob@example.com" };

            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([apiKey]);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([profile]);

            var subject = CreateSubject();
            await subject.Seed();

            Assert.That(apiKey.OwnerSubject, Is.EqualTo("bob-subject"));
            Assert.That(apiKey.OwnerUserProfileId, Is.EqualTo(7));
        }

        [Test]
        public async Task Seed_UnlinkedKeyMatchesMultipleProfiles_LeavesUnlinked()
        {
            var profile1 = new UserProfile { Id = 1, Subject = "alice-1", DisplayName = "Alice" };
            var profile2 = new UserProfile { Id = 2, Subject = "alice-2", DisplayName = "Alice" };
            var apiKey = new ApiKey { Id = 1, CreatedByUser = "Alice" };

            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([apiKey]);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([profile1, profile2]);

            var subject = CreateSubject();
            await subject.Seed();

            Assert.That(apiKey.OwnerSubject, Is.Null);
            Assert.That(apiKey.OwnerUserProfileId, Is.Null);
        }

        [Test]
        public async Task Seed_UnlinkedKeyWithNoMatchingProfile_LeavesUnlinked()
        {
            var profile = new UserProfile { Id = 1, Subject = "carol-subject", DisplayName = "Carol" };
            var apiKey = new ApiKey { Id = 1, CreatedByUser = "UnknownUser" };

            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([apiKey]);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([profile]);

            var subject = CreateSubject();
            await subject.Seed();

            Assert.That(apiKey.OwnerSubject, Is.Null);
            Assert.That(apiKey.OwnerUserProfileId, Is.Null);
        }

        [Test]
        public async Task Seed_UnlinkedKeyWithNoMatchingProfile_LogsWarning()
        {
            var profile = new UserProfile { Id = 1, Subject = "carol-subject", DisplayName = "Carol" };
            var apiKey = new ApiKey { Id = 42, CreatedByUser = "UnknownUser" };

            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([apiKey]);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([profile]);

            var subject = CreateSubject();
            await subject.Seed();

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("42")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task Seed_WhenAnyKeyReconciled_SavesOnce()
        {
            var profile = new UserProfile { Id = 5, Subject = "alice-subject", DisplayName = "Alice" };
            var apiKey1 = new ApiKey { Id = 1, CreatedByUser = "Alice" };
            var apiKey2 = new ApiKey { Id = 2, CreatedByUser = "Alice" };

            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([apiKey1, apiKey2]);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([profile]);

            var subject = CreateSubject();
            await subject.Seed();

            apiKeyRepositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task Seed_MixedLinkedAndUnlinked_OnlyProcessesUnlinked()
        {
            var profile = new UserProfile { Id = 5, Subject = "alice-subject", DisplayName = "Alice" };
            var linkedKey = new ApiKey { Id = 1, CreatedByUser = "Alice", OwnerSubject = "existing-subject" };
            var unlinkedKey = new ApiKey { Id = 2, CreatedByUser = "Alice" };

            apiKeyRepositoryMock.Setup(r => r.GetAll()).Returns([linkedKey, unlinkedKey]);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([profile]);

            var subject = CreateSubject();
            await subject.Seed();

            Assert.That(linkedKey.OwnerSubject, Is.EqualTo("existing-subject"), "Already-linked key must not be overwritten");
            Assert.That(unlinkedKey.OwnerSubject, Is.EqualTo("alice-subject"), "Unlinked key must be reconciled");
        }

        private ApiKeyOwnerReconciliationSeeder CreateSubject()
        {
            return new ApiKeyOwnerReconciliationSeeder(
                apiKeyRepositoryMock.Object,
                userProfileRepositoryMock.Object,
                loggerMock.Object);
        }
    }
}
