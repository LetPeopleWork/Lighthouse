using Lighthouse.Backend.API.DTO;

namespace Lighthouse.Backend.Services.Interfaces.OAuth
{
    public interface IOAuthHealthAggregator
    {
        Task<OAuthHealthDto> AggregateAsync(CancellationToken cancellationToken);
    }
}
