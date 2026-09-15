using Lighthouse.Backend.Models.Logging;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// What has recently gone wrong, said in words an operator can act on: where a problem is about a
    /// refresh, it names what the refresh was of rather than pointing at it by an id nobody can look up.
    ///
    /// Everything <see cref="IRecentProblems"/> says about the buffer behind this — bounded, this process
    /// only, not a log and not an audit trail — is true of this too.
    /// </summary>
    public interface IRecentProblemsReport
    {
        /// <summary>
        /// Everything still retained, most recent first, with every name looked up as this is answered. A
        /// team renamed this morning therefore reads under the name it has now, and one that has since been
        /// deleted falls back to the kind of work and its id, which is less than a name and far more than
        /// nothing.
        /// </summary>
        IReadOnlyList<RecentProblem> MostRecentFirst();
    }
}
