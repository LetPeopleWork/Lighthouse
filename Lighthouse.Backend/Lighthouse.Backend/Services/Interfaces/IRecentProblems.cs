using Lighthouse.Backend.Models.Logging;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Read-only view over what has recently gone wrong on this instance. The buffer behind it is bounded
    /// and lives in this process only: it starts empty when the instance starts, it forgets its oldest
    /// entry to make room, and under several replicas each one answers only for itself. It is not a log
    /// and it is not an audit trail, and nothing in the product may present it as either.
    /// </summary>
    public interface IRecentProblems
    {
        /// <summary>
        /// Everything still retained, most recent first — the order somebody reading a short list from the
        /// top expects to find it in.
        /// </summary>
        IReadOnlyList<RecentProblem> MostRecentFirst();
    }
}
