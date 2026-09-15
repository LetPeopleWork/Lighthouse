using Lighthouse.Backend.Models.Logging;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.Logging
{
    /// <summary>
    /// Turns what was captured into what is worth reading. The log pipeline records which team or portfolio
    /// a failed refresh belonged to but never what it is called, because the sink runs on every thread in
    /// the application and must not go to the database; the name is looked up here instead, once somebody
    /// has actually asked to see the list.
    ///
    /// Doing it here rather than at capture is also what keeps the answer honest after a rename: the name
    /// is never older than the moment it is read.
    /// </summary>
    public class RecentProblemsReport : IRecentProblemsReport
    {
        private readonly IRecentProblems recentProblems;

        private readonly IUpdateTaskNaming naming;

        public RecentProblemsReport(IRecentProblems recentProblems, IUpdateTaskNaming naming)
        {
            this.recentProblems = recentProblems;
            this.naming = naming;
        }

        public IReadOnlyList<RecentProblem> MostRecentFirst()
            => [.. recentProblems.MostRecentFirst().Select(NamingWhatItWasAbout)];

        private RecentProblem NamingWhatItWasAbout(RecentProblem problem)
        {
            if (problem.Refresh is not { } refresh)
            {
                return problem;
            }

            var name = naming.NameOf(new UpdateStatus { UpdateType = refresh.Kind, Id = refresh.Id });

            return problem with
            {
                Message = problem.Message.Replace(refresh.AsWritten, name, StringComparison.Ordinal),
            };
        }
    }
}
