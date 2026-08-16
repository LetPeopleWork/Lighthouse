using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// The two facts the encryption settings need about the stored secrets, asked as one question
    /// because it is one screen. They are worked out differently and stay apart behind this; what goes
    /// away is the chance for a caller to ask for one, forget the other, and draw a panel that is half
    /// right.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class StoredSecretSummaryReaderTests
    {
        private static readonly string[] TwoKeys = ["k-2026-08-16-01", "k-legacy-default"];

        [Test]
        public async Task ReadAsync_CarriesBothAnswersThroughUntouched()
        {
            var summary = await new StoredSecretSummaryReader(
                new SaysThisManyAreExposed(3), new SaysTheseKeysAreInUse(TwoKeys)).ReadAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.UnderThePublishedKey, Is.EqualTo(3));
                Assert.That(summary.KeyIdsSeen, Is.EquivalentTo(TwoKeys));
            }
        }

        // Either half missing is a panel that reports one fact and invents the other, which is worse than
        // a panel that fails: an operator reads a count of zero as nothing to do.
        [Test]
        public void TheReader_RefusesToBeBuiltWithoutEitherHalf()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => new StoredSecretSummaryReader(null!, new SaysTheseKeysAreInUse(TwoKeys)),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => new StoredSecretSummaryReader(new SaysThisManyAreExposed(0), null!),
                    Throws.ArgumentNullException);
            }
        }

        private sealed class SaysThisManyAreExposed : IPublishedKeySecretCount
        {
            private readonly int howMany;

            public SaysThisManyAreExposed(int howMany)
            {
                this.howMany = howMany;
            }

            public Task<int> CountAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(howMany);
            }
        }

        private sealed class SaysTheseKeysAreInUse : IReferencedKeyIds
        {
            private readonly IReadOnlyCollection<string> keyIds;

            public SaysTheseKeysAreInUse(IReadOnlyCollection<string> keyIds)
            {
                this.keyIds = keyIds;
            }

            public Task<IReadOnlyCollection<string>> ReadAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(keyIds);
            }
        }
    }
}
