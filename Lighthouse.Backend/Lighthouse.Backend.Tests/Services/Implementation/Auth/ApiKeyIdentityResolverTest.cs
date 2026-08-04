using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    // Epic 5146 slice 02a (#5641) — ADR-129 D30. The embed entry point resolves the key's owner
    // AFTER the token was minted, so every branch here decides whether a session is handed out at
    // all, and with whose subject.
    [TestFixture]
    public class ApiKeyIdentityResolverTest
    {
        private const int ApiKeyId = 41;
        private const int OwnerProfileId = 12;
        private const int DanglingProfileId = 99;
        private const string OwnerSubject = "owner-subject";
        private const string OwnerDisplayName = "Owner Display Name";
        private const string OwnerEmail = "owner@example.test";
        private const string BlankSubject = "   ";

        private Mock<IApiKeyRepository> apiKeyRepository = null!;
        private Mock<IRepository<UserProfile>> userProfileRepository = null!;

        [SetUp]
        public void SetUp()
        {
            apiKeyRepository = new Mock<IApiKeyRepository>();
            userProfileRepository = new Mock<IRepository<UserProfile>>();
            userProfileRepository.Setup(repository => repository.GetAll()).Returns(Array.Empty<UserProfile>());
        }

        [Test]
        public void ResolveByApiKeyId_KeyNoLongerExists_ResolvesNoIdentity()
        {
            apiKeyRepository.Setup(repository => repository.GetById(ApiKeyId)).Returns((ApiKey?)null);

            Assert.That(CreateSubject().ResolveByApiKeyId(ApiKeyId), Is.Null,
                "a key deleted between mint and redemption must not still establish a session");
        }

        [Test]
        public void ResolveByApiKeyId_KeyWithNoOwnerAtAll_ReportsUnlinkedRatherThanFailing()
        {
            GivenApiKey(new ApiKey { Id = ApiKeyId });

            var identity = CreateSubject().ResolveByApiKeyId(ApiKeyId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(identity, Is.Not.Null);
                Assert.That(identity!.OwnerResolutionState, Is.EqualTo(ApiKeyOwnerResolutionState.Unlinked),
                    "D30: an unlinked owner is a refusal the caller can be told about, not a null the caller reads as 'key gone'");
                Assert.That(identity.IsValid, Is.True);
                Assert.That(identity.ApiKeyId, Is.EqualTo(ApiKeyId));
                Assert.That(identity.OwnerSubject, Is.Null);
            }
        }

        [Test]
        public void ResolveByApiKeyId_OwnerLinkedByProfileId_CarriesTheOwnersIdentity()
        {
            GivenApiKey(new ApiKey { Id = ApiKeyId, OwnerUserProfileId = OwnerProfileId });
            GivenProfileById(OwnerProfileId, AnOwnerProfile(OwnerProfileId, OwnerSubject));

            var identity = CreateSubject().ResolveByApiKeyId(ApiKeyId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(identity!.OwnerResolutionState, Is.EqualTo(ApiKeyOwnerResolutionState.Resolved));
                Assert.That(identity.IsValid, Is.True);
                Assert.That(identity.OwnerSubject, Is.EqualTo(OwnerSubject),
                    "the subject is what every scoped RBAC check resolves against");
                Assert.That(identity.OwnerDisplayName, Is.EqualTo(OwnerDisplayName));
                Assert.That(identity.OwnerEmail, Is.EqualTo(OwnerEmail));
            }
        }

        [Test]
        public void ResolveByApiKeyId_OwnerLinkedBySubjectOnly_CarriesTheOwnersIdentity()
        {
            GivenApiKey(new ApiKey { Id = ApiKeyId, OwnerSubject = OwnerSubject });
            GivenProfiles(AnOwnerProfile(OwnerProfileId, OwnerSubject));

            var identity = CreateSubject().ResolveByApiKeyId(ApiKeyId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(identity!.OwnerResolutionState, Is.EqualTo(ApiKeyOwnerResolutionState.Resolved),
                    "a key linked by subject alone — no profile id — must still resolve, or its embed session renders empty");
                Assert.That(identity.OwnerSubject, Is.EqualTo(OwnerSubject));
            }
        }

        [Test]
        public void ResolveByApiKeyId_ProfileIdDangles_FallsBackToTheSubject()
        {
            GivenApiKey(new ApiKey { Id = ApiKeyId, OwnerUserProfileId = DanglingProfileId, OwnerSubject = OwnerSubject });
            GivenProfileById(DanglingProfileId, null);
            GivenProfiles(AnOwnerProfile(OwnerProfileId, OwnerSubject));

            var identity = CreateSubject().ResolveByApiKeyId(ApiKeyId);

            Assert.That(identity!.OwnerResolutionState, Is.EqualTo(ApiKeyOwnerResolutionState.Resolved),
                "the profile id is a cache of the subject link; a stale one must not cost the owner their session");
        }

        [Test]
        public void ResolveByApiKeyId_SubjectMatchesNoProfile_ReportsUnlinkedInsteadOfThrowing()
        {
            GivenApiKey(new ApiKey { Id = ApiKeyId, OwnerSubject = "subject-of-a-deleted-profile" });
            GivenProfiles(AnOwnerProfile(OwnerProfileId, OwnerSubject));

            var identity = CreateSubject().ResolveByApiKeyId(ApiKeyId);

            Assert.That(identity!.OwnerResolutionState, Is.EqualTo(ApiKeyOwnerResolutionState.Unlinked),
                "no match is an ordinary outcome — throwing here would turn a legible refusal into a 500 inside the frame");
        }

        [Test]
        [TestCase((string?)null)]
        [TestCase("")]
        [TestCase(BlankSubject)]
        public void ResolveByApiKeyId_OwnerSubjectIsBlank_NeverMatchesAProfile(string? blankOwnerSubject)
        {
            GivenApiKey(new ApiKey { Id = ApiKeyId, OwnerSubject = blankOwnerSubject });
            GivenProfiles(AnOwnerProfile(OwnerProfileId, BlankSubject));

            var identity = CreateSubject().ResolveByApiKeyId(ApiKeyId);

            Assert.That(identity!.OwnerResolutionState, Is.EqualTo(ApiKeyOwnerResolutionState.Unlinked),
                "a blank owner subject must never be matched against a profile — that would hand a key an arbitrary identity");
        }

        private ApiKeyIdentityResolver CreateSubject()
        {
            return new ApiKeyIdentityResolver(apiKeyRepository.Object, userProfileRepository.Object);
        }

        private void GivenApiKey(ApiKey apiKey)
        {
            apiKeyRepository.Setup(repository => repository.GetById(ApiKeyId)).Returns(apiKey);
        }

        private void GivenProfileById(int profileId, UserProfile? profile)
        {
            userProfileRepository.Setup(repository => repository.GetById(profileId)).Returns(profile);
        }

        private void GivenProfiles(params UserProfile[] profiles)
        {
            userProfileRepository.Setup(repository => repository.GetAll()).Returns(profiles);
        }

        private static UserProfile AnOwnerProfile(int profileId, string subject)
        {
            return new UserProfile
            {
                Id = profileId,
                Subject = subject,
                SubjectClaimType = "sub",
                DisplayName = OwnerDisplayName,
                Email = OwnerEmail,
            };
        }
    }
}
