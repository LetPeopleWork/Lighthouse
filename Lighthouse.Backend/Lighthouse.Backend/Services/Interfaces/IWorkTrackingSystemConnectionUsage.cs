namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IWorkTrackingSystemConnectionUsage
    {
        IReadOnlyDictionary<int, ConnectionUsage> ByConnection();
    }

    public sealed record ConnectionUsage(int Teams, int Portfolios)
    {
        public static readonly ConnectionUsage Unused = new(0, 0);
    }
}
