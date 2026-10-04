using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Refinement;
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

        private static readonly string OneHundredCharacterName = new('a', VoterIdentityResolver.LongestVoterName);

        private static readonly string OneHundredAndOneCharacterName = new('a', VoterIdentityResolver.LongestVoterName + 1);

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
            yield return new TestCaseData(null, null, VoterRefusal.NameRequired).SetName("Neither a name nor a key asks for the name first");
        }

        [TestCase(AuthMode.Disabled, VoterIdentityKind.SelfDeclared)]
        [TestCase(AuthMode.Enabled, VoterIdentityKind.Account)]
        public void HowAVoterIsKnownFollowsTheAuthMode(AuthMode mode, VoterIdentityKind expected)
        {
            Assert.That(ResolverWhere(mode).Kind, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(WhatADeclaredNameAndAKeyAmountTo))]
        public void WithoutSignInANameAndAKeyMakeAVoterOrSayWhatIsMissing(string? declaredName, string? presentedKey, VoterRefusal? expectedRefusal)
        {
            var resolution = ResolverWhere(AuthMode.Disabled).ForWrite(declaredName, presentedKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.Refusal, Is.EqualTo(expectedRefusal));
                Assert.That(resolution.Voter is null, Is.EqualTo(expectedRefusal is not null));
            }
        }

        [TestCase("Jonas Weber", "Jonas Weber")]
        [TestCase("  Jonas Weber  ", "Jonas Weber")]
        [TestCase("J", "J")]
        public void WithoutSignInTheDeclaredNameIsTrimmedAndKept(string declared, string kept)
        {
            var resolution = ResolverWhere(AuthMode.Disabled).ForWrite(declared, ThirtyTwoCharacterKey);

            Assert.That(resolution.Voter, Is.EqualTo(new Voter(StoredFormOfThatKey, kept, null)));
        }

        [Test]
        public void WithSignInNobodyIsYetAVoter()
        {
            var resolution = ResolverWhere(AuthMode.Enabled).ForWrite("Jonas Weber", ThirtyTwoCharacterKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.Voter, Is.Null);
                Assert.That(resolution.Refusal, Is.EqualTo(VoterRefusal.NeedsAPerson));
            }
        }

        [TestCase(ThirtyTwoCharacterKey, StoredFormOfThatKey)]
        [TestCase(null, null)]
        [TestCase(ThirtyOneCharacterKey, null)]
        public void OnAReadTheKeyRecognisesTheReadersOwnVotes(string? presented, string? stored)
        {
            Assert.That(ResolverWhere(AuthMode.Disabled).ReaderKeyFrom(presented), Is.EqualTo(stored));
        }

        [Test]
        public void TwoBrowsersWithOneNameAreTwoVoters()
        {
            var resolver = ResolverWhere(AuthMode.Disabled);

            var first = resolver.ForWrite("Ana Lima", ThirtyTwoCharacterKey);
            var second = resolver.ForWrite("Ana Lima", "fedcba9876543210fedcba9876543210");

            Assert.That(first.Voter?.Key, Is.Not.EqualTo(second.Voter?.Key));
        }

        private static VoterIdentityResolver ResolverWhere(AuthMode mode)
            => new(Mock.Of<IAuthModeResolver>(resolver => resolver.Resolve() == new RuntimeAuthStatus { Mode = mode }));
    }
}
