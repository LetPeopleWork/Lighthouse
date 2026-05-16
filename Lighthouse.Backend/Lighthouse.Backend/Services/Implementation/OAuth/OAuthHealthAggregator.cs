using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.OAuth
{
    public sealed class OAuthHealthAggregator : IOAuthHealthAggregator
    {
        private readonly IRepository<OAuthCredential> credentialRepository;

        public OAuthHealthAggregator(IRepository<OAuthCredential> credentialRepository, TimeProvider timeProvider)
        {
            this.credentialRepository = credentialRepository ?? throw new ArgumentNullException(nameof(credentialRepository));
            _ = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public Task<OAuthHealthDto> AggregateAsync(CancellationToken cancellationToken)
        {
            var latestPerConnection = credentialRepository
                .GetAll()
                .GroupBy(c => c.WorkTrackingSystemConnectionId)
                .Select(g => g.OrderByDescending(c => c.UpdatedAt).First())
                .ToList();

            var totalOAuthConnections = latestPerConnection.Count;
            var disconnected = latestPerConnection.Where(c => c.Status != OAuthCredentialStatus.Valid).ToList();
            var firstDisconnectedConnectionId = disconnected
                .OrderByDescending(c => c.UpdatedAt)
                .ThenBy(c => c.WorkTrackingSystemConnectionId)
                .Select(c => (int?)c.WorkTrackingSystemConnectionId)
                .FirstOrDefault();

            return Task.FromResult(new OAuthHealthDto(totalOAuthConnections, disconnected.Count, firstDisconnectedConnectionId));
        }
    }
}
