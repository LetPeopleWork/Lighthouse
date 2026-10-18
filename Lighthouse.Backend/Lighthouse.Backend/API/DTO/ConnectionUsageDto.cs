using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>Who uses a connection, how many of them follow each of its defaults, and which defaults each follows.</summary>
    public sealed record ConnectionUsageDto(int Teams, int Portfolios, ConnectionFollowers Followers, IReadOnlyList<FollowerGroupDto> FollowerGroups);

    /// <summary>How many owners follow exactly these defaults, each named as the connection's defaults are.</summary>
    public sealed record FollowerGroupDto(IReadOnlyList<string> Follows, int Count)
    {
        public static FollowerGroupDto From(FollowerGroup group)
            => new([.. group.Follows.Select(WorkTrackingSystemConnectionDto.NameOf)], group.Count);
    }
}
