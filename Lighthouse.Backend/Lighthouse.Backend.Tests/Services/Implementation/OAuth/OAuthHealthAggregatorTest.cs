using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    [TestFixture]
    public class OAuthHealthAggregatorTest
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public async Task AggregateAsync_CountsStaleRefreshFailedCredentialsOver7dWindow(int refreshFailedOlderThan7d)
        {
            var credentials = new List<OAuthCredential>();
            for (var index = 0; index < refreshFailedOlderThan7d; index++)
            {
                credentials.Add(new OAuthCredential
                {
                    Id = index + 1,
                    Status = OAuthCredentialStatus.RefreshFailed,
                    UpdatedAt = FixedNow.AddDays(-8),
                });
            }

            var aggregator = CreateAggregator(credentials);

            var result = await aggregator.AggregateAsync(CancellationToken.None);

            Assert.That(result.StaleRefreshFailedCount7d, Is.EqualTo(refreshFailedOlderThan7d));
        }

        [TestCase(OAuthCredentialStatus.Valid)]
        [TestCase(OAuthCredentialStatus.Disconnected)]
        public async Task AggregateAsync_ExcludesNonRefreshFailedCredentialsFromStaleCount(OAuthCredentialStatus nonFailedStatus)
        {
            var credentials = new List<OAuthCredential>
            {
                new()
                {
                    Id = 1,
                    Status = nonFailedStatus,
                    UpdatedAt = FixedNow.AddDays(-30),
                },
                new()
                {
                    Id = 2,
                    Status = OAuthCredentialStatus.RefreshFailed,
                    UpdatedAt = FixedNow.AddDays(-2),
                },
            };

            var aggregator = CreateAggregator(credentials);

            var result = await aggregator.AggregateAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.StaleRefreshFailedCount24h, Is.EqualTo(1));
                Assert.That(result.StaleRefreshFailedCount7d, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task AggregateAsync_StaleCount24hIsSupersetOfStaleCount7d()
        {
            var credentials = new List<OAuthCredential>
            {
                new()
                {
                    Id = 1,
                    Status = OAuthCredentialStatus.RefreshFailed,
                    UpdatedAt = FixedNow.AddHours(-25),
                },
                new()
                {
                    Id = 2,
                    Status = OAuthCredentialStatus.RefreshFailed,
                    UpdatedAt = FixedNow.AddDays(-8),
                },
                new()
                {
                    Id = 3,
                    Status = OAuthCredentialStatus.RefreshFailed,
                    UpdatedAt = FixedNow.AddDays(-10),
                },
            };

            var aggregator = CreateAggregator(credentials);

            var result = await aggregator.AggregateAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.StaleRefreshFailedCount24h, Is.EqualTo(3), "all three rows are stale > 24h");
                Assert.That(result.StaleRefreshFailedCount7d, Is.EqualTo(2), "only two rows are stale > 7d");
                Assert.That(result.StaleRefreshFailedCount24h, Is.GreaterThanOrEqualTo(result.StaleRefreshFailedCount7d));
            }
        }

        [Test]
        public async Task AggregateAsync_ReportsEventStorePendingForUnavailableKpis()
        {
            var aggregator = CreateAggregator(new List<OAuthCredential>());

            var result = await aggregator.AggregateAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.SetupSuccessRate30d.Value, Is.Null);
                Assert.That(result.SetupSuccessRate30d.UnavailableReason, Is.EqualTo("event_store_pending"));
                Assert.That(result.RefreshSuccessRate7d.Value, Is.Null);
                Assert.That(result.RefreshSuccessRate7d.UnavailableReason, Is.EqualTo("event_store_pending"));
            }
        }

        private static OAuthHealthAggregator CreateAggregator(List<OAuthCredential> credentials)
        {
            var timeProvider = new FakeTimeProvider(FixedNow);

            var repositoryMock = new Mock<IRepository<OAuthCredential>>();
            repositoryMock
                .Setup(repository => repository.GetAll())
                .Returns(credentials);

            return new OAuthHealthAggregator(repositoryMock.Object, timeProvider);
        }
    }
}
