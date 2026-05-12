using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    public class ApiKeyServiceTest
    {
        private Mock<IApiKeyRepository> repositoryMock;
        private Mock<IRepository<UserProfile>> userProfileRepositoryMock;
        private Mock<IRepository<ApiKeyPermission>> apiKeyPermissionRepositoryMock;
        private Mock<ILogger<ApiKeyService>> loggerMock;
        private ApiKeyService subject;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IApiKeyRepository>();
            userProfileRepositoryMock = new Mock<IRepository<UserProfile>>();
            apiKeyPermissionRepositoryMock = new Mock<IRepository<ApiKeyPermission>>();
            loggerMock = new Mock<ILogger<ApiKeyService>>();
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([]);
            subject = new ApiKeyService(
                repositoryMock.Object,
                userProfileRepositoryMock.Object,
                apiKeyPermissionRepositoryMock.Object,
                loggerMock.Object);
        }

        // --- CreateApiKey ---

        [Test]
        public async Task CreateApiKey_ReturnsNonEmptyPlainTextKey()
        {
            SetupSave();

            var result = await subject.CreateApiKeyAsync("my-key", "desc", "alice", "alice-subject");

            Assert.That(result.PlainTextKey, Is.Not.Empty);
        }

        [Test]
        public async Task CreateApiKey_PlainTextKeyIsAtLeast32Characters()
        {
            SetupSave();

            var result = await subject.CreateApiKeyAsync("my-key", "desc", "alice", "alice-subject");

            Assert.That(result.PlainTextKey, Has.Length.GreaterThanOrEqualTo(32));
        }

        [Test]
        public async Task CreateApiKey_TwoCallsReturnDifferentKeys()
        {
            SetupSave();

            var result1 = await subject.CreateApiKeyAsync("key1", "desc", "alice", "alice-subject");
            var result2 = await subject.CreateApiKeyAsync("key2", "desc", "alice", "alice-subject");

            Assert.That(result1.PlainTextKey, Is.Not.EqualTo(result2.PlainTextKey));
        }

        [Test]
        public async Task CreateApiKey_PersistsApiKeyWithHash()
        {
            ApiKey? savedKey = null;
            repositoryMock.Setup(r => r.Add(It.IsAny<ApiKey>()))
                .Callback<ApiKey>(k => savedKey = k);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            await subject.CreateApiKeyAsync("my-key", "desc", "alice", "alice-subject");

            Assert.That(savedKey, Is.Not.Null);
            Assert.That(savedKey!.KeyHash, Is.Not.Empty);
        }

        [Test]
        public async Task CreateApiKey_StoredHashIsNotPlainTextKey()
        {
            ApiKey? savedKey = null;
            repositoryMock.Setup(r => r.Add(It.IsAny<ApiKey>()))
                .Callback<ApiKey>(k => savedKey = k);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            var result = await subject.CreateApiKeyAsync("my-key", "desc", "alice", "alice-subject");

            Assert.That(savedKey!.KeyHash, Is.Not.EqualTo(result.PlainTextKey));
        }

        [Test]
        public async Task CreateApiKey_StoredHashHasSalt()
        {
            ApiKey? savedKey = null;
            repositoryMock.Setup(r => r.Add(It.IsAny<ApiKey>()))
                .Callback<ApiKey>(k => savedKey = k);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            await subject.CreateApiKeyAsync("my-key", "desc", "alice", "alice-subject");

            Assert.That(savedKey!.Salt, Is.Not.Empty);
        }

        [Test]
        public async Task CreateApiKey_ReturnsCorrectMetadata()
        {
            SetupSave();

            var result = await subject.CreateApiKeyAsync("my-key", "A description", "alice", "alice-subject");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Name, Is.EqualTo("my-key"));
                Assert.That(result.Description, Is.EqualTo("A description"));
                Assert.That(result.CreatedByUser, Is.EqualTo("alice"));
            }
        }

        [Test]
        public async Task CreateApiKey_CreatedAtIsApproximatelyNow()
        {
            SetupSave();

            var before = DateTime.UtcNow;
            var result = await subject.CreateApiKeyAsync("key", "desc", "alice", "alice-subject");
            var after = DateTime.UtcNow;

            Assert.That(result.CreatedAt, Is.InRange(before, after));
        }

        // --- GetApiKeysByOwnerSubject ---

        [Test]
        public void GetApiKeysByOwnerSubject_ReturnsInfoWithoutHash()
        {
            var keys = new List<ApiKey>
            {
                new ApiKey { Id = 1, Name = "key1", Description = "d1", CreatedByUser = "alice", OwnerSubject = "alice-subject", CreatedAt = DateTime.UtcNow },
                new ApiKey { Id = 2, Name = "key2", Description = "d2", CreatedByUser = "bob", OwnerSubject = "bob-subject", CreatedAt = DateTime.UtcNow },
            };
            repositoryMock.Setup(r => r.GetAll()).Returns(keys);

            var result = subject.GetApiKeysByOwnerSubject("alice-subject").ToList();

            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetApiKeysByOwnerSubject_MapsMetadataCorrectly()
        {
            var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastUsed = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var keys = new List<ApiKey>
            {
                new ApiKey { Id = 42, Name = "k", Description = "desc", CreatedByUser = "alice", OwnerSubject = "alice-subject", CreatedAt = createdAt, LastUsedAt = lastUsed },
            };
            repositoryMock.Setup(r => r.GetAll()).Returns(keys);

            var result = subject.GetApiKeysByOwnerSubject("alice-subject").Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(42));
                Assert.That(result.Name, Is.EqualTo("k"));
                Assert.That(result.Description, Is.EqualTo("desc"));
                Assert.That(result.CreatedByUser, Is.EqualTo("alice"));
                Assert.That(result.CreatedAt, Is.EqualTo(createdAt));
                Assert.That(result.LastUsedAt, Is.EqualTo(lastUsed));
            }
        }

        // --- DeleteApiKey ---

        [Test]
        public async Task DeleteApiKey_ExistingIdOwnedByCurrentUser_ReturnsTrue()
        {
            repositoryMock.Setup(r => r.Exists(7)).Returns(true);
            repositoryMock.Setup(r => r.GetById(7)).Returns(new ApiKey { Id = 7, CreatedByUser = "alice", OwnerSubject = "alice-subject" });
            repositoryMock.Setup(r => r.Remove(7));
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            var result = await subject.DeleteApiKey(7, "alice-subject");

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task DeleteApiKey_NonExistentId_ReturnsFalse()
        {
            repositoryMock.Setup(r => r.Exists(99)).Returns(false);

            var result = await subject.DeleteApiKey(99, "alice-subject");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteApiKey_ExistingIdOwnedByDifferentUser_ReturnsFalse()
        {
            repositoryMock.Setup(r => r.Exists(99)).Returns(true);
            repositoryMock.Setup(r => r.GetById(99)).Returns(new ApiKey { Id = 99, CreatedByUser = "bob", OwnerSubject = "bob-subject" });

            var result = await subject.DeleteApiKey(99, "alice-subject");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task DeleteApiKey_NonExistentId_DoesNotCallRemove()
        {
            repositoryMock.Setup(r => r.Exists(99)).Returns(false);

            await subject.DeleteApiKey(99, "alice-subject");

            repositoryMock.Verify(r => r.Remove(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task DeleteApiKey_DifferentOwner_DoesNotCallRemove()
        {
            repositoryMock.Setup(r => r.Exists(99)).Returns(true);
            repositoryMock.Setup(r => r.GetById(99)).Returns(new ApiKey { Id = 99, CreatedByUser = "bob", OwnerSubject = "bob-subject" });

            await subject.DeleteApiKey(99, "alice-subject");

            repositoryMock.Verify(r => r.Remove(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task ValidateApiKeyWithOwnerAsync_ResolvedOwner_ReturnsResolvedState()
        {
            var (plainText, hash, salt) = GenerateTestKey();
            var keys = new List<ApiKey>
            {
                new ApiKey { Id = 1, Name = "k", KeyHash = hash, Salt = salt, CreatedByUser = "alice", OwnerSubject = "alice-subject" },
            };
            var profiles = new List<UserProfile>
            {
                new UserProfile { Id = 5, Subject = "alice-subject", SubjectClaimType = "sub", DisplayName = "Alice" },
            };

            repositoryMock.Setup(r => r.GetAll()).Returns(keys);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns(profiles);

            var result = await subject.ValidateApiKeyWithOwnerAsync(plainText);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.OwnerResolutionState, Is.EqualTo(ApiKeyOwnerResolutionState.Resolved));
                Assert.That(result.OwnerSubject, Is.EqualTo("alice-subject"));
            }
        }

        [Test]
        public async Task ValidateApiKeyWithOwnerAsync_UnlinkedOwner_ReturnsUnlinkedState()
        {
            var (plainText, hash, salt) = GenerateTestKey();
            var keys = new List<ApiKey>
            {
                new ApiKey { Id = 1, Name = "k", KeyHash = hash, Salt = salt, CreatedByUser = "unknown" },
            };

            repositoryMock.Setup(r => r.GetAll()).Returns(keys);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([]);

            var result = await subject.ValidateApiKeyWithOwnerAsync(plainText);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.OwnerResolutionState, Is.EqualTo(ApiKeyOwnerResolutionState.Unlinked));
                Assert.That(result.OwnerSubject, Is.Null);
            }
        }

        // --- ValidateApiKey ---

        [Test]
        public async Task ValidateApiKey_ValidKey_ReturnsTrue()
        {
            var (plainText, hash, salt) = GenerateTestKey();
            var keys = new List<ApiKey>
            {
                new ApiKey { Id = 1, Name = "k", KeyHash = hash, Salt = salt, CreatedByUser = "alice", OwnerSubject = "alice-subject" },
            };
            repositoryMock.Setup(r => r.GetAll()).Returns(keys);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            var result = await subject.ValidateApiKeyAsync(plainText);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ValidateApiKey_InvalidKey_ReturnsFalse()
        {
            var (_, hash, salt) = GenerateTestKey();
            var keys = new List<ApiKey>
            {
                new ApiKey { Id = 1, Name = "k", KeyHash = hash, Salt = salt, CreatedByUser = "alice", OwnerSubject = "alice-subject" },
            };
            repositoryMock.Setup(r => r.GetAll()).Returns(keys);

            var result = await subject.ValidateApiKeyAsync("wrong-key");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateApiKey_EmptyKey_ReturnsFalse()
        {
            repositoryMock.Setup(r => r.GetAll()).Returns([]);

            var result = await subject.ValidateApiKeyAsync("");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateApiKey_NoKeys_ReturnsFalse()
        {
            repositoryMock.Setup(r => r.GetAll()).Returns([]);

            var result = await subject.ValidateApiKeyAsync("some-key");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task ValidateApiKey_ValidKey_UpdatesLastUsedAt()
        {
            var (plainText, hash, salt) = GenerateTestKey();
            var key = new ApiKey { Id = 1, Name = "k", KeyHash = hash, Salt = salt, CreatedByUser = "alice", OwnerSubject = "alice-subject" };
            repositoryMock.Setup(r => r.GetAll()).Returns([key]);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            await subject.ValidateApiKeyAsync(plainText);

            Assert.That(key.LastUsedAt, Is.Not.Null);
        }

        // --- Helpers ---

        private void SetupSave()
        {
            repositoryMock.Setup(r => r.Add(It.IsAny<ApiKey>()));
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);
        }

        private (string plainText, string hash, string salt) GenerateTestKey()
        {
            // Generate a real key+hash using the service to set up test data
            SetupSave();
            ApiKey? captured = null;
            repositoryMock.Setup(r => r.Add(It.IsAny<ApiKey>()))
                .Callback<ApiKey>(k => captured = k);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            var creation = subject.CreateApiKeyAsync("setup-key", "desc", "alice", "alice-subject").GetAwaiter().GetResult();
            return (creation.PlainTextKey, captured!.KeyHash, captured.Salt);
        }

        // --- Authorization telemetry ---

        [Test]
        public async Task ValidateApiKeyWithOwnerAsync_WhenOwnerUnlinked_LogsWarning()
        {
            var (plainText, hash, salt) = GenerateTestKey();
            var key = new ApiKey { Id = 77, KeyHash = hash, Salt = salt };
            repositoryMock.Setup(r => r.GetAll()).Returns([key]);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([]);

            await subject.ValidateApiKeyWithOwnerAsync(plainText);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("77")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task ValidateApiKeyWithOwnerAsync_WhenOwnerResolved_LogsDebug()
        {
            var profile = new UserProfile { Id = 5, Subject = "alice-subject", DisplayName = "Alice" };
            var (plainText, hash, salt) = GenerateTestKey();
            var key = new ApiKey { Id = 88, KeyHash = hash, Salt = salt, OwnerSubject = "alice-subject" };
            repositoryMock.Setup(r => r.GetAll()).Returns([key]);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([profile]);

            await subject.ValidateApiKeyWithOwnerAsync(plainText);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("88")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void DeleteApiKey_WhenOwnerSubjectMismatch_LogsWarning()
        {
            var profile = new UserProfile { Id = 5, Subject = "alice-subject", DisplayName = "Alice" };
            var key = new ApiKey { Id = 99, OwnerSubject = "alice-subject" };
            repositoryMock.Setup(r => r.Exists(99)).Returns(true);
            repositoryMock.Setup(r => r.GetById(99)).Returns(key);
            userProfileRepositoryMock.Setup(r => r.GetAll()).Returns([profile]);

            subject.DeleteApiKey(99, "other-subject");

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("99")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
