namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IWorkTrackingSystemConnectionUsage
    {
        IReadOnlyDictionary<int, ConnectionUsage> ByConnection();

        IReadOnlyDictionary<int, ConnectionFollowers> FollowersByConnection();
    }

    public sealed record ConnectionUsage(int Teams, int Portfolios)
    {
        public static readonly ConnectionUsage Unused = new(0, 0);
    }

    /// <summary>How many owners on a connection follow each of its defaults, whether or not that default is set.</summary>
    public sealed record ConnectionFollowers(int ParentOverrideForTeams)
    {
        public static readonly ConnectionFollowers None = new(0);
    }
}
