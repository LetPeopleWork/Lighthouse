using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>Who uses a connection, and how many of them follow each of its defaults.</summary>
    public sealed record ConnectionUsageDto(int Teams, int Portfolios, ConnectionFollowers Followers);
}
