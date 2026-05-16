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

        [Test]
        public async Task AggregateAsync_NoCredentials_ReturnsZeroTotals()
        {
            var aggregator = CreateAggregator([]);

            var result = await aggregator.AggregateAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.TotalOAuthConnections, Is.Zero);
                Assert.That(result.DisconnectedCount, Is.Zero);
            }
        }

        [Test]
        public async Task AggregateAsync_OnlyValidCredentials_ReturnsTotalAndZeroDisconnected()
        {
            var credentials = new List<OAuthCredential>
            {
                new() { Id = 1, WorkTrackingSystemConnectionId = 10, Status = OAuthCredentialStatus.Valid, UpdatedAt = FixedNow },
                new() { Id = 2, WorkTrackingSystemConnectionId = 11, Status = OAuthCredentialStatus.Valid, UpdatedAt = FixedNow },
            };

            var aggregator = CreateAggregator(credentials);

            var result = await aggregator.AggregateAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.TotalOAuthConnections, Is.EqualTo(2));
                Assert.That(result.DisconnectedCount, Is.Zero);
            }
        }

        [TestCase(OAuthCredentialStatus.RefreshFailed)]
        [TestCase(OAuthCredentialStatus.Disconnected)]
        public async Task AggregateAsync_NonValidCredential_IncrementsDisconnectedCount(OAuthCredentialStatus status)
        {
            var credentials = new List<OAuthCredential>
            {
                new() { Id = 1, WorkTrackingSystemConnectionId = 10, Status = OAuthCredentialStatus.Valid, UpdatedAt = FixedNow },
                new() { Id = 2, WorkTrackingSystemConnectionId = 11, Status = status, UpdatedAt = FixedNow },
            };

            var aggregator = CreateAggregator(credentials);

            var result = await aggregator.AggregateAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.TotalOAuthConnections, Is.EqualTo(2));
                Assert.That(result.DisconnectedCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task AggregateAsync_MultipleCredentialsForSameConnection_CountsConnectionOnce()
        {
            var credentials = new List<OAuthCredential>
            {
                new() { Id = 1, WorkTrackingSystemConnectionId = 10, Status = OAuthCredentialStatus.Disconnected, UpdatedAt = FixedNow.AddDays(-1) },
                new() { Id = 2, WorkTrackingSystemConnectionId = 10, Status = OAuthCredentialStatus.Valid, UpdatedAt = FixedNow },
            };

            var aggregator = CreateAggregator(credentials);

            var result = await aggregator.AggregateAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.TotalOAuthConnections, Is.EqualTo(1));
                Assert.That(result.DisconnectedCount, Is.Zero, "latest credential is Valid, so connection is healthy");
            }
        }

        private static OAuthHealthAggregator CreateAggregator(List<OAuthCredential> credentials)
        {
            var timeProvider = new FakeTimeProvider(FixedNow);

            var repositoryMock = new Mock<IRepository<OAuthCredential>>();
            repositoryMock.Setup(repository => repository.GetAll()).Returns(credentials);

            return new OAuthHealthAggregator(repositoryMock.Object, timeProvider);
        }
    }
}
