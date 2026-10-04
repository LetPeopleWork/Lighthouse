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

        [TestCase(AuthMode.Disabled, VoterIdentityKind.SelfDeclared)]
        [TestCase(AuthMode.Enabled, VoterIdentityKind.Account)]
        public void HowAVoterIsKnownFollowsTheAuthMode(AuthMode mode, VoterIdentityKind expected)
        {
            Assert.That(ResolverWhere(mode).Kind, Is.EqualTo(expected));
        }

        [TestCase("Jonas Weber", "Jonas Weber")]
        [TestCase("  Jonas Weber  ", "Jonas Weber")]
        [TestCase("J", "J")]
        public void WithoutSignInTheDeclaredNameIsTrimmedAndKept(string declared, string kept)
        {
            var voter = ResolverWhere(AuthMode.Disabled).ForWrite(declared, ThirtyTwoCharacterKey);

            Assert.That(voter, Is.EqualTo(new Voter(StoredFormOfThatKey, kept, null)));
        }

        [Test]
        public void ANameOfOneHundredCharactersIsKept()
        {
            var name = new string('a', VoterIdentityResolver.LongestVoterName);

            var voter = ResolverWhere(AuthMode.Disabled).ForWrite(name, ThirtyTwoCharacterKey);

            Assert.That(voter?.DisplayName, Is.EqualTo(name));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void WithoutANameNobodyVotes(string? declared)
        {
            Assert.That(ResolverWhere(AuthMode.Disabled).ForWrite(declared, ThirtyTwoCharacterKey), Is.Null);
        }

        [Test]
        public void ANameLongerThanOneHundredCharactersIsRefused()
        {
            var name = new string('a', VoterIdentityResolver.LongestVoterName + 1);

            Assert.That(ResolverWhere(AuthMode.Disabled).ForWrite(name, ThirtyTwoCharacterKey), Is.Null);
        }

        [TestCase(null)]
        [TestCase("0123456789abcdef0123456789abcde")]
        public void AMissingOrShortKeyIsRefusedOnAWrite(string? presented)
        {
            Assert.That(ResolverWhere(AuthMode.Disabled).ForWrite("Jonas Weber", presented), Is.Null);
        }

        [TestCase(ThirtyTwoCharacterKey, StoredFormOfThatKey)]
        [TestCase(null, null)]
        [TestCase("0123456789abcdef0123456789abcde", null)]
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

            Assert.That(first?.Key, Is.Not.EqualTo(second?.Key));
        }

        private static VoterIdentityResolver ResolverWhere(AuthMode mode)
            => new(Mock.Of<IAuthModeResolver>(resolver => resolver.Resolve() == new RuntimeAuthStatus { Mode = mode }));
    }
}
