using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    // Epic 5146 slice 02a (#5641) — ADR-131. Single use and pruning are each one conditional
    // statement over the same four columns; the HTTP tests can only reach them at whatever instant
    // the clock happens to be at, so the boundaries are pinned here at the exact instant instead.
    public class EmbedSessionTokenRepositoryTest : IntegrationTestBase
    {
        private static readonly DateTime Now = new(2026, 8, 4, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Later = Now.AddMinutes(5);
        private static readonly DateTime Earlier = Now.AddMinutes(-5);

        private const int ApiKeyId = 41;
        private const int OtherApiKeyId = 42;

        // The tokens carry a foreign key to the owning API key, so the keys have to exist first.
        [SetUp]
        public void SeedOwningApiKeys()
        {
            DatabaseContext.ApiKeys.AddRange(AnApiKey(ApiKeyId), AnApiKey(OtherApiKeyId));
            DatabaseContext.SaveChanges();
        }

        [Test]
        public async Task TryMarkRedeemed_AtTheExactExpiryInstant_RefusesWhileASecondOfLifeStillSucceeds()
        {
            await GivenTokens(
                AToken("expires-exactly-now", expiresAt: Now),
                AToken("expires-a-second-later", expiresAt: Now.AddSeconds(1)));

            var atTheBoundary = await CreateSubject().TryMarkRedeemedAsync("expires-exactly-now", Now, CancellationToken.None);
            var insideTheWindow = await CreateSubject().TryMarkRedeemedAsync("expires-a-second-later", Now, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(atTheBoundary, Is.Zero,
                    "expiry is exclusive: a token is dead at its own ExpiresAt, not one tick after it");
                Assert.That(insideTheWindow, Is.EqualTo(1),
                    "differential control: the same call inside the window still marks exactly one row");
            }
        }

        [Test]
        public async Task TryMarkRedeemed_TokenAlreadySpent_MarksNothing()
        {
            await GivenTokens(
                Spent(AToken("already-redeemed", expiresAt: Later), redeemedAt: Earlier),
                Revoked(AToken("revoked", expiresAt: Later), revokedAt: Earlier),
                AToken("expired", expiresAt: Earlier),
                AToken("outstanding", expiresAt: Later));

            var redeemedAgain = await CreateSubject().TryMarkRedeemedAsync("already-redeemed", Now, CancellationToken.None);
            var revoked = await CreateSubject().TryMarkRedeemedAsync("revoked", Now, CancellationToken.None);
            var expired = await CreateSubject().TryMarkRedeemedAsync("expired", Now, CancellationToken.None);
            var outstanding = await CreateSubject().TryMarkRedeemedAsync("outstanding", Now, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(redeemedAgain, Is.Zero,
                    "D27: single use is the affected-row count — a second redemption of the same token is a second session");
                Assert.That(revoked, Is.Zero);
                Assert.That(expired, Is.Zero);
                Assert.That(outstanding, Is.EqualTo(1),
                    "differential control: without it a zero everywhere proves only that the predicate never matches");
            }
        }

        [Test]
        public async Task PruneSpent_DeletesEveryTokenThatIsExpiredOrRedeemedOrRevoked_AndKeepsTheRest()
        {
            await GivenTokens(
                AToken("expires-exactly-now", expiresAt: Now),
                Spent(AToken("redeemed-but-unexpired", expiresAt: Later), redeemedAt: Earlier),
                Revoked(AToken("revoked-but-unexpired", expiresAt: Later), revokedAt: Earlier),
                AToken("outstanding", expiresAt: Later));

            var deleted = await CreateSubject().PruneSpentAsync(Now, CancellationToken.None);
            var survivors = await DatabaseContext.EmbedSessionTokens.AsNoTracking().Select(token => token.TokenId).ToListAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deleted, Is.EqualTo(3));
                Assert.That(survivors, Has.Count.EqualTo(1),
                    "each of the three spent states is independently sufficient — an AND here would leave live rows behind forever");
                Assert.That(survivors, Does.Contain("outstanding"),
                    "an outstanding token must survive the prune that every mint performs");
            }
        }

        [Test]
        public async Task RevokeOutstandingForApiKey_LeavesTokensOfOtherKeysAndAlreadySpentTokensAlone()
        {
            await GivenTokens(
                AToken("outstanding-of-this-key", expiresAt: Later),
                Spent(AToken("already-redeemed-of-this-key", expiresAt: Later), redeemedAt: Earlier),
                AToken("outstanding-of-another-key", expiresAt: Later, apiKeyId: OtherApiKeyId));

            var revoked = await CreateSubject().RevokeOutstandingForApiKeyAsync(ApiKeyId, Now, CancellationToken.None);
            var stillRedeemable = await CreateSubject().TryMarkRedeemedAsync("outstanding-of-another-key", Now, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(revoked, Is.EqualTo(1),
                    "revocation cascades from one API key only, and a token already spent needs no revoking");
                Assert.That(stillRedeemable, Is.EqualTo(1),
                    "another key's outstanding token is untouched");
            }
        }

        private EmbedSessionTokenRepository CreateSubject()
        {
            return new EmbedSessionTokenRepository(DatabaseContext);
        }

        private async Task GivenTokens(params EmbedSessionToken[] tokens)
        {
            DatabaseContext.EmbedSessionTokens.AddRange(tokens);
            await DatabaseContext.SaveChangesAsync();
        }

        private static ApiKey AnApiKey(int apiKeyId)
        {
            return new ApiKey
            {
                Id = apiKeyId,
                Name = $"embed-repository-key-{apiKeyId}",
                KeyHash = "hash",
                Salt = "salt",
            };
        }

        private static EmbedSessionToken AToken(string tokenId, DateTime expiresAt, int apiKeyId = ApiKeyId)
        {
            return new EmbedSessionToken
            {
                TokenId = tokenId,
                SecretHash = $"hash-of-{tokenId}",
                ApiKeyId = apiKeyId,
                CreatedAt = Earlier,
                ExpiresAt = expiresAt,
            };
        }

        private static EmbedSessionToken Spent(EmbedSessionToken token, DateTime redeemedAt)
        {
            token.RedeemedAt = redeemedAt;
            return token;
        }

        private static EmbedSessionToken Revoked(EmbedSessionToken token, DateTime revokedAt)
        {
            token.RevokedAt = revokedAt;
            return token;
        }
    }
}
