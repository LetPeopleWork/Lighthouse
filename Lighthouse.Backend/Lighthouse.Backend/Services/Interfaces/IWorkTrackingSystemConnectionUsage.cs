using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IWorkTrackingSystemConnectionUsage
    {
        IReadOnlyDictionary<int, ConnectionUsage> ByConnection();

        IReadOnlyDictionary<int, ConnectionFollowers> FollowersByConnection();

        IReadOnlyDictionary<int, IReadOnlyList<FollowerGroup>> FollowerGroupsByConnection();
    }

    public sealed record ConnectionUsage(int Teams, int Portfolios)
    {
        public static readonly ConnectionUsage Unused = new(0, 0);
    }

    /// <summary>How many owners on a connection follow each of its defaults, whether or not that default is set.</summary>
    public sealed record ConnectionFollowers(int ParentOverrideForTeams, int ParentOverrideForPortfolios, int DependencyFieldForPortfolios)
    {
        public static readonly ConnectionFollowers None = new(0, 0, 0);
    }

    /// <summary>
    /// How many owners on a connection follow exactly these defaults. An owner sits in one group only, so the
    /// owners following any of several changed defaults can be counted once each.
    /// </summary>
    public sealed record FollowerGroup(IReadOnlyList<ConnectionDefaultSetting> Follows, int Count);
}
