using Lighthouse.Backend.Models.OAuth;

namespace Lighthouse.Backend.Services.Interfaces.OAuth
{
    public interface IOAuthHealthAggregator
    {
        Task<OAuthHealthDto> AggregateAsync(CancellationToken cancellationToken);
    }
}
