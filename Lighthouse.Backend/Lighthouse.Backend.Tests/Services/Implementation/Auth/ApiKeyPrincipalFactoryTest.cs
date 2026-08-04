using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    // Epic 5146 slice 02a (#5641) — ADR-129 D29.
    // A dropped api_key_id fails OPEN: GetEffectivePermissionsAsync stops intersecting with the
    // per-key scope and returns the OWNER's permissions. Comparing two principals cannot catch that,
    // because the comparison passes when both sides drop the claim — so presence is asserted first.
    [TestFixture]
    public class ApiKeyPrincipalFactoryTest
    {
        private const int ApiKeyId = 77;
        private const string OwnerSubject = "owner-subject";
        private const string OwnerDisplayName = "Owner Display Name";

        [Test]
        public void Create_ResolvedOwner_CarriesTheApiKeyIdClaim()
        {
            var principal = ApiKeyPrincipalFactory.Create(ResolvedKey(), SchemeUnderTest);

            var apiKeyIdClaim = principal.FindFirst(ApiKeyPrincipalFactory.ApiKeyIdClaimType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(apiKeyIdClaim, Is.Not.Null,
                    "without api_key_id the session silently widens to the key owner's full scope");
                Assert.That(apiKeyIdClaim!.Value, Is.EqualTo("77"));
            }
        }

        [Test]
        public void Create_ResolvedOwner_CarriesTheStableSubjectClaim()
        {
            var principal = ApiKeyPrincipalFactory.Create(ResolvedKey(), SchemeUnderTest);

            var subjectClaim = principal.FindFirst(ApiKeyPrincipalFactory.SubjectClaimType);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subjectClaim, Is.Not.Null,
                    "without sub every scoped RBAC check fails closed and the frame renders empty");
                Assert.That(subjectClaim!.Value, Is.EqualTo(OwnerSubject));
            }
        }

        [Test]
        public void Create_SameValidationResult_ProducesTheSameClaimsForBothAuthenticationPaths()
        {
            var validationResult = ResolvedKey();

            var headerPrincipal = ApiKeyPrincipalFactory.Create(validationResult, HeaderScheme);
            var embedPrincipal = ApiKeyPrincipalFactory.Create(validationResult, EmbedScheme);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ClaimPairs(embedPrincipal), Is.EquivalentTo(ClaimPairs(headerPrincipal)),
                    "independent construction would drift silently while both paths kept authenticating");
                Assert.That(ClaimPairs(embedPrincipal), Does.Contain(ExpectedApiKeyIdPair),
                    "parity alone is satisfied when BOTH sides drop the claim, so the claim is pinned too");
            }
        }

        [Test]
        public void Create_UnlinkedOwner_EmitsNoSubjectButStillIdentifiesTheKey()
        {
            var principal = ApiKeyPrincipalFactory.Create(
                new ApiKeyValidationResult
                {
                    IsValid = true,
                    ApiKeyId = ApiKeyId,
                    OwnerResolutionState = ApiKeyOwnerResolutionState.Unlinked,
                },
                SchemeUnderTest);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(principal.FindFirst(ApiKeyPrincipalFactory.SubjectClaimType), Is.Null);
                Assert.That(principal.FindFirst(ApiKeyPrincipalFactory.ApiKeyIdClaimType), Is.Not.Null);
            }
        }

        [Test]
        public void Create_NoValidationResult_RefusesToBuildAPrincipal()
        {
            Assert.Throws<ArgumentNullException>(() => ApiKeyPrincipalFactory.Create(null!, SchemeUnderTest));
        }

        [Test]
        public void Create_UnlinkedOwnerStillCarryingASubject_EmitsNoSubjectClaim()
        {
            var principal = ApiKeyPrincipalFactory.Create(
                new ApiKeyValidationResult
                {
                    IsValid = true,
                    ApiKeyId = ApiKeyId,
                    OwnerResolutionState = ApiKeyOwnerResolutionState.Unlinked,
                    OwnerSubject = OwnerSubject,
                },
                SchemeUnderTest);

            Assert.That(principal.FindFirst(ApiKeyPrincipalFactory.SubjectClaimType), Is.Null,
                "the resolution state decides, not a leftover subject — an unlinked key must never authenticate as that person");
        }

        [Test]
        public void Create_ResolvedOwnerWithoutASubject_EmitsNoSubjectClaim()
        {
            var principal = ApiKeyPrincipalFactory.Create(
                new ApiKeyValidationResult
                {
                    IsValid = true,
                    ApiKeyId = ApiKeyId,
                    OwnerResolutionState = ApiKeyOwnerResolutionState.Resolved,
                    OwnerSubject = "   ",
                },
                SchemeUnderTest);

            Assert.That(principal.FindFirst(ApiKeyPrincipalFactory.SubjectClaimType), Is.Null,
                "a blank sub is an identity no RBAC row can match; emitting none at all is what fails closed");
        }

        [Test]
        public void Create_ResolvedOwner_CarriesTheOwnerDisplayNameAsTheNameClaim()
        {
            var principal = ApiKeyPrincipalFactory.Create(ResolvedKey(), SchemeUnderTest);

            Assert.That(principal.FindFirst(ApiKeyPrincipalFactory.NameClaimType)?.Value, Is.EqualTo(OwnerDisplayName),
                "the framed SPA renders this name; dropping it shows a working embed as an anonymous one");
        }

        [Test]
        [TestCase((string?)null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Create_ResolvedOwnerWithoutADisplayName_EmitsNoNameClaim(string? blankDisplayName)
        {
            var validationResult = ResolvedKey();
            validationResult.OwnerDisplayName = blankDisplayName;

            var principal = ApiKeyPrincipalFactory.Create(validationResult, SchemeUnderTest);

            Assert.That(principal.FindFirst(ApiKeyPrincipalFactory.NameClaimType), Is.Null,
                "no display name is not the same as a blank one — the SPA falls back only when the claim is absent");
        }

        // The factory is pure over its scheme argument, so the parity property is stated with two
        // arbitrary names rather than coupling this test to the scheme registry.
        private const string SchemeUnderTest = "AnyScheme";
        private const string HeaderScheme = "HeaderBorneScheme";
        private const string EmbedScheme = "EmbedCookieBorneScheme";

        private static readonly (string Type, string Value) ExpectedApiKeyIdPair =
            (ApiKeyPrincipalFactory.ApiKeyIdClaimType, "77");

        private static ApiKeyValidationResult ResolvedKey()
        {
            return new ApiKeyValidationResult
            {
                IsValid = true,
                ApiKeyId = ApiKeyId,
                OwnerResolutionState = ApiKeyOwnerResolutionState.Resolved,
                OwnerSubject = OwnerSubject,
                OwnerDisplayName = OwnerDisplayName,
            };
        }

        private static List<(string Type, string Value)> ClaimPairs(ClaimsPrincipal principal)
        {
            return principal.Claims
                .Select(claim => (claim.Type, claim.Value))
                .ToList();
        }
    }
}
