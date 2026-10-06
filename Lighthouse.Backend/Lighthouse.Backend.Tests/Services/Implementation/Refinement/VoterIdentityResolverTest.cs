using System.Security.Claims;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    public class VoterIdentityResolverTest
    {
        private const string ThirtyTwoCharacterKey = "0123456789abcdef0123456789abcdef";

        // SHA-256 of the key above, worked out outside the product with `sha256sum`.
        private const string StoredFormOfThatKey = "self:3eb1bd439947eb762998e566ccc2e099c791118b2f40579cc4f7da2b5061b7f9";

        private const string ThirtyOneCharacterKey = "0123456789abcdef0123456789abcde";

        private const string SixtyFourCharacterKey = ThirtyTwoCharacterKey + ThirtyTwoCharacterKey;

        private const string ThirtyTwoSpaces = "                                ";

        private static readonly string OneHundredCharacterName = new('a', VoterIdentityResolver.LongestVoterName);

        private static readonly string OneHundredAndOneCharacterName = new('a', VoterIdentityResolver.LongestVoterName + 1);

        private static readonly UserProfile Jonas = new() { Id = 7, Subject = "jonas-subject", DisplayName = "Jonas Weber" };

        private ClaimsPrincipal signedIn = new(new ClaimsIdentity());

        private int personLookups;

        [SetUp]
        public void SetUp()
        {
            signedIn = new ClaimsPrincipal(new ClaimsIdentity());
            personLookups = 0;
        }

        private static IEnumerable<TestCaseData> WhatADeclaredNameAndAKeyAmountTo()
        {
            yield return new TestCaseData(null, ThirtyTwoCharacterKey, VoterRefusal.NameRequired).SetName("No name");
            yield return new TestCaseData("", ThirtyTwoCharacterKey, VoterRefusal.NameRequired).SetName("An empty name");
            yield return new TestCaseData("   ", ThirtyTwoCharacterKey, VoterRefusal.NameRequired).SetName("A blank name");
            yield return new TestCaseData("J", ThirtyTwoCharacterKey, null).SetName("A one-character name");
            yield return new TestCaseData(OneHundredCharacterName, ThirtyTwoCharacterKey, null).SetName("A one-hundred-character name");
            yield return new TestCaseData($"  {OneHundredCharacterName}  ", ThirtyTwoCharacterKey, null).SetName("A one-hundred-character name with spaces around it");
            yield return new TestCaseData(OneHundredAndOneCharacterName, ThirtyTwoCharacterKey, VoterRefusal.NameTooLong).SetName("A one-hundred-and-one-character name");
            yield return new TestCaseData("Jonas Weber", null, VoterRefusal.KeyRequired).SetName("No key");
            yield return new TestCaseData("Jonas Weber", ThirtyOneCharacterKey, VoterRefusal.KeyRequired).SetName("A thirty-one-character key");
            yield return new TestCaseData("Jonas Weber", ThirtyTwoCharacterKey, null).SetName("A thirty-two-character key");
            yield return new TestCaseData("Jonas Weber", SixtyFourCharacterKey, null).SetName("A sixty-four-character key");
            yield return new TestCaseData("Jonas Weber", ThirtyTwoSpaces, VoterRefusal.KeyRequired).SetName("A key of thirty-two spaces");
            yield return new TestCaseData("Jonas Weber", "0123456789abcdef 0123456789abcdef", VoterRefusal.KeyRequired).SetName("A key with a space inside");
            yield return new TestCaseData("Jonas Weber", ThirtyTwoCharacterKey + "\t", VoterRefusal.KeyRequired).SetName("A key ending in a tab");
            yield return new TestCaseData("Jonas Weber", ThirtyTwoCharacterKey + "\u0001", VoterRefusal.KeyRequired).SetName("A key carrying a control character");
            yield return new TestCaseData(null, null, VoterRefusal.NameRequired).SetName("Neither a name nor a key asks for the name first");
        }

        [TestCase(AuthMode.Disabled, VoterIdentityKind.SelfDeclared)]
        [TestCase(AuthMode.Enabled, VoterIdentityKind.Account)]
        public void HowAVoterIsKnownFollowsTheAuthMode(AuthMode mode, VoterIdentityKind expected)
        {
            Assert.That(ResolverWhere(mode).Kind, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(WhatADeclaredNameAndAKeyAmountTo))]
        public async Task WithoutSignInANameAndAKeyMakeAVoterOrSayWhatIsMissing(string? declaredName, string? presentedKey, VoterRefusal? expectedRefusal)
        {
            var resolution = await ResolverWhere(AuthMode.Disabled).ForWriteAsync(declaredName, presentedKey, ThePerson(Jonas));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.Refusal, Is.EqualTo(expectedRefusal));
                Assert.That(resolution.Voter is null, Is.EqualTo(expectedRefusal is not null));
                Assert.That(personLookups, Is.Zero, "without sign-in there is no account to look up");
            }
        }

        [TestCase("Jonas Weber", "Jonas Weber")]
        [TestCase("  Jonas Weber  ", "Jonas Weber")]
        [TestCase("J", "J")]
        public async Task WithoutSignInTheDeclaredNameIsTrimmedAndKept(string declared, string kept)
        {
            var resolution = await ResolverWhere(AuthMode.Disabled).ForWriteAsync(declared, ThirtyTwoCharacterKey, ThePerson(Jonas));

            Assert.That(resolution.Voter, Is.EqualTo(new Voter(StoredFormOfThatKey, kept, null)));
        }

        [TestCase(null, null)]
        [TestCase("Ana Lima", ThirtyTwoCharacterKey)]
        [TestCase("", ThirtyOneCharacterKey)]
        public async Task WithSignInTheVoterIsTheAccountWhateverNameOrKeyIsSent(string? declaredName, string? presentedKey)
        {
            var resolution = await ResolverWhere(AuthMode.Enabled).ForWriteAsync(declaredName, presentedKey, ThePerson(Jonas));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.Voter, Is.EqualTo(new Voter("account:jonas-subject", "Jonas Weber", 7)));
                Assert.That(resolution.Refusal, Is.Null);
                Assert.That(personLookups, Is.EqualTo(1));
            }
        }

        [TestCase(" ", "e@x", "e@x")]
        [TestCase("", "e@x", "e@x")]
        [TestCase(" ", "", "s")]
        [TestCase(null, " ", "s")]
        [TestCase("  Ana Lima  ", "e@x", "Ana Lima")]
        [TestCase(null, "  e@x  ", "e@x")]
        public async Task WithSignInABlankAccountNameFallsBackToTheEmailThenTheSubject(string? displayName, string? email, string shownAs)
        {
            var person = new UserProfile { Id = 9, Subject = "s", DisplayName = displayName, Email = email };

            var resolution = await ResolverWhere(AuthMode.Enabled).ForWriteAsync(null, null, ThePerson(person));

            Assert.That(resolution.Voter?.DisplayName, Is.EqualTo(shownAs));
        }

        [Test]
        public async Task WithSignInACredentialNoPersonStandsBehindIsRefused()
        {
            var resolution = await ResolverWhere(AuthMode.Enabled).ForWriteAsync("Jonas Weber", ThirtyTwoCharacterKey, ThePerson(null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.Voter, Is.Null);
                Assert.That(resolution.Refusal, Is.EqualTo(VoterRefusal.NeedsAPerson));
            }
        }

        [Test]
        public async Task TwoAccountsWithOneNameAreTwoVoters()
        {
            var resolver = ResolverWhere(AuthMode.Enabled);

            var zurich = await resolver.ForWriteAsync(null, null, ThePerson(new UserProfile { Id = 1, Subject = "ana-zurich", DisplayName = "Ana Lima" }));
            var lisbon = await resolver.ForWriteAsync(null, null, ThePerson(new UserProfile { Id = 2, Subject = "ana-lisbon", DisplayName = "Ana Lima" }));

            Assert.That(zurich.Voter?.Key, Is.Not.EqualTo(lisbon.Voter?.Key));
        }

        [TestCase(ThirtyTwoCharacterKey, StoredFormOfThatKey)]
        [TestCase(null, null)]
        [TestCase(ThirtyOneCharacterKey, null)]
        [TestCase(ThirtyTwoSpaces, null)]
        public void WithoutSignInOnAReadTheKeyRecognisesTheReadersOwnVotes(string? presented, string? stored)
        {
            Assert.That(ResolverWhere(AuthMode.Disabled).ReaderKeyFrom(presented), Is.EqualTo(stored));
        }

        [TestCase("sub", "account:jonas-subject")]
        [TestCase("oid", "account:jonas-subject")]
        [TestCase("name", null)]
        public void WithSignInOnAReadTheSessionsSubjectRecognisesTheReadersOwnVotes(string claimType, string? stored)
        {
            signedIn = new ClaimsPrincipal(new ClaimsIdentity([new Claim(claimType, "jonas-subject")], "test"));

            Assert.That(ResolverWhere(AuthMode.Enabled).ReaderKeyFrom(ThirtyTwoCharacterKey), Is.EqualTo(stored));
        }

        [Test]
        public void WithSignInTheSubjectClaimWinsOverTheObjectId()
        {
            signedIn = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "object-id"), new Claim("sub", "jonas-subject")], "test"));

            Assert.That(ResolverWhere(AuthMode.Enabled).ReaderKeyFrom(null), Is.EqualTo("account:jonas-subject"));
        }

        [Test]
        public async Task TwoBrowsersWithOneNameAreTwoVoters()
        {
            var resolver = ResolverWhere(AuthMode.Disabled);

            var first = await resolver.ForWriteAsync("Ana Lima", ThirtyTwoCharacterKey, ThePerson(null));
            var second = await resolver.ForWriteAsync("Ana Lima", "fedcba9876543210fedcba9876543210", ThePerson(null));

            Assert.That(first.Voter?.Key, Is.Not.EqualTo(second.Voter?.Key));
        }

        private Func<Task<UserProfile?>> ThePerson(UserProfile? person)
            => () =>
            {
                personLookups++;
                return Task.FromResult(person);
            };

        private VoterIdentityResolver ResolverWhere(AuthMode mode)
            => new(
                Mock.Of<IAuthModeResolver>(resolver => resolver.Resolve() == new RuntimeAuthStatus { Mode = mode }),
                Mock.Of<IHttpContextAccessor>(accessor => accessor.HttpContext == new DefaultHttpContext { User = signedIn }));
    }
}
