using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    public class ApiKeyServiceTest
    {
        private Mock<IApiKeyRepository> repositoryMock;
        private ApiKeyService subject;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IApiKeyRepository>();
            subject = new ApiKeyService(repositoryMock.Object);
        }

        // --- CreateApiKey ---

        [Test]
        public async Task CreateApiKey_ReturnsNonEmptyPlainTextKey()
        {
            SetupSave();

            var result = await subject.CreateApiKeyAsync("my-key", "desc", "alice");

            Assert.That(result.PlainTextKey, Is.Not.Empty);
        }

        [Test]
        public async Task CreateApiKey_PlainTextKeyIsAtLeast32Characters()
        {
            SetupSave();

            var result = await subject.CreateApiKeyAsync("my-key", "desc", "alice");

            Assert.That(result.PlainTextKey, Has.Length.GreaterThanOrEqualTo(32));
        }

        [Test]
        public async Task CreateApiKey_TwoCallsReturnDifferentKeys()
        {
            SetupSave();

            var result1 = await subject.CreateApiKeyAsync("key1", "desc", "alice");
            var result2 = await subject.CreateApiKeyAsync("key2", "desc", "alice");

            Assert.That(result1.PlainTextKey, Is.Not.EqualTo(result2.PlainTextKey));
        }

        [Test]
        public async Task CreateApiKey_PersistsApiKeyWithHash()
        {
            ApiKey? savedKey = null;
            repositoryMock.Setup(r => r.Add(It.IsAny<ApiKey>()))
                .Callback<ApiKey>(k => savedKey = k);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            await subject.CreateApiKeyAsync("my-key", "desc", "alice");

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

            var result = await subject.CreateApiKeyAsync("my-key", "desc", "alice");

            Assert.That(savedKey!.KeyHash, Is.Not.EqualTo(result.PlainTextKey));
        }

        [Test]
        public async Task CreateApiKey_StoredHashHasSalt()
        {
            ApiKey? savedKey = null;
            repositoryMock.Setup(r => r.Add(It.IsAny<ApiKey>()))
                .Callback<ApiKey>(k => savedKey = k);
            repositoryMock.Setup(r => r.Save()).Returns(Task.CompletedTask);

            await subject.CreateApiKeyAsync("my-key", "desc", "alice");

            Assert.That(savedKey!.Salt, Is.Not.Empty);
        }

        [Test]
        public async Task CreateApiKey_ReturnsCorrectMetadata()
        {
            SetupSave();

            var result = await subject.CreateApiKeyAsync("my-key", "A description", "alice");

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
            var result = await subject.CreateApiKeyAsync("key", "desc", "alice");
            var after = DateTime.UtcNow;

            Assert.That(result.CreatedAt, Is.InRange(before, after));
        }

        // --- GetAllApiKeys ---

        [Test]
        public void GetAllApiKeys_ReturnsInfoWithoutHash()
        {
            var keys = new List<ApiKey>
            {
                new ApiKey { Id = 1, Name = "key1", Description = "d1", CreatedByUser = "alice", CreatedAt = DateTime.UtcNow },
                new ApiKey { Id = 2, Name = "key2", Description = "d2", CreatedByUser = "bob", CreatedAt = DateTime.UtcNow },
            };
            repositoryMock.Setup(r => r.GetAll()).Returns(keys);

            var result = subject.GetAllApiKeys().ToList();

            Assert.That(result, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetAllApiKeys_MapsMetadataCorrectly()
        {
            var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastUsed = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var keys = new List<ApiKey>
            {
                new ApiKey { Id = 42, Name = "k", Description = "desc", CreatedByUser = "alice", CreatedAt = createdAt, LastUsedAt = lastUsed },
            };
            repositoryMock.Setup(r => r.GetAll()).Returns(keys);

            var result = subject.GetAllApiKeys().Single();

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
        public void DeleteApiKey_ExistingId_ReturnsTrue()
        {
            repositoryMock.Setup(r => r.Exists(7)).Returns(true);
            repositoryMock.Setup(r => r.Remove(7));
            repositoryMock.Setup(r => r.Save());

            var result = subject.DeleteApiKey(7);

            Assert.That(result, Is.True);
        }

        [Test]
        public void DeleteApiKey_NonExistentId_ReturnsFalse()
        {
            repositoryMock.Setup(r => r.Exists(99)).Returns(false);

            var result = subject.DeleteApiKey(99);

            Assert.That(result, Is.False);
        }

        [Test]
        public void DeleteApiKey_NonExistentId_DoesNotCallRemove()
        {
            repositoryMock.Setup(r => r.Exists(99)).Returns(false);

            subject.DeleteApiKey(99);

            repositoryMock.Verify(r => r.Remove(It.IsAny<int>()), Times.Never);
        }

        // --- ValidateApiKey ---

        [Test]
        public async Task ValidateApiKey_ValidKey_ReturnsTrue()
        {
            var (plainText, hash, salt) = GenerateTestKey();
            var keys = new List<ApiKey>
            {
                new ApiKey { Id = 1, Name = "k", KeyHash = hash, Salt = salt, CreatedByUser = "alice" },
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
                new ApiKey { Id = 1, Name = "k", KeyHash = hash, Salt = salt, CreatedByUser = "alice" },
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
            var key = new ApiKey { Id = 1, Name = "k", KeyHash = hash, Salt = salt, CreatedByUser = "alice" };
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

            var creation = subject.CreateApiKeyAsync("setup-key", "desc", "alice").GetAwaiter().GetResult();
            return (creation.PlainTextKey, captured!.KeyHash, captured.Salt);
        }
    }
}
